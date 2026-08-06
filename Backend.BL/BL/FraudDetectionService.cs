using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Fraud;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace Backend.BL.BL
{
    /// <summary>
    /// Moteur de détection de fraude / anomalie, style bancaire (AML).
    /// Types de règles supportées :
    ///  - MontantMaximal    : un mouvement dépasse un montant absolu.
    ///  - MontantAnormal    : un mouvement dépasse un multiple de la moyenne du client.
    ///  - PicDebit          : un pic de débits sur une journée dépasse un multiple de la moyenne journalière.
    ///  - MultiComptes      : le client détient plus de comptes que le seuil.
    ///  - Frequence         : le nombre de mouvements dépasse le seuil sur la période.
    ///  - DecoupageAnormal  : structuration/smurfing — plusieurs mouvements juste sous un seuil de déclaration.
    ///  - RapidInOut        : passage de fonds — crédit suivi d'un débit équivalent très rapproché.
    ///  - CompteReactive    : compte dormant réactivé par un crédit important.
    ///  - ActiviteNocturne  : activité hors horaires bancaires (nuit / week-end).
    /// Le poids (Poids) de chaque règle module le score final de l'alerte (0-100).
    /// </summary>
    public class FraudDetectionService : IFraudDetectionService
    {
        private const string StatutNouvelle = "Nouvelle";

        private readonly IRegleAntifraudeRepository _regleRepository;
        private readonly IAlertesFraudeRepository _alerteRepository;
        private readonly IClientRepository _clientRepository;
        private readonly ICompteRepository _compteRepository;
        private readonly IMouvementRepository _mouvementRepository;
        private readonly IActivityService _activityService;
        private readonly ILogger<FraudDetectionService> _logger;

        public FraudDetectionService(
            IRegleAntifraudeRepository regleRepository,
            IAlertesFraudeRepository alerteRepository,
            IClientRepository clientRepository,
            ICompteRepository compteRepository,
            IMouvementRepository mouvementRepository,
            IActivityService activityService,
            ILogger<FraudDetectionService> logger)
        {
            _regleRepository = regleRepository;
            _alerteRepository = alerteRepository;
            _clientRepository = clientRepository;
            _compteRepository = compteRepository;
            _mouvementRepository = mouvementRepository;
            _activityService = activityService;
            _logger = logger;
        }

        // ---------------------------------------------------------------- Analyse

        public async Task<AnalyseFraudeResultatDto> AnalyserClientAsync(int idClient, int utilisateurId = 0)
        {
            var client = await _clientRepository.GetByIdAsync(idClient);
            if (client == null)
                throw new KeyNotFoundException($"Client {idClient} introuvable.");

            var regles = await _regleRepository.GetAllAsync(activesSeulement: true);
            var comptes = await _compteRepository.GetByClientIdAsync(idClient);
            var resultat = new AnalyseFraudeResultatDto { IdClient = idClient };
            var alertesCrees = new List<AlertesFraude>();

            if (comptes.Count == 0)
                return resultat;

            // Périmètre des mouvements selon la fenêtre maximale des règles
            var periodeMax = regles.Any() ? regles.Max(r => r.PeriodeJours) : 0;
            var dateDebut = periodeMax > 0 ? DateTime.UtcNow.AddDays(-periodeMax) : (DateTime?)null;
            var mouvements = await _mouvementRepository.GetByClientAllAsync(idClient, dateDebut);

            foreach (var regle in regles)
            {
                var detection = DetecterRegle(regle, client, comptes, mouvements);

                if (detection == null)
                    continue;

                // Intégration du poids de la règle dans le score (Poids=50 → neutre)
                detection.Score = Math.Min(100, Math.Max(10, detection.Score + (regle.Poids - 50) / 2));

                var fingerprint = ConstruireFingerprint(regle, idClient, detection.NumCompte, detection.IdMouvement);
                var existante = await _alerteRepository.GetByFingerprintAsync(fingerprint);

                if (existante != null)
                {
                    resultat.AlertesExistantes++;
                    continue;
                }

                var alerte = new AlertesFraude
                {
                    IdClient = idClient,
                    IdRegle = regle.Id,
                    IdMouvement = detection.IdMouvement,
                    NumCompte = detection.NumCompte,
                    Agence = detection.Agence,
                    Montant = detection.Montant,
                    Type = regle.Type,
                    Niveau = detection.Niveau,
                    Score = detection.Score,
                    Description = detection.Description,
                    Statut = StatutNouvelle,
                    Fingerprint = fingerprint,
                    DonneesJson = detection.DonneesJson,
                    DateDetection = DateTime.UtcNow
                };

                await _alerteRepository.AddAsync(alerte);
                alertesCrees.Add(alerte);

                resultat.AlertesCrees++;
                resultat.Alertes.Add(MapAlerteDetectee(alerte, regle));
            }

            if (alertesCrees.Count > 0)
            {
                await _alerteRepository.SaveChangesAsync();

                await _activityService.EnregistrerEvenementAsync(
                    idClient,
                    EvenementTypes.Compte,
                    $"{alertesCrees.Count} alerte(s) de fraude détectée(s) par le moteur d'analyse.",
                    donneesJson: JsonSerializer.Serialize(alertesCrees.Select(a => a.Id)),
                    idLien: alertesCrees.First().Id,
                    typeLien: nameof(AlertesFraude));
            }

            _logger.LogInformation("Analyse antifraude client {ClientId} : {Count} nouvelle(s) alerte(s).", idClient, alertesCrees.Count);
            return resultat;
        }

        public async Task<int> AnalyserTousAsync()
        {
            var clients = await _clientRepository.GetAllAsync();
            var total = 0;

            foreach (var client in clients)
            {
                try
                {
                    var resultat = await AnalyserClientAsync(client.Id);
                    total += resultat.AlertesCrees;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Échec de l'analyse antifraude du client {ClientId}.", client.Id);
                }
            }

            return total;
        }

        private DetectionRegle? DetecterRegle(
            RegleAntifraude regle,
            Client client,
            List<Compte> comptes,
            List<Mouvement> mouvements)
        {
            switch (regle.Type)
            {
                case "MontantMaximal":
                    return DetecterMontantMaximal(regle, mouvements);
                case "MontantAnormal":
                    return DetecterMontantAnormal(regle, mouvements);
                case "PicDebit":
                    return DetecterPicDebit(regle, mouvements);
                case "MultiComptes":
                    return DetecterMultiComptes(regle, comptes);
                case "Frequence":
                    return DetecterFrequence(regle, mouvements);
                case "DecoupageAnormal":
                    return DetecterDecoupageAnormal(regle, mouvements);
                case "RapidInOut":
                    return DetecterRapidInOut(regle, mouvements);
                case "CompteReactive":
                    return DetecterCompteReactive(regle, mouvements);
                case "ActiviteNocturne":
                    return DetecterActiviteNocturne(regle, mouvements);
                default:
                    return null;
            }
        }

        private DetectionRegle? DetecterMontantMaximal(RegleAntifraude regle, List<Mouvement> mouvements)
        {
            var mouvementsPeriode = FiltrerPeriode(mouvements, regle.PeriodeJours);
            var mvt = mouvementsPeriode.FirstOrDefault(m => m.Montant > regle.Seuil);
            if (mvt == null) return null;

            var ratio = mvt.Montant / regle.Seuil;
            var score = Math.Min(100, 60 + (int)((ratio - 1m) * 40m));
            var sensLibelle = mvt.Sens == "D" ? "débit" : "crédit";

            return new DetectionRegle
            {
                IdMouvement = mvt.Id,
                NumCompte = mvt.NumCompte,
                Agence = mvt.Agence,
                Montant = mvt.Montant,
                Score = score,
                Niveau = NiveauPourScore(score),
                Description = $"Mouvement de {mvt.Montant:N0} TND ({sensLibelle}) sur le compte {mvt.NumCompte} le {mvt.DateMvt:dd/MM/yyyy} — dépasse le seuil de {regle.Seuil:N0} TND (règle « {regle.Nom} »).",
                DonneesJson = JsonSerializer.Serialize(new { Regle = regle.Code, Compte = mvt.NumCompte, Montant = mvt.Montant, Date = mvt.DateMvt })
            };
        }

        private DetectionRegle? DetecterMontantAnormal(RegleAntifraude regle, List<Mouvement> mouvements)
        {
            var mouvementsPeriode = FiltrerPeriode(mouvements, regle.PeriodeJours);
            if (mouvementsPeriode.Count < 5) return null;

            var moyenne = mouvementsPeriode.Average(m => (double)m.Montant);
            if (moyenne <= 0) return null;

            var seuilAnomalie = (double)regle.Seuil * moyenne;
            var mvt = mouvementsPeriode.FirstOrDefault(m => (double)m.Montant > seuilAnomalie);
            if (mvt == null) return null;

            var ratio = (double)mvt.Montant / moyenne;
            var score = Math.Min(100, (int)((ratio / ((double)regle.Seuil * 2d)) * 100d));

            return new DetectionRegle
            {
                IdMouvement = mvt.Id,
                NumCompte = mvt.NumCompte,
                Agence = mvt.Agence,
                Montant = mvt.Montant,
                Score = score,
                Niveau = NiveauPourScore(score),
                Description = $"Mouvement de {mvt.Montant:N0} TND le {mvt.DateMvt:dd/MM/yyyy} — {ratio:F1} fois la moyenne du client ({moyenne:N0} TND) sur la période (règle « {regle.Nom} »).",
                DonneesJson = JsonSerializer.Serialize(new { Regle = regle.Code, Compte = mvt.NumCompte, Montant = mvt.Montant, Ratio = Math.Round(ratio, 2), Moyenne = Math.Round(moyenne, 2) })
            };
        }

        private DetectionRegle? DetecterPicDebit(RegleAntifraude regle, List<Mouvement> mouvements)
        {
            var mouvementsPeriode = FiltrerPeriode(mouvements, regle.PeriodeJours);
            var debits = mouvementsPeriode.Where(m => m.Sens == "D").ToList();
            if (debits.Count < 10) return null;

            var debitQuotidien = debits
                .GroupBy(m => m.DateMvt.Date)
                .Select(g => (Jour: g.Key, Total: g.Sum(m => m.Montant)))
                .OrderByDescending(x => x.Total)
                .ToList();

            if (debitQuotidien.Count == 0) return null;

            var moyenneJournaliere = debitQuotidien.Average(x => (double)x.Total);
            var seuilPic = (double)regle.Seuil * moyenneJournaliere;

            var pic = debitQuotidien.FirstOrDefault(x => (double)x.Total > seuilPic);
            if (pic.Total == 0) return null;

            var ratio = (double)pic.Total / moyenneJournaliere;
            var score = Math.Min(100, (int)((ratio / ((double)regle.Seuil * 2d)) * 100d));

            return new DetectionRegle
            {
                NumCompte = null,
                Score = score,
                Niveau = NiveauPourScore(score),
                Montant = pic.Total,
                Description = $"Pic de débits le {pic.Jour:dd/MM/yyyy} : {pic.Total:N0} TND débités ({ratio:F1} fois la moyenne journalière de {moyenneJournaliere:N0} TND) (règle « {regle.Nom} »).",
                DonneesJson = JsonSerializer.Serialize(new { Regle = regle.Code, Jour = pic.Jour, Total = pic.Total, Ratio = Math.Round(ratio, 2) })
            };
        }

        private DetectionRegle? DetecterMultiComptes(RegleAntifraude regle, List<Compte> comptes)
        {
            var nbComptes = comptes.Count;
            if (nbComptes <= (int)regle.Seuil) return null;

            var score = Math.Min(100, 50 + (nbComptes - (int)regle.Seuil) * 25);

            return new DetectionRegle
            {
                Score = score,
                Niveau = NiveauPourScore(score),
                Description = $"Le client détient {nbComptes} comptes, au-delà du seuil de {regle.Seuil:N0} (règle « {regle.Nom} ») : {string.Join(", ", comptes.Select(c => c.NumCompte))}.",
                DonneesJson = JsonSerializer.Serialize(new { Regle = regle.Code, NbComptes = nbComptes, Comptes = comptes.Select(c => c.NumCompte).ToList() })
            };
        }

        private DetectionRegle? DetecterFrequence(RegleAntifraude regle, List<Mouvement> mouvements)
        {
            var mouvementsPeriode = FiltrerPeriode(mouvements, regle.PeriodeJours);
            var nbMouvements = mouvementsPeriode.Count;
            if (nbMouvements <= (int)regle.Seuil) return null;

            var score = Math.Min(100, 50 + (nbMouvements - (int)regle.Seuil) * 10);

            return new DetectionRegle
            {
                Score = score,
                Niveau = NiveauPourScore(score),
                Description = $"{nbMouvements} mouvements sur les {regle.PeriodeJours} derniers jours, au-delà du seuil de {regle.Seuil:N0} (règle « {regle.Nom} »).",
                DonneesJson = JsonSerializer.Serialize(new { Regle = regle.Code, NbMouvements = nbMouvements, PeriodeJours = regle.PeriodeJours })
            };
        }

        /// Structuration / smurfing : plusieurs mouvements juste sous un seuil de déclaration, sur le même compte.
        private DetectionRegle? DetecterDecoupageAnormal(RegleAntifraude regle, List<Mouvement> mouvements)
        {
            var mouvementsPeriode = FiltrerPeriode(mouvements, regle.PeriodeJours);
            var seuilBas = regle.Seuil * 0.5m;
            var seuilHaut = regle.Seuil;

            var tranches = mouvementsPeriode
                .Where(m => m.Montant >= seuilBas && m.Montant < seuilHaut)
                .ToList();

            var meilleurCompte = tranches
                .GroupBy(m => m.NumCompte)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (meilleurCompte == null || meilleurCompte.Count() < 3) return null;

            var nb = meilleurCompte.Count();
            var somme = meilleurCompte.Sum(m => m.Montant);
            var score = Math.Min(100, 50 + nb * 10 + (int)(somme / regle.Seuil) * 5);
            var dernier = meilleurCompte.OrderByDescending(m => m.DateMvt).First();

            return new DetectionRegle
            {
                IdMouvement = dernier.Id,
                NumCompte = meilleurCompte.Key,
                Agence = dernier.Agence,
                Montant = somme,
                Score = score,
                Niveau = NiveauPourScore(score),
                Description = $"Structuration suspecte : {nb} mouvements entre {seuilBas:N0} et {seuilHaut:N0} TND sur le compte {meilleurCompte.Key} ({somme:N0} TND cumulés) sur {regle.PeriodeJours} jours — découpage pouvant contourner le seuil de déclaration (règle « {regle.Nom} »).",
                DonneesJson = JsonSerializer.Serialize(new { Regle = regle.Code, Compte = meilleurCompte.Key, NbMouvements = nb, Total = somme })
            };
        }

        /// Passage de fonds : un crédit suivi d'un débit équivalent dans une fenêtre courte.
        private DetectionRegle? DetecterRapidInOut(RegleAntifraude regle, List<Mouvement> mouvements)
        {
            var mouvementsPeriode = FiltrerPeriode(mouvements, regle.PeriodeJours);
            var fenetreJours = regle.SeuilSecondaire > 0 ? (int)regle.SeuilSecondaire.Value : 2;
            var montantMin = regle.Seuil;

            var credits = mouvementsPeriode.Where(m => m.Sens != "D").ToList();
            foreach (var credit in credits.OrderByDescending(c => c.DateMvt))
            {
                if (credit.Montant < montantMin) continue;

                var debitSuivant = mouvementsPeriode
                    .Where(m => m.Sens == "D" && m.NumCompte == credit.NumCompte
                        && m.DateMvt >= credit.DateMvt
                        && m.DateMvt <= credit.DateMvt.AddDays(fenetreJours))
                    .OrderBy(m => m.DateMvt)
                    .FirstOrDefault(d => Math.Abs(d.Montant - credit.Montant) / credit.Montant <= 0.20m);

                if (debitSuivant == null) continue;

                var ecartHeures = (debitSuivant.DateMvt - credit.DateMvt).TotalHours;
                var score = Math.Min(100, 50 + (int)Math.Max(0, 48 - ecartHeures) + (credit.Montant >= montantMin * 3 ? 10 : 0));

                return new DetectionRegle
                {
                    IdMouvement = debitSuivant.Id,
                    NumCompte = credit.NumCompte,
                    Agence = debitSuivant.Agence,
                    Montant = credit.Montant,
                    Score = score,
                    Niveau = NiveauPourScore(score),
                    Description = $"Passage de fonds suspect : crédit de {credit.Montant:N0} TND le {credit.DateMvt:dd/MM/yyyy HH:mm} suivi d'un débit équivalent de {debitSuivant.Montant:N0} TND le {debitSuivant.DateMvt:dd/MM/yyyy HH:mm} (écart {ecartHeures:0} h) sur le compte {credit.NumCompte} — aller-retour de fonds (règle « {regle.Nom} »).",
                    DonneesJson = JsonSerializer.Serialize(new { Regle = regle.Code, Compte = credit.NumCompte, Montant = credit.Montant, EcartHeures = Math.Round(ecartHeures, 1) })
                };
            }

            return null;
        }

        /// Compte dormant réactivé : longue inactivité puis crédit important.
        private DetectionRegle? DetecterCompteReactive(RegleAntifraude regle, List<Mouvement> mouvements)
        {
            var joursInactivite = regle.Seuil > 0 ? (int)regle.Seuil : 180;
            var montantMinCredit = regle.SeuilSecondaire ?? 50000m;
            var seuilInactivite = DateTime.UtcNow.AddDays(-joursInactivite);

            foreach (var groupe in mouvements.GroupBy(m => m.NumCompte))
            {
                var chronologiques = groupe.OrderBy(m => m.DateMvt).ToList();
                if (chronologiques.Count < 2) continue;

                var avantDernier = chronologiques[chronologiques.Count - 2];
                var dernier = chronologiques[chronologiques.Count - 1];

                if (dernier.Sens == "D") continue;
                if (dernier.Montant < montantMinCredit) continue;
                if (avantDernier.DateMvt >= seuilInactivite) continue;

                var jours = (int)(dernier.DateMvt - avantDernier.DateMvt).TotalDays;
                var score = Math.Min(100, 60 + jours / 30);

                return new DetectionRegle
                {
                    IdMouvement = dernier.Id,
                    NumCompte = groupe.Key,
                    Agence = dernier.Agence,
                    Montant = dernier.Montant,
                    Score = score,
                    Niveau = NiveauPourScore(score),
                    Description = $"Compte réactivé : {groupe.Key} inactif depuis le {avantDernier.DateMvt:dd/MM/yyyy} ({jours} jours) reçoit un crédit de {dernier.Montant:N0} TND — réactivation d'un compte dormant (règle « {regle.Nom} »).",
                    DonneesJson = JsonSerializer.Serialize(new { Regle = regle.Code, Compte = groupe.Key, JoursInactivite = jours, Montant = dernier.Montant })
                };
            }

            return null;
        }

        /// Activité hors horaires bancaires : mouvements la nuit ou le week-end.
        private DetectionRegle? DetecterActiviteNocturne(RegleAntifraude regle, List<Mouvement> mouvements)
        {
            var mouvementsPeriode = FiltrerPeriode(mouvements, regle.PeriodeJours);
            var seuilNb = regle.Seuil > 0 ? (int)regle.Seuil : 5;
            var montantMin = regle.SeuilSecondaire ?? 0m;

            var nocturnes = mouvementsPeriode
                .Where(m => m.Montant >= montantMin &&
                    (m.DateMvt.Hour >= 22 || m.DateMvt.Hour < 6 ||
                     m.DateMvt.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday))
                .ToList();

            if (nocturnes.Count < seuilNb) return null;

            var somme = nocturnes.Sum(m => m.Montant);
            var score = Math.Min(100, 50 + (nocturnes.Count - seuilNb) * 10 + (somme > 50000 ? 15 : 0));

            return new DetectionRegle
            {
                Score = score,
                Niveau = NiveauPourScore(score),
                Montant = somme,
                Description = $"Activité inhabituelle : {nocturnes.Count} mouvement(s) hors horaires bancaires (nuit / week-end) totalisant {somme:N0} TND sur la période — activité atypique à risque (règle « {regle.Nom} »).",
                DonneesJson = JsonSerializer.Serialize(new { Regle = regle.Code, NbMouvements = nocturnes.Count, Total = somme })
            };
        }

        private static List<Mouvement> FiltrerPeriode(List<Mouvement> mouvements, int periodeJours)
        {
            if (periodeJours <= 0) return mouvements;
            var seuil = DateTime.UtcNow.AddDays(-periodeJours);
            return mouvements.Where(m => m.DateMvt >= seuil).ToList();
        }

        private static string NiveauPourScore(int score) => score switch
        {
            >= 80 => "Critique",
            >= 60 => "Haute",
            >= 40 => "Moyenne",
            _ => "Basse"
        };

        private static string ConstruireFingerprint(RegleAntifraude regle, int idClient, string? numCompte, int? idMouvement)
        {
            var suffixe = idMouvement.HasValue
                ? $"mvt-{idMouvement.Value}"
                : string.IsNullOrWhiteSpace(numCompte)
                    ? $"client-{idClient}-jour-{DateTime.UtcNow:yyyyMMdd}"
                    : $"compte-{numCompte}-{DateTime.UtcNow:yyyyMMdd}";
            return $"{regle.Id}:{idClient}:{suffixe}";
        }

        // ---------------------------------------------------------------- Lecture des alertes

        public async Task<(List<AlertesFraudeDto> Items, int Total)> GetAlertesAsync(AlertesFraudeSearchDto search)
        {
            var (items, total) = await _alerteRepository.SearchAsync(
                search.IdClient, search.Statut, search.Niveau, search.Type,
                search.DateDebut, search.DateFin, search.Page, search.PageSize);

            return (items.Select(MapAlerte).ToList(), total);
        }

        public async Task<List<AlertesFraudeDto>> GetAlertesByClientAsync(int idClient, int page = 1, int pageSize = 50)
        {
            var items = await _alerteRepository.GetByClientAsync(idClient, page, pageSize);
            return items.Select(MapAlerte).ToList();
        }

        public async Task<AlertesFraudeDto?> GetAlerteAsync(int id)
        {
            var alerte = await _alerteRepository.GetByIdAsync(id);
            return alerte == null ? null : MapAlerte(alerte);
        }

        public async Task<FraudStatsDto> GetStatsAsync()
        {
            var toutes = await _alerteRepository.GetRecentAsync(int.MaxValue);

            return new FraudStatsDto
            {
                TotalAlertes = toutes.Count,
                AlertesActives = toutes.Count(a => a.Statut != "Traitee" && a.Statut != "Ignoree"),
                Critique = toutes.Count(a => a.Niveau == "Critique"),
                Haute = toutes.Count(a => a.Niveau == "Haute"),
                Moyenne = toutes.Count(a => a.Niveau == "Moyenne"),
                Traitees = toutes.Count(a => a.Statut == "Traitee")
            };
        }

        public async Task<List<AlertesFraudeDto>> GetAlertesRecentesAsync(int count = 10)
        {
            var items = await _alerteRepository.GetRecentAsync(count);
            return items.Select(MapAlerte).ToList();
        }

        public async Task<bool> TraiterAlerteAsync(int id, string statut, string? commentaire, int utilisateurId)
        {
            var alerte = await _alerteRepository.GetByIdAsync(id);
            if (alerte == null) return false;

            if (statut is not ("Traitee" or "Ignoree" or "EnCours" or "Nouvelle"))
                throw new ArgumentException("Statut d'alerte invalide.");

            alerte.Statut = statut;
            alerte.Commentaire = commentaire;
            alerte.TraiteeParUserId = utilisateurId == 0 ? null : utilisateurId;
            alerte.DateTraitement = DateTime.UtcNow;

            await _alerteRepository.UpdateAsync(alerte);
            await _alerteRepository.SaveChangesAsync();
            return true;
        }

        // ---------------------------------------------------------------- Règles

        public async Task<List<RegleAntifraudeDto>> GetReglesAsync()
        {
            var regles = await _regleRepository.GetAllAsync();
            return regles.Select(r => new RegleAntifraudeDto
            {
                Id = r.Id,
                Code = r.Code,
                Nom = r.Nom,
                Description = r.Description,
                Type = r.Type,
                Seuil = r.Seuil,
                SeuilSecondaire = r.SeuilSecondaire,
                PeriodeJours = r.PeriodeJours,
                Poids = r.Poids,
                Actif = r.Actif,
                Priorite = r.Priorite,
                DateCreation = r.DateCreation
            }).ToList();
        }

        public async Task<RegleAntifraudeDto> CreateRegleAsync(CreateRegleAntifraudeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Nom))
                throw new ArgumentException("Code et nom obligatoires.");

            if (await _regleRepository.GetByCodeAsync(dto.Code.Trim()) != null)
                throw new ArgumentException($"Une règle avec le code « {dto.Code} » existe déjà.");

            var regle = new RegleAntifraude
            {
                Code = dto.Code.Trim(),
                Nom = dto.Nom.Trim(),
                Description = dto.Description,
                Type = dto.Type,
                Seuil = dto.Seuil,
                SeuilSecondaire = dto.SeuilSecondaire,
                PeriodeJours = dto.PeriodeJours,
                Poids = dto.Poids,
                Actif = dto.Actif,
                Priorite = dto.Priorite,
                DateCreation = DateTime.UtcNow
            };

            await _regleRepository.AddAsync(regle);
            await _regleRepository.SaveChangesAsync();

            var creee = await _regleRepository.GetByIdAsync(regle.Id);
            return MapRegle(creee!);
        }

        public async Task<RegleAntifraudeDto?> UpdateRegleAsync(int id, UpdateRegleAntifraudeDto dto)
        {
            var regle = await _regleRepository.GetByIdAsync(id);
            if (regle == null) return null;

            if (dto.Code != null) regle.Code = dto.Code.Trim();
            if (dto.Nom != null) regle.Nom = dto.Nom.Trim();
            if (dto.Description != null) regle.Description = dto.Description;
            if (dto.Type != null) regle.Type = dto.Type;
            if (dto.Seuil.HasValue) regle.Seuil = dto.Seuil.Value;
            if (dto.SeuilSecondaire.HasValue) regle.SeuilSecondaire = dto.SeuilSecondaire.Value;
            if (dto.PeriodeJours.HasValue) regle.PeriodeJours = dto.PeriodeJours.Value;
            if (dto.Poids.HasValue) regle.Poids = dto.Poids.Value;
            if (dto.Actif.HasValue) regle.Actif = dto.Actif.Value;
            if (dto.Priorite.HasValue) regle.Priorite = dto.Priorite.Value;
            regle.DateMaj = DateTime.UtcNow;

            await _regleRepository.UpdateAsync(regle);
            await _regleRepository.SaveChangesAsync();

            var maj = await _regleRepository.GetByIdAsync(id);
            return MapRegle(maj!);
        }

        public async Task<bool> ToggleRegleAsync(int id)
        {
            var regle = await _regleRepository.GetByIdAsync(id);
            if (regle == null) return false;

            regle.Actif = !regle.Actif;
            regle.DateMaj = DateTime.UtcNow;
            await _regleRepository.UpdateAsync(regle);
            await _regleRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteRegleAsync(int id)
        {
            var regle = await _regleRepository.GetByIdAsync(id);
            if (regle == null) return false;

            await _regleRepository.DeleteAsync(regle);
            await _regleRepository.SaveChangesAsync();
            return true;
        }

        // ---------------------------------------------------------------- Mappers

        private static AlertesFraudeDto MapAlerte(AlertesFraude a) => new()
        {
            Id = a.Id,
            IdClient = a.IdClient,
            CodeClient = a.Client?.CodeClient ?? string.Empty,
            NomClient = a.Client != null ? $"{a.Client.Prenom} {a.Client.Nom}" : string.Empty,
            IdRegle = a.IdRegle,
            NomRegle = a.Regle?.Nom,
            IdMouvement = a.IdMouvement,
            NumCompte = a.NumCompte,
            Agence = a.Agence,
            Montant = a.Montant,
            Type = a.Type,
            Niveau = a.Niveau,
            Score = a.Score,
            Description = a.Description,
            Statut = a.Statut,
            DonneesJson = a.DonneesJson,
            DateDetection = a.DateDetection,
            TraiteeParUserId = a.TraiteeParUserId,
            DateTraitement = a.DateTraitement,
            Commentaire = a.Commentaire
        };

        private static AlerteDetecteeDto MapAlerteDetectee(AlertesFraude a, RegleAntifraude r) => new()
        {
            Type = a.Type,
            Niveau = a.Niveau,
            Score = a.Score,
            Description = a.Description,
            NumCompte = a.NumCompte,
            Montant = a.Montant
        };

        private static RegleAntifraudeDto MapRegle(RegleAntifraude r) => new()
        {
            Id = r.Id,
            Code = r.Code,
            Nom = r.Nom,
            Description = r.Description,
            Type = r.Type,
            Seuil = r.Seuil,
            SeuilSecondaire = r.SeuilSecondaire,
            PeriodeJours = r.PeriodeJours,
            Poids = r.Poids,
            Actif = r.Actif,
            Priorite = r.Priorite,
            DateCreation = r.DateCreation
        };

        private sealed class DetectionRegle
        {
            public int? IdMouvement { get; set; }
            public string? NumCompte { get; set; }
            public string? Agence { get; set; }
            public decimal? Montant { get; set; }
            public int Score { get; set; }
            public string Niveau { get; set; } = "Moyenne";
            public string Description { get; set; } = string.Empty;
            public string? DonneesJson { get; set; }
        }
    }
}

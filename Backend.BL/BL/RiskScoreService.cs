using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Risk;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Backend.BL.BL
{
    /// <summary>
    /// Calcul du score de risque (0-100) d'un client selon un modèle bancaire pondéré :
    ///  1. Solvabilité / endettement (25 pts) : ratio charges mensuelles / revenu mensuel
    ///  2. Situation financière (15 pts) : solde vs revenus, découvert
    ///  3. Comportement de compte (15 pts) : volatilité, débits, volume
    ///  4. Ancienneté bancaire (10 pts) : durée de la relation
    ///  5. Profil socio-démographique (10 pts) : âge, situation familiale, secteur
    ///  6. Diversification des comptes (10 pts) : nombre de comptes
    ///  7. Statut du client (7 pts) : statut + type de client
    ///  8. Alertes de fraude actives (8 pts) : 3 pts par alerte (max 8)
    /// </summary>
    public class RiskScoreService : IRiskScoreService
    {
        private readonly IClientRepository _clientRepository;
        private readonly ICompteRepository _compteRepository;
        private readonly IMouvementRepository _mouvementRepository;
        private readonly IAlertesFraudeRepository _alerteRepository;
        private readonly IScoreRisqueRepository _scoreRepository;
        private readonly ILogger<RiskScoreService> _logger;

        public RiskScoreService(
            IClientRepository clientRepository,
            ICompteRepository compteRepository,
            IMouvementRepository mouvementRepository,
            IAlertesFraudeRepository alerteRepository,
            IScoreRisqueRepository scoreRepository,
            ILogger<RiskScoreService> logger)
        {
            _clientRepository = clientRepository;
            _compteRepository = compteRepository;
            _mouvementRepository = mouvementRepository;
            _alerteRepository = alerteRepository;
            _scoreRepository = scoreRepository;
            _logger = logger;
        }

        public async Task<RapportRisqueDto> CalculerAsync(int idClient)
        {
            var client = await _clientRepository.GetByIdWithDetailAsync(idClient);
            if (client == null)
                throw new KeyNotFoundException($"Client {idClient} introuvable.");

            var comptes = await _compteRepository.GetByClientIdAsync(idClient);
            var mouvements = await _mouvementRepository.GetByClientAllAsync(idClient, DateTime.UtcNow.AddDays(-90));
            var alertesActives = await _alerteRepository.CountActivesByClientAsync(idClient);

            var composants = new List<ComposantRisqueDto>();
            var points = 0;

            // 1) Solvabilité / endettement (25 pts)
            var composantEndettement = CalculerComposantEndettement(client.Detail?.RevenuMensuel, mouvements);
            composants.Add(composantEndettement);
            points += composantEndettement.Points;

            // 2) Situation financière (15 pts)
            var soldeTotal = comptes.Sum(c => c.Solde);
            var composantSolde = CalculerComposantSolde(soldeTotal, client.Detail?.RevenuMensuel);
            composants.Add(composantSolde);
            points += composantSolde.Points;

            // 3) Comportement de compte (15 pts)
            var composantMouvements = CalculerComposantMouvements(mouvements);
            composants.Add(composantMouvements);
            points += composantMouvements.Points;

            // 4) Ancienneté bancaire (10 pts)
            var composantAnciennete = CalculerComposantAnciennete(comptes, client.DateCreation);
            composants.Add(composantAnciennete);
            points += composantAnciennete.Points;

            // 5) Profil socio-démographique (10 pts)
            var composantProfil = CalculerComposantProfil(client, client.Detail);
            composants.Add(composantProfil);
            points += composantProfil.Points;

            // 6) Diversification des comptes (10 pts)
            var composantComptes = CalculerComposantComptes(comptes.Count);
            composants.Add(composantComptes);
            points += composantComptes.Points;

            // 7) Statut du client (7 pts)
            var composantStatut = CalculerComposantStatut(client.Statut, client.TypeClient);
            composants.Add(composantStatut);
            points += composantStatut.Points;

            // 8) Alertes de fraude actives (8 pts)
            var composantFraude = CalculerComposantFraude(alertesActives);
            composants.Add(composantFraude);
            points += composantFraude.Points;

            var score = Math.Clamp(points, 0, 100);
            var niveau = NiveauPourScore(score);
            var synthese = ConstruireSynthese(client, composants, alertesActives);

            var entite = new ScoreRisque
            {
                IdClient = idClient,
                Score = score,
                Niveau = niveau,
                ComposantsJson = JsonSerializer.Serialize(composants),
                Synthese = synthese,
                DateCalcul = DateTime.UtcNow
            };

            await _scoreRepository.AddAsync(entite);
            await _scoreRepository.SaveChangesAsync();

            _logger.LogInformation("Score de risque calculé pour le client {ClientId} : {Score}/100 ({Niveau}).", idClient, score, niveau);

            return new RapportRisqueDto
            {
                IdClient = idClient,
                Score = score,
                Niveau = niveau,
                Synthese = synthese,
                DateCalcul = entite.DateCalcul,
                Composants = composants
            };
        }

        public async Task<RapportRisqueDto?> GetRapportAsync(int idClient)
        {
            var dernier = await _scoreRepository.GetLatestByClientAsync(idClient);
            if (dernier == null) return null;

            var composants = string.IsNullOrWhiteSpace(dernier.ComposantsJson)
                ? new List<ComposantRisqueDto>()
                : JsonSerializer.Deserialize<List<ComposantRisqueDto>>(dernier.ComposantsJson) ?? new List<ComposantRisqueDto>();

            return new RapportRisqueDto
            {
                IdClient = idClient,
                Score = dernier.Score,
                Niveau = dernier.Niveau,
                Synthese = dernier.Synthese,
                DateCalcul = dernier.DateCalcul,
                Composants = composants
            };
        }

        public async Task<HistoriqueRisqueDto> GetHistoriqueAsync(int idClient, int page = 1, int pageSize = 50)
        {
            var items = await _scoreRepository.GetHistoriqueAsync(idClient, page, pageSize);
            return new HistoriqueRisqueDto
            {
                Items = items.Select(s => new ScoreRisqueDto
                {
                    Id = s.Id,
                    IdClient = s.IdClient,
                    Score = s.Score,
                    Niveau = s.Niveau,
                    ComposantsJson = s.ComposantsJson,
                    Synthese = s.Synthese,
                    DateCalcul = s.DateCalcul
                }).ToList(),
                Total = await _scoreRepository.CountByClientAsync(idClient)
            };
        }

        public async Task<List<ScoreRisqueDto>> GetRapportsPourClientsAsync(IEnumerable<int> idClients)
        {
            var scores = await _scoreRepository.GetLatestForClientsAsync(idClients);
            return scores.Select(s => new ScoreRisqueDto
            {
                Id = s.Id,
                IdClient = s.IdClient,
                Score = s.Score,
                Niveau = s.Niveau,
                ComposantsJson = s.ComposantsJson,
                Synthese = s.Synthese,
                DateCalcul = s.DateCalcul
            }).ToList();
        }

        // ---------------------------------------------------------------- Composants

        private static ComposantRisqueDto CalculerComposantEndettement(decimal? revenuMensuel, List<Mouvement> mouvements)
        {
            if (mouvements.Count == 0)
                return new ComposantRisqueDto { Code = "Endettement", Libelle = "Solvabilité / endettement", Points = 6, MaxPoints = 25, Detail = "Aucun mouvement sur 90 jours : capacité de remboursement indéterminée." };

            var chargesMensuelles = mouvements.Where(m => m.Sens == "D").Select(m => m.Montant).DefaultIfEmpty(0).Sum() / 3m;

            if (revenuMensuel == null || revenuMensuel <= 0)
                return new ComposantRisqueDto { Code = "Endettement", Libelle = "Solvabilité / endettement", Points = 12, MaxPoints = 25, Detail = $"Charges mensuelles estimées à {chargesMensuelles:N0} TND ; revenu non renseigné." };

            var ratio = chargesMensuelles / revenuMensuel.Value;
            var points = ratio switch
            {
                > 0.60m => 25,
                > 0.40m => 18,
                > 0.20m => 10,
                _ => 4
            };

            return new ComposantRisqueDto
            {
                Code = "Endettement",
                Libelle = "Solvabilité / endettement",
                Points = points,
                MaxPoints = 25,
                Detail = $"Charges {chargesMensuelles:N0} TND / revenu {revenuMensuel.Value:N0} TND, soit {ratio:P0} d'endettement."
            };
        }

        private static ComposantRisqueDto CalculerComposantSolde(decimal soldeTotal, decimal? revenuMensuel)
        {
            if (soldeTotal < 0)
                return new ComposantRisqueDto { Code = "Solde", Libelle = "Situation financière", Points = 15, MaxPoints = 15, Detail = $"Découvert de {soldeTotal:N0} TND." };

            if (revenuMensuel is > 0)
            {
                if (soldeTotal < revenuMensuel.Value)
                    return new ComposantRisqueDto { Code = "Solde", Libelle = "Situation financière", Points = 10, MaxPoints = 15, Detail = $"Solde ({soldeTotal:N0} TND) inférieur à un mois de revenus." };
                if (soldeTotal < revenuMensuel.Value * 3)
                    return new ComposantRisqueDto { Code = "Solde", Libelle = "Situation financière", Points = 5, MaxPoints = 15, Detail = $"Solde modéré ({soldeTotal:N0} TND)." };
                return new ComposantRisqueDto { Code = "Solde", Libelle = "Situation financière", Points = 0, MaxPoints = 15, Detail = $"Réserve de trésorerie confortable ({soldeTotal:N0} TND)." };
            }

            if (soldeTotal < 1000m)
                return new ComposantRisqueDto { Code = "Solde", Libelle = "Situation financière", Points = 10, MaxPoints = 15, Detail = $"Solde faible ({soldeTotal:N0} TND)." };
            if (soldeTotal < 10000m)
                return new ComposantRisqueDto { Code = "Solde", Libelle = "Situation financière", Points = 6, MaxPoints = 15, Detail = $"Solde modéré ({soldeTotal:N0} TND)." };
            return new ComposantRisqueDto { Code = "Solde", Libelle = "Situation financière", Points = 0, MaxPoints = 15, Detail = $"Solde confortable ({soldeTotal:N0} TND)." };
        }

        private static ComposantRisqueDto CalculerComposantMouvements(List<Mouvement> mouvements)
        {
            if (mouvements.Count == 0)
                return new ComposantRisqueDto { Code = "Mouvements", Libelle = "Comportement de compte", Points = 5, MaxPoints = 15, Detail = "Aucun mouvement sur la période (compte dormant)." };

            var montants = mouvements.Select(m => (double)m.Montant).ToList();
            var moyenne = montants.Average();
            var ecartType = Math.Sqrt(montants.Average(v => Math.Pow(v - moyenne, 2)));
            var debitMoyen = mouvements.Where(m => m.Sens == "D").Select(m => (double)m.Montant).DefaultIfEmpty(0).Average();

            var points = 0;
            var details = new List<string>();

            if (ecartType > moyenne * 3)
            {
                points += 6;
                details.Add("forte volatilité des montants");
            }
            if (debitMoyen > moyenne * 1.5)
            {
                points += 6;
                details.Add("débits élevés par rapport à la moyenne");
            }
            if (mouvements.Count > 100)
            {
                points += 3;
                details.Add("volume de mouvements important");
            }

            return new ComposantRisqueDto
            {
                Code = "Mouvements",
                Libelle = "Comportement de compte",
                Points = Math.Min(points, 15),
                MaxPoints = 15,
                Detail = details.Count > 0 ? string.Join(" ; ", details) : $"Mouvements réguliers ({mouvements.Count} sur 90 jours)."
            };
        }

        private static ComposantRisqueDto CalculerComposantAnciennete(List<Compte> comptes, DateTime dateCreationClient)
        {
            var dateDebut = comptes.Where(c => c.DateOuverture.HasValue)
                .Select(c => c.DateOuverture!.Value)
                .DefaultIfEmpty(dateCreationClient)
                .Min();

            var annees = (decimal)(DateTime.UtcNow - dateDebut).TotalDays / 365m;
            var points = annees switch
            {
                < 1 => 10,
                < 3 => 6,
                < 5 => 3,
                _ => 0
            };

            return new ComposantRisqueDto
            {
                Code = "Anciennete",
                Libelle = "Ancienneté bancaire",
                Points = points,
                MaxPoints = 10,
                Detail = $"Relation bancaire de {annees:0.#} an(s)."
            };
        }

        private static ComposantRisqueDto CalculerComposantProfil(Client client, DetailClient? detail)
        {
            var points = 0;
            var details = new List<string>();

            var age = (int)((DateTime.UtcNow - client.DateNaissance).TotalDays / 365.25);
            if (age < 25) { points += 4; details.Add($"client jeune ({age} ans)"); }
            else if (age > 65) { points += 3; details.Add($"client senior ({age} ans)"); }

            var situation = (detail?.SituationFamiliale ?? string.Empty).Trim().ToLowerInvariant();
            if (situation is "célibataire" or "celibataire" or "divorcé(e)" or "divorce(e)") { points += 3; details.Add(situation); }
            else if (situation is "veuf(ve)" or "veuf" or "veuve") { points += 2; details.Add(situation); }
            else if (situation is "marié(e)" or "marie(e)" or "en couple") { points += 1; details.Add(situation); }
            else points += 2;

            var secteur = (detail?.SecteurActivite ?? string.Empty).Trim().ToLowerInvariant();
            var secteursRisque = new[] { "jeu", "paris", "crypto", "change", "devise", "casino", "import", "commerce informel" };
            if (secteursRisque.Any(s => secteur.Contains(s, StringComparison.OrdinalIgnoreCase)))
            {
                points += 3;
                details.Add($"secteur à risque ({detail!.SecteurActivite})");
            }
            else if (string.IsNullOrWhiteSpace(secteur))
            {
                points += 2;
            }

            return new ComposantRisqueDto
            {
                Code = "Profil",
                Libelle = "Profil socio-démographique",
                Points = Math.Min(points, 10),
                MaxPoints = 10,
                Detail = details.Count > 0 ? string.Join(" ; ", details) : "Profil stable."
            };
        }

        private static ComposantRisqueDto CalculerComposantComptes(int nbComptes)
        {
            var points = nbComptes switch
            {
                <= 1 => 0,
                <= 3 => 5,
                _ => 10
            };

            return new ComposantRisqueDto
            {
                Code = "Comptes",
                Libelle = "Diversification des comptes",
                Points = points,
                MaxPoints = 10,
                Detail = $"{nbComptes} compte(s) détenu(s) par le client."
            };
        }

        private static ComposantRisqueDto CalculerComposantStatut(StatutClient statut, string? typeClient)
        {
            var pointsStatut = statut switch
            {
                StatutClient.Bloqué => 5,
                StatutClient.EnAttente => 3,
                _ => 0
            };

            var pointsType = (typeClient ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "association" => 3,
                "professionnel" => 2,
                "particulier" => 2,
                "entreprise" => 1,
                "vip" => 1,
                _ => 0
            };

            return new ComposantRisqueDto
            {
                Code = "Statut",
                Libelle = "Statut du client",
                Points = Math.Min(pointsStatut + pointsType, 7),
                MaxPoints = 7,
                Detail = $"Statut {statut}, type {typeClient ?? "non renseigné"}."
            };
        }

        private static ComposantRisqueDto CalculerComposantFraude(int alertesActives)
        {
            return new ComposantRisqueDto
            {
                Code = "Fraude",
                Libelle = "Alertes de fraude",
                Points = Math.Min(alertesActives * 3, 8),
                MaxPoints = 8,
                Detail = alertesActives == 0
                    ? "Aucune alerte de fraude active."
                    : $"{alertesActives} alerte(s) de fraude active(s)."
            };
        }

        private static string NiveauPourScore(int score) => score switch
        {
            >= 70 => "Critique",
            >= 45 => "Eleve",
            >= 25 => "Modere",
            _ => "Faible"
        };

        private static string ConstruireSynthese(Client client, List<ComposantRisqueDto> composants, int alertesActives)
        {
            var risques = composants.Where(c => c.Points > 0).OrderByDescending(c => c.Points).ToList();
            if (risques.Count == 0)
                return $"Profil de risque favorable pour {client.Prenom} {client.Nom} : aucun facteur de risque significatif identifié.";

            var facteurs = string.Join(", ", risques.Take(3).Select(r => r.Libelle.ToLowerInvariant()));
            return $"Profil {NiveauPourScore(composants.Sum(c => c.Points)).ToLowerInvariant()} pour {client.Prenom} {client.Nom} : facteurs identifiés ({facteurs})" +
                   (alertesActives > 0 ? $", dont {alertesActives} alerte(s) de fraude active(s)." : ".");
        }
    }
}

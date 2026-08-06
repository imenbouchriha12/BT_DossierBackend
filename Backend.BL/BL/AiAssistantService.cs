using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Ai;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Backend.BL.BL
{
    /// <summary>
    /// Assistant IA conversationnel (approche RAG simplifiée) :
    ///  1. Classifie la question en langage naturel vers une intention structurée (LLM).
    ///  2. Exécute l'intention sur les données applicatives (récupération de faits).
    ///  3. Génère la réponse en français avec les faits (LLM), avec repli déterministe.
    /// </summary>
    public class AiAssistantService : IAiAssistantService
    {
        private const string RoleSystem = "system";

        private readonly ILlmApiService _llmApi;
        private readonly IClientRepository _clientRepository;
        private readonly ICompteRepository _compteRepository;
        private readonly IMouvementRepository _mouvementRepository;
        private readonly IAlertesFraudeRepository _alerteRepository;
        private readonly IScoreRisqueRepository _scoreRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IReminderRepository _reminderRepository;
        private readonly ClientStatusManager _clientStatusManager;
        private readonly ILogger<AiAssistantService> _logger;

        public AiAssistantService(
            ILlmApiService llmApi,
            IClientRepository clientRepository,
            ICompteRepository compteRepository,
            IMouvementRepository mouvementRepository,
            IAlertesFraudeRepository alerteRepository,
            IScoreRisqueRepository scoreRepository,
            IDocumentRepository documentRepository,
            IUserRepository userRepository,
            IReminderRepository reminderRepository,
            ClientStatusManager clientStatusManager,
            ILogger<AiAssistantService> logger)
        {
            _llmApi = llmApi;
            _clientRepository = clientRepository;
            _compteRepository = compteRepository;
            _mouvementRepository = mouvementRepository;
            _alerteRepository = alerteRepository;
            _scoreRepository = scoreRepository;
            _documentRepository = documentRepository;
            _userRepository = userRepository;
            _reminderRepository = reminderRepository;
            _clientStatusManager = clientStatusManager;
            _logger = logger;
        }

        // ---------------------------------------------------------------- Chat

        public async Task<ChatResponseDto> ChatAsync(ChatRequestDto request)
        {
            var message = request.Message?.Trim();
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Le message est vide.");

            // 1) Classification de l'intention
            var intention = await ClassifierIntentionAsync(message, request.ClientId);

            // 2) Récupération des faits (contexte RAG)
            var contexte = new List<DonneeContextuelleDto>();
            var faits = await ExecuterIntentionAsync(intention, contexte);

            // 3) Génération de la réponse
            var reponseLlm = await GenererReponseAsync(intention, message, faits);
            var mode = "llm";

            if (string.IsNullOrWhiteSpace(reponseLlm))
            {
                reponseLlm = GenererReponseFallback(intention, faits);
                mode = "fallback";
            }

            return new ChatResponseDto
            {
                Reponse = reponseLlm,
                Mode = mode,
                Contexte = contexte.Count > 0 ? contexte : null
            };
        }

        // ---------------------------------------------------------------- Résumé de dossier

        public async Task<ClientSummaryDto> ResumerClientAsync(int idClient)
        {
            var client = await _clientRepository.GetByIdWithDetailAsync(idClient);
            if (client == null)
                throw new KeyNotFoundException($"Client {idClient} introuvable.");

            var comptes = await _compteRepository.GetByClientIdAsync(idClient);
            var soldeTotal = comptes.Sum(c => c.Solde);
            var statutReport = await _clientStatusManager.GetStatusReportAsync(idClient);
            var alertesActives = await _alerteRepository.CountActivesByClientAsync(idClient);
            var scoreRisque = await _scoreRepository.GetLatestByClientAsync(idClient);

            var dto = new ClientSummaryDto
            {
                IdClient = idClient,
                CodeClient = client.CodeClient,
                NomComplet = $"{client.Prenom} {client.Nom}",
                TypeClient = client.TypeClient,
                Statut = client.Statut == StatutClient.EnAttente ? "En attente" : client.Statut.ToString(),
                SoldeTotal = soldeTotal,
                NbComptes = comptes.Count,
                NbDocumentsManquants = statutReport.DocumentsManquants.Count,
                NbAlertesActives = alertesActives,
                NiveauRisque = scoreRisque?.Niveau,
                ScoreRisque = scoreRisque?.Score,
                Mode = "template"
            };

            // Faits structurés pour le LLM
            var faits = new List<string>
            {
                $"Client {dto.NomComplet} ({dto.CodeClient}), type {dto.TypeClient ?? "non renseigné"}, statut {dto.Statut}.",
                $"Situation financière : solde total {dto.SoldeTotal:N0} TND sur {dto.NbComptes} compte(s).",
                $"Documents : {statutReport.DocumentsValides} validé(s), {statutReport.DocumentsEnAttente} en attente, {dto.NbDocumentsManquants} manquant(s){FormaterDocumentsManquants(statutReport.DocumentsManquants)}.",
                $"Conformité : {dto.NbAlertesActives} alerte(s) de fraude active(s).",
                $"Risque : score {dto.ScoreRisque?.ToString() ?? "non calculé"}/100 (niveau {dto.NiveauRisque ?? "non calculé"})."
            };

            var resumeLlm = await _llmApi.CompleteAsync(
                "Tu es un assistant bancaire de BT DocCenter. Rédige un résumé de dossier client en FRANÇAIS en exactement 5 lignes, une par fait (statut, situation financière, documents manquants, alertes, risque). Concise, professionnelle, sans introduction ni conclusion.",
                string.Join("\n", faits),
                temperature: 0.3,
                maxTokens: 400);

            if (!string.IsNullOrWhiteSpace(resumeLlm))
            {
                dto.Resume = resumeLlm;
                dto.Mode = "llm";
            }
            else
            {
                dto.Resume = string.Join("\n", faits.Select(f => "• " + f.TrimStart('•', ' ')));
            }

            return dto;
        }

        private static string FormaterDocumentsManquants(List<string> manquants)
        {
            if (manquants.Count == 0) return string.Empty;
            return $" ({string.Join(", ", manquants.Take(3))}{(manquants.Count > 3 ? "…" : "")})";
        }

        // ---------------------------------------------------------------- Classification

        private sealed class Intention
        {
            public string Intent { get; set; } = "general";
            public int? ClientId { get; set; }
            public string? Nom { get; set; }
            public string? Code { get; set; }
            public string? Type { get; set; }
            public string? Statut { get; set; }
        }

        private async Task<Intention> ClassifierIntentionAsync(string message, int? clientId)
        {
            var systemPrompt = """
                Tu es un assistant d'analyse pour une banque (BT DocCenter).
                L'utilisateur pose une question en français concernant les clients, comptes, mouvements, documents, fraude ou risque.
                Réponds UNIQUEMENT par un objet JSON valide, sans texte ni balises markdown, au format :
                {"intent":"clients_stats","clientId":null,"nom":"","code":"","type":"","statut":""}
                Intents possibles :
                - clients_stats : comptage de clients, répartition par statut (filtres optionnels type, statut)
                - agents : nombre d'agents et de superviseurs, leurs noms et matricules
                - taches : tâches et rappels planifiés, par utilisateur (qui doit faire quoi, échéances)
                - statistiques : vue d'ensemble chiffrée avec pourcentages (clients, comptes, documents, alertes, tâches, agents)
                - client_recherche : recherche d'un client par nom ou code
                - client_detail : informations détaillées d'un client (clientId ou nom requis)
                - comptes : comptes et soldes d'un client précis, ou statistiques globales des comptes
                - mouvements : mouvements récents d'un client
                - documents : statut des documents d'un client précis, ou statistiques globales des documents
                - fraude : alertes de fraude ou anomalies
                - risque : score de risque d'un client
                - general : toute autre question
                Renseigne clientId si le client est clairement identifié, sinon nom/code pour le rechercher.
                """;

            var json = await _llmApi.CompleteAsync(systemPrompt, message, temperature: 0, maxTokens: 300);

            var intention = ExtraireIntentionJson(json);
            if (intention == null)
                intention = ClassifierIntentionFallback(message);

            intention.ClientId ??= clientId;
            return intention;
        }

        private static Intention? ExtraireIntentionJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                var debut = json.IndexOf('{');
                var fin = json.LastIndexOf('}');
                if (debut < 0 || fin <= debut) return null;

                var payload = json.Substring(debut, fin - debut + 1);
                using var doc = JsonDocument.Parse(payload);
                var racine = doc.RootElement;

                var intention = new Intention
                {
                    Intent = LireString(racine, "intent") ?? "general",
                    Nom = LireString(racine, "nom"),
                    Code = LireString(racine, "code"),
                    Type = LireString(racine, "type"),
                    Statut = LireString(racine, "statut")
                };

                if (racine.TryGetProperty("clientId", out var cid) && cid.ValueKind == JsonValueKind.Number && cid.TryGetInt32(out var id))
                    intention.ClientId = id;

                return intention;
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                return null;
            }
        }

        private static string? LireString(JsonElement racine, string propriete)
        {
            if (!racine.TryGetProperty(propriete, out var elem)) return null;
            if (elem.ValueKind != JsonValueKind.String) return null;
            var valeur = elem.GetString();
            return string.IsNullOrWhiteSpace(valeur) ? null : valeur;
        }

        private static Intention ClassifierIntentionFallback(string message)
        {
            var m = message.ToLowerInvariant();
            var intention = new Intention();

            if (Contient(m, "statistique", "statistiques", "stats", "pourcentage", "pourcent", "bilan", "chiffre", "chiffres", "vue d'ensemble"))
                intention.Intent = "statistiques";
            else if (Contient(m, "tache", "tâche", "taches", "tâches", "rappel", "reminder", "agenda", "échéance", "echeance", "echeances"))
                intention.Intent = "taches";
            else if (Contient(m, "agent", "agents", "employé", "employes", "équipe", "equipe", "utilisateur", "personnel", "superviseur", "superviseurs"))
                intention.Intent = "agents";
            else if (Contient(m, "fraude", "anomal", "alerte"))
                intention.Intent = "fraude";
            else if (Contient(m, "risque", "score"))
                intention.Intent = "risque";
            else if (Contient(m, "document", "papier", "pièce", "pièces"))
                intention.Intent = "documents";
            else if (Contient(m, "mouvement", "opération", "opérations", "débit", "crédit", "transaction"))
                intention.Intent = "mouvements";
            else if (Contient(m, "compte", "solde", "soldes"))
                intention.Intent = "comptes";
            else if (Contient(m, "combien", "nombre", "compte de", "nb ", "total de", "statut", "status", "répartition", "repartition"))
                intention.Intent = "clients_stats";
            else if (Contient(m, "client", "dossier"))
                intention.Intent = "client_detail";
            else
                intention.Intent = "general";

            // Détection grossière d'un type de client
            if (Contient(m, "vip")) intention.Type = "VIP";
            else if (Contient(m, "entreprise")) intention.Type = "Entreprise";
            else if (Contient(m, "professionnel")) intention.Type = "Professionnel";
            else if (Contient(m, "association")) intention.Type = "Association";
            else if (Contient(m, "particulier")) intention.Type = "Particulier";

            if (Contient(m, "en attente", "attente")) intention.Statut = "En attente";
            else if (Contient(m, "bloqué", "bloque", "bloqués", "bloques")) intention.Statut = "Bloqué";
            else if (Contient(m, "actif", "actifs")) intention.Statut = "Actif";

            return intention;
        }

        private static bool Contient(string texte, params string[] mots)
        {
            foreach (var mot in mots)
            {
                if (texte.Contains(mot, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- Exécution des intentions

        private async Task<string> ExecuterIntentionAsync(Intention intention, List<DonneeContextuelleDto> contexte)
        {
            int? clientId = intention.ClientId;

            var intentNecessiteClient = intention.Intent is "client_detail" or "mouvements" or "risque";
            if (intentNecessiteClient)
            {
                if (!clientId.HasValue && !string.IsNullOrWhiteSpace(intention.Nom))
                    clientId = await RechercherClientAsync(intention.Nom, intention.Code);
                if (!clientId.HasValue && !string.IsNullOrWhiteSpace(intention.Code))
                    clientId = await RechercherClientAsync(null, intention.Code);
                intention.ClientId = clientId;
            }

            switch (intention.Intent)
            {
                case "clients_stats":
                    return await FaitsStatsClientsAsync(intention, contexte);
                case "agents":
                    return await FaitsAgentsAsync(contexte);
                case "taches":
                    return await FaitsTachesAsync(contexte);
                case "statistiques":
                    return await FaitsStatistiquesAsync(contexte);
                case "client_recherche":
                    return await FaitsRechercheClientAsync(intention, contexte);
                case "client_detail":
                    return await FaitsDetailClientAsync(clientId, contexte);
                case "comptes":
                    return await FaitsComptesAsync(clientId, contexte);
                case "mouvements":
                    return await FaitsMouvementsAsync(clientId, contexte);
                case "documents":
                    return await FaitsDocumentsAsync(clientId, contexte);
                case "fraude":
                    return await FaitsFraudeAsync(intention, contexte);
                case "risque":
                    return await FaitsRisqueAsync(clientId, contexte);
                default:
                    return string.Empty;
            }
        }

        private async Task<int?> RechercherClientAsync(string? nom, string? code)
        {
            var clients = await _clientRepository.GetAllAsync();
            if (!string.IsNullOrWhiteSpace(code))
            {
                var parCode = clients.FirstOrDefault(c => c.CodeClient.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase));
                if (parCode != null) return parCode.Id;
            }
            if (!string.IsNullOrWhiteSpace(nom))
            {
                var parNom = clients.FirstOrDefault(c =>
                    $"{c.Prenom} {c.Nom}".Contains(nom, StringComparison.OrdinalIgnoreCase) ||
                    c.Nom.Contains(nom, StringComparison.OrdinalIgnoreCase));
                if (parNom != null) return parNom.Id;
            }
            return null;
        }

        private async Task<string> FaitsStatsClientsAsync(Intention intention, List<DonneeContextuelleDto> contexte)
        {
            StatutClient? statut = intention.Statut?.ToLowerInvariant() switch
            {
                "actif" or "actifs" => StatutClient.Actif,
                "bloqué" or "bloque" or "bloqués" or "bloques" => StatutClient.Bloqué,
                "en attente" or "attente" or "en attente de documents" => StatutClient.EnAttente,
                _ => null
            };

            var total = await _clientRepository.CountByTypeAndStatutAsync(intention.Type, statut);

            var libelleStatut = statut switch
            {
                StatutClient.Actif => "actifs",
                StatutClient.Bloqué => "bloqués",
                StatutClient.EnAttente => "en attente",
                _ => ""
            };
            var type = intention.Type ?? "clients";
            var libelle = $"{type} {libelleStatut}".Trim();

            contexte.Add(new DonneeContextuelleDto
            {
                Type = "clients_stats",
                Libelle = $"Nombre de {libelle}",
                Valeur = total.ToString()
            });

            var details = "";
            if (statut == null && intention.Type == null)
            {
                var actifs = await _clientRepository.CountByTypeAndStatutAsync(null, StatutClient.Actif);
                var bloques = await _clientRepository.CountByTypeAndStatutAsync(null, StatutClient.Bloqué);
                var enAttente = await _clientRepository.CountByTypeAndStatutAsync(null, StatutClient.EnAttente);
                details = $" Statuts : Actif={actifs} ({Pourcent(actifs, total)}%), Bloqué={bloques} ({Pourcent(bloques, total)}%), En attente={enAttente} ({Pourcent(enAttente, total)}%).";
                contexte.Add(new DonneeContextuelleDto
                {
                    Type = "clients_stats",
                    Libelle = "Clients par statut",
                    Valeur = $"Actif: {actifs} ({Pourcent(actifs, total)}%), Bloqué: {bloques} ({Pourcent(bloques, total)}%), En attente: {enAttente} ({Pourcent(enAttente, total)}%)"
                });
            }

            return $"Clients: total={total}; filtre={libelle}.{details}";
        }

        private async Task<string> FaitsAgentsAsync(List<DonneeContextuelleDto> contexte)
        {
            var agents = await _userRepository.GetByRoleAsync("Agent");
            var superviseurs = await _userRepository.GetByRoleAsync("Superviseur");

            contexte.Add(new DonneeContextuelleDto { Type = "agents", Libelle = "Nombre d'agents", Valeur = agents.Count.ToString() });
            contexte.Add(new DonneeContextuelleDto { Type = "agents", Libelle = "Nombre de superviseurs", Valeur = superviseurs.Count.ToString() });
            contexte.Add(new DonneeContextuelleDto
            {
                Type = "agents",
                Libelle = "Agents actifs",
                Valeur = agents.Count == 0 ? "Aucun" : string.Join(", ", agents.Select(a => $"{a.Username} ({a.Matricule})"))
            });
            contexte.Add(new DonneeContextuelleDto
            {
                Type = "agents",
                Libelle = "Superviseurs",
                Valeur = superviseurs.Count == 0 ? "Aucun" : string.Join(", ", superviseurs.Select(s => $"{s.Username} ({s.Matricule})"))
            });

            var nomAgents = agents.Count == 0 ? "aucun" : string.Join("; ", agents.Select(a => $"{a.Username} ({a.Matricule}, {a.Email})"));
            var nomSuperviseurs = superviseurs.Count == 0 ? "aucun" : string.Join("; ", superviseurs.Select(s => $"{s.Username} ({s.Matricule})"));
            return $"Agents: total={agents.Count}; noms={nomAgents}. Superviseurs: total={superviseurs.Count}; noms={nomSuperviseurs}.";
        }

        private async Task<string> FaitsTachesAsync(List<DonneeContextuelleDto> contexte)
        {
            var taches = (await _reminderRepository.GetAllTeamAsync(null, null, null, null, null, null, null))
                .Where(r => r.Status != "Termine" && r.Status != "Annule")
                .OrderBy(r => r.DueDate)
                .ToList();

            var total = taches.Count;
            var enRetard = taches.Count(r => r.DueDate < DateTime.UtcNow);

            contexte.Add(new DonneeContextuelleDto { Type = "taches", Libelle = "Tâches ouvertes (équipe)", Valeur = total.ToString() });
            contexte.Add(new DonneeContextuelleDto { Type = "taches", Libelle = "Tâches en retard", Valeur = enRetard.ToString() });

            var parUtilisateur = taches
                .GroupBy(r => r.AssignedToUser?.Username ?? "Non assigné")
                .Select(g => $"{g.Key}: {g.Count()} tâche(s)")
                .ToList();

            contexte.Add(new DonneeContextuelleDto { Type = "taches", Libelle = "Tâches par utilisateur", Valeur = string.Join(", ", parUtilisateur) });

            var prochaines = taches.Take(8).Select(r =>
                $"{r.Title} ({(r.AssignedToUser?.Username ?? "non assigné")}) échéance {r.DueDate:dd/MM/yyyy}");

            return $"Taches: total={total}; enRetard={enRetard}. Par utilisateur : {string.Join(", ", parUtilisateur)}. Prochaines : {string.Join("; ", prochaines)}.";
        }

        private async Task<string> FaitsRechercheClientAsync(Intention intention, List<DonneeContextuelleDto> contexte)
        {
            var id = await RechercherClientAsync(intention.Nom, intention.Code);
            if (!id.HasValue)
            {
                contexte.Add(new DonneeContextuelleDto { Type = "client", Libelle = "Client recherché", Valeur = "Aucun résultat" });
                return "Recherche: aucun client trouvé.";
            }

            var client = await _clientRepository.GetByIdWithDetailAsync(id.Value);
            if (client == null) return "Recherche: aucun client trouvé.";

            contexte.Add(new DonneeContextuelleDto { Type = "client", Libelle = "Client", Valeur = $"{client.Prenom} {client.Nom} ({client.CodeClient})" });
            return $"Client: {client.Prenom} {client.Nom} ({client.CodeClient}), statut {client.Statut}, type {client.TypeClient ?? "non renseigné"}, ville {client.Ville}.";
        }

        private async Task<string> FaitsDetailClientAsync(int? clientId, List<DonneeContextuelleDto> contexte)
        {
            if (!clientId.HasValue)
            {
                contexte.Add(new DonneeContextuelleDto { Type = "client", Libelle = "Client", Valeur = "Non identifié" });
                return "Client non identifié.";
            }

            var client = await _clientRepository.GetByIdWithDetailAsync(clientId.Value);
            if (client == null) return "Client non identifié.";

            var statut = client.Statut == StatutClient.EnAttente ? "En attente" : client.Statut.ToString();
            contexte.Add(new DonneeContextuelleDto { Type = "client", Libelle = "Client", Valeur = $"{client.Prenom} {client.Nom} ({client.CodeClient})" });
            contexte.Add(new DonneeContextuelleDto { Type = "client", Libelle = "Statut", Valeur = statut });
            contexte.Add(new DonneeContextuelleDto { Type = "client", Libelle = "Type", Valeur = client.TypeClient ?? "non renseigné" });
            contexte.Add(new DonneeContextuelleDto { Type = "client", Libelle = "Ville", Valeur = client.Ville });

            return $"Client {client.Prenom} {client.Nom} ({client.CodeClient}) : statut {statut}, type {client.TypeClient ?? "non renseigné"}, né(e) le {client.DateNaissance:dd/MM/yyyy}, {client.Adresse}, {client.Ville}, téléphone {client.Telephone}, email {client.Email}.";
        }

        private async Task<string> FaitsComptesAsync(int? clientId, List<DonneeContextuelleDto> contexte)
        {
            if (!clientId.HasValue)
                return await FaitsComptesStatsAsync(contexte);

            var comptes = await _compteRepository.GetByClientIdAsync(clientId.Value);
            var soldeTotal = comptes.Sum(c => c.Solde);

            contexte.Add(new DonneeContextuelleDto { Type = "comptes", Libelle = "Nombre de comptes", Valeur = comptes.Count.ToString() });
            contexte.Add(new DonneeContextuelleDto { Type = "comptes", Libelle = "Solde total (TND)", Valeur = soldeTotal.ToString("N0") });

            var lignes = comptes.Select(c => $"{c.NumCompte} ({c.Agence}) : {c.Solde:N0} {c.Devise}");
            return $"Comptes ({comptes.Count}) : {string.Join(" ; ", lignes)}. Solde total {soldeTotal:N0} TND.";
        }

        private async Task<string> FaitsComptesStatsAsync(List<DonneeContextuelleDto> contexte)
        {
            var comptes = await _compteRepository.GetAllAsync();
            var total = comptes.Count;
            var soldeTotal = comptes.Sum(c => c.Solde);

            contexte.Add(new DonneeContextuelleDto { Type = "comptes", Libelle = "Nombre total de comptes", Valeur = total.ToString() });
            contexte.Add(new DonneeContextuelleDto { Type = "comptes", Libelle = "Solde total (TND)", Valeur = soldeTotal.ToString("N0") });

            var parAgence = comptes.GroupBy(c => c.Agence).OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key}: {g.Count()} compte(s) ({Pourcent(g.Count(), total)}%)").ToList();
            var parDevise = comptes.GroupBy(c => c.Devise).OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key}: {g.Count()} compte(s) ({Pourcent(g.Count(), total)}%) pour {g.Sum(x => x.Solde):N0} TND").ToList();

            return $"Comptes: total={total}; solde={soldeTotal:N0} TND. Par agence : {string.Join(", ", parAgence)}. Par devise : {string.Join(", ", parDevise)}.";
        }

        private async Task<string> FaitsMouvementsAsync(int? clientId, List<DonneeContextuelleDto> contexte)
        {
            if (!clientId.HasValue)
            {
                contexte.Add(new DonneeContextuelleDto { Type = "mouvements", Libelle = "Mouvements", Valeur = "Client non identifié" });
                return "Client non identifié.";
            }

            var mouvements = await _mouvementRepository.GetByClientAllAsync(clientId.Value, DateTime.UtcNow.AddDays(-90));
            var recents = mouvements.OrderByDescending(m => m.DateMvt).Take(10).ToList();

            contexte.Add(new DonneeContextuelleDto { Type = "mouvements", Libelle = "Mouvements sur 90 jours", Valeur = mouvements.Count.ToString() });

            var lignes = recents.Select(m => $"{m.DateMvt:dd/MM/yyyy} : {m.Montant:N0} {m.DevMVT} ({(m.Sens == "D" ? "débit" : "crédit")}) compte {m.NumCompte}");
            return $"Mouvements récents ({recents.Count} affichés / {mouvements.Count} total) : {string.Join(" ; ", lignes)}.";
        }

        private async Task<string> FaitsDocumentsAsync(int? clientId, List<DonneeContextuelleDto> contexte)
        {
            if (!clientId.HasValue)
                return await FaitsDocumentsStatsAsync(contexte);

            var rapport = await _clientStatusManager.GetStatusReportAsync(clientId.Value);

            contexte.Add(new DonneeContextuelleDto { Type = "documents", Libelle = "Documents validés", Valeur = rapport.DocumentsValides.ToString() });
            contexte.Add(new DonneeContextuelleDto { Type = "documents", Libelle = "Documents en attente", Valeur = rapport.DocumentsEnAttente.ToString() });
            contexte.Add(new DonneeContextuelleDto { Type = "documents", Libelle = "Documents manquants", Valeur = rapport.DocumentsManquants.Count.ToString() });

            return $"Documents : {rapport.DocumentsValides} validé(s), {rapport.DocumentsEnAttente} en attente, {rapport.DocumentsRejetes} rejeté(s), {rapport.DocumentsManquants.Count} manquant(s) ({string.Join(", ", rapport.DocumentsManquants)}).";
        }

        private async Task<string> FaitsDocumentsStatsAsync(List<DonneeContextuelleDto> contexte)
        {
            var documents = await _documentRepository.GetAllAsync();
            var total = documents.Count;

            contexte.Add(new DonneeContextuelleDto { Type = "documents", Libelle = "Nombre total de documents", Valeur = total.ToString() });

            var parStatut = documents.GroupBy(d => d.StatutValidation).OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key}: {g.Count()} ({Pourcent(g.Count(), total)}%)").ToList();

            return $"Documents: total={total}. Par statut : {string.Join(", ", parStatut)}.";
        }

        private async Task<string> FaitsStatistiquesAsync(List<DonneeContextuelleDto> contexte)
        {
            var clients = await _clientRepository.GetAllAsync();
            var totalClients = clients.Count;

            var parStatut = clients.GroupBy(c => c.Statut).OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key}: {g.Count()} ({Pourcent(g.Count(), totalClients)}%)").ToList();
            var parType = clients.GroupBy(c => c.TypeClient).OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key}: {g.Count()} ({Pourcent(g.Count(), totalClients)}%)").ToList();

            var comptes = await _compteRepository.GetAllAsync();
            var totalComptes = comptes.Count;
            var soldeTotal = comptes.Sum(c => c.Solde);

            var documents = await _documentRepository.GetAllAsync();
            var totalDocuments = documents.Count;
            var parStatutDoc = documents.GroupBy(d => d.StatutValidation).OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key}: {g.Count()} ({Pourcent(g.Count(), totalDocuments)}%)").ToList();

            var alertesActives = await _alerteRepository.CountActivesAsync();

            var taches = (await _reminderRepository.GetAllTeamAsync(null, null, null, null, null, null, null))
                .Where(r => r.Status != "Termine" && r.Status != "Annule").ToList();
            var tachesRetard = taches.Count(r => r.DueDate < DateTime.UtcNow);

            var agents = await _userRepository.GetByRoleAsync("Agent");
            var superviseurs = await _userRepository.GetByRoleAsync("Superviseur");

            contexte.Add(new DonneeContextuelleDto { Type = "statistiques", Libelle = "Clients", Valeur = totalClients.ToString() });
            contexte.Add(new DonneeContextuelleDto { Type = "statistiques", Libelle = "Clients par statut", Valeur = string.Join(", ", parStatut) });
            contexte.Add(new DonneeContextuelleDto { Type = "statistiques", Libelle = "Clients par type", Valeur = string.Join(", ", parType) });
            contexte.Add(new DonneeContextuelleDto { Type = "statistiques", Libelle = "Comptes", Valeur = $"{totalComptes} (solde total {soldeTotal:N0} TND)" });
            contexte.Add(new DonneeContextuelleDto { Type = "statistiques", Libelle = "Documents", Valeur = string.Join(", ", parStatutDoc) });
            contexte.Add(new DonneeContextuelleDto { Type = "statistiques", Libelle = "Alertes de fraude actives", Valeur = alertesActives.ToString() });
            contexte.Add(new DonneeContextuelleDto { Type = "statistiques", Libelle = "Tâches ouvertes", Valeur = $"{taches.Count} (dont {tachesRetard} en retard)" });
            contexte.Add(new DonneeContextuelleDto { Type = "statistiques", Libelle = "Équipe", Valeur = $"{agents.Count} agent(s), {superviseurs.Count} superviseur(s)" });

            return $"Statistiques: clients_total={totalClients}; clients_par_statut={string.Join(", ", parStatut)}; " +
                   $"clients_par_type={string.Join(", ", parType)}; comptes_total={totalComptes}; solde_total={soldeTotal:N0} TND; " +
                   $"documents_total={totalDocuments}; documents_par_statut={string.Join(", ", parStatutDoc)}; " +
                   $"alertes_actives={alertesActives}; taches_ouvertes={taches.Count}; taches_retard={tachesRetard}; " +
                   $"agents={agents.Count}; superviseurs={superviseurs.Count}.";
        }

        private static decimal Pourcent(int partie, int total)
        {
            if (total <= 0) return 0;
            return Math.Round((decimal)partie * 100 / total, 1);
        }

        private async Task<string> FaitsFraudeAsync(Intention intention, List<DonneeContextuelleDto> contexte)
        {
            var actives = await _alerteRepository.CountActivesAsync();
            var critiques = await _alerteRepository.GetRecentAsync(5);

            contexte.Add(new DonneeContextuelleDto { Type = "fraude", Libelle = "Alertes de fraude actives", Valeur = actives.ToString() });

            var lignes = critiques
                .Where(a => a.Statut != "Traitee" && a.Statut != "Ignoree")
                .Select(a => $"{a.Client?.Prenom} {a.Client?.Nom} : {a.Description}");
            return $"Alertes de fraude actives : {actives}. Dernières : {string.Join(" ; ", lignes)}.";
        }

        private async Task<string> FaitsRisqueAsync(int? clientId, List<DonneeContextuelleDto> contexte)
        {
            if (!clientId.HasValue)
            {
                contexte.Add(new DonneeContextuelleDto { Type = "risque", Libelle = "Risque", Valeur = "Client non identifié" });
                return "Client non identifié.";
            }

            var score = await _scoreRepository.GetLatestByClientAsync(clientId.Value);
            if (score == null)
            {
                contexte.Add(new DonneeContextuelleDto { Type = "risque", Libelle = "Risque", Valeur = "Non calculé" });
                return $"Score de risque : non calculé pour le client {clientId.Value}.";
            }

            contexte.Add(new DonneeContextuelleDto { Type = "risque", Libelle = "Score de risque (/100)", Valeur = score.Score.ToString() });
            contexte.Add(new DonneeContextuelleDto { Type = "risque", Libelle = "Niveau de risque", Valeur = score.Niveau });

            return $"Score de risque du client {clientId.Value} : {score.Score}/100, niveau {score.Niveau}. Synthèse : {score.Synthese}.";
        }

        // ---------------------------------------------------------------- Génération de réponse

        private async Task<string?> GenererReponseAsync(Intention intention, string message, string faits)
        {
            var systemPrompt = """
                Tu es un assistant IA bancaire pour les agents de BT DocCenter.
                Tu réponds en FRANÇAIS, de manière concise et professionnelle, UNIQUEMENT à partir des données fournies après « DONNEES : ».
                Si aucune donnée n'est fournie, réponds de façon générale mais utile.
                Formate les montants en dinars (TND) avec séparateur de milliers.
                N'invente jamais de chiffres : base-toi exclusivement sur les données fournies.
                """;

            var userPrompt = $"Question de l'agent : {message}\n\nDONNEES :\n{faits}";

            return await _llmApi.CompleteAsync(systemPrompt, userPrompt, temperature: 0.4, maxTokens: 500);
        }

        private static string GenererReponseFallback(Intention intention, string faits)
        {
            if (string.IsNullOrWhiteSpace(faits))
            {
                return "Je n'ai pas trouvé de données correspondant à votre question. Essayez par exemple : « combien de clients VIP sont en attente ? », « montrez-moi les alertes de fraude », ou « résumez le dossier du client X ».";
            }

            switch (intention.Intent)
            {
                case "clients_stats":
                {
                    var total = ExtraireValeurFait(faits, "total") ?? "0";
                    var type = intention.Type ?? "clients";
                    var statut = (intention.Statut ?? "").ToLowerInvariant();
                    var libelle = statut.Length > 0 ? $"{type} {statut}" : type;
                    return $"Il y a actuellement {total} {libelle} dans la base.";
                }
                case "agents":
                {
                    var total = ExtraireValeurFait(faits, "total") ?? "0";
                    var noms = ExtraireValeurFait(faits, "noms");
                    if (string.IsNullOrEmpty(noms) || noms == "aucun")
                        return $"Il y a actuellement {total} agent(s) enregistré(s) dans la base.";
                    return $"Il y a actuellement {total} agent(s) enregistré(s) : {noms}.";
                }
                case "taches":
                {
                    var total = ExtraireValeurFait(faits, "total") ?? "0";
                    var enRetard = ExtraireValeurFait(faits, "enRetard") ?? "0";
                    return $"Il y a actuellement {total} tâche(s) ouverte(s) dans l'équipe (dont {enRetard} en retard).";
                }
                case "statistiques":
                    return $"Voici les statistiques de la base : {faits.TrimEnd('.')}.";
                case "fraude":
                    return $"Voici la situation antifraude : {faits.TrimEnd('.')}.";
                case "client_recherche":
                    return faits.Contains("aucun", StringComparison.OrdinalIgnoreCase)
                        ? "Je n'ai trouvé aucun client correspondant à cette recherche."
                        : $"{faits.TrimEnd('.')}.";
                default:
                    return $"{faits.TrimEnd('.')}.";
            }
        }

        private static string? ExtraireValeurFait(string faits, string cle)
        {
            var idx = faits.IndexOf($"{cle}=", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            var debut = idx + cle.Length + 1;
            var fin = faits.IndexOf(';', debut);
            if (fin < 0) fin = faits.IndexOf('.', debut);
            if (fin < 0) fin = faits.Length;
            return faits.Substring(debut, fin - debut).Trim();
        }
    }
}

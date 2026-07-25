using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.BL.IBL;
using Microsoft.Extensions.Logging;

namespace Backend.BL.BL
{
    /// <summary>
    /// Gère automatiquement le statut des clients en fonction de leurs documents
    /// </summary>
    public class ClientStatusManager
    {
        private readonly IClientRepository _clientRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<ClientStatusManager> _logger;
        private readonly IHistoriqueService _historiqueService;
        private readonly ICurrentUserService _currentUser;

        // Documents obligatoires pour qu'un client soit "Actif"
        private static readonly HashSet<string> DOCUMENTS_REQUIS = new()
        {
            "CIN", // Carte d'identité
            "Justificatif de domicile",
            "Photo",
            "Fiche de renseignements"
        };

        public ClientStatusManager(
            IClientRepository clientRepository,
            IDocumentRepository documentRepository,
            ILogger<ClientStatusManager> logger,
            IHistoriqueService historiqueService,
            ICurrentUserService currentUser)
        {
            _clientRepository = clientRepository;
            _documentRepository = documentRepository;
            _logger = logger;
            _historiqueService = historiqueService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Recalcule et met à jour automatiquement le statut d'un client
        /// en fonction de ses documents
        /// </summary>
        public async Task<StatutClient> RecalculerStatutClientAsync(int clientId)
        {
            var client = await _clientRepository.GetByIdAsync(clientId);
            if (client == null)
            {
                throw new KeyNotFoundException($"Client {clientId} introuvable");
            }

            // Si le client est bloqué, on ne change pas automatiquement (décision admin)
            if (client.Statut == StatutClient.Bloqué)
            {
                _logger.LogInformation("Client {ClientId} est bloqué - pas de changement automatique", clientId);
                return client.Statut;
            }

            var documents = await _documentRepository.GetByClientIdAsync(clientId);
            var nouveauStatut = DeterminerStatut(documents);

            // Mise à jour si nécessaire
            if (client.Statut != nouveauStatut)
            {
                _logger.LogInformation(
                    "Changement automatique du statut du client {ClientId}: {AncienStatut} → {NouveauStatut}",
                    clientId, client.Statut, nouveauStatut);

                client.Statut = nouveauStatut;
                await _clientRepository.SaveChangesAsync();
            }

            return nouveauStatut;
        }

        /// <summary>
        /// Vérifie si le client a tous les documents requis validés
        /// </summary>
        private StatutClient DeterminerStatut(IEnumerable<DocumentClient> documents)
        {
            var docsValides = documents
                .Where(d => d.StatutValidation == "Validé")
                .Select(d => d.TypeDocument)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Tous les documents requis sont-ils validés ?
            var tousValidés = DOCUMENTS_REQUIS.All(requis => 
                docsValides.Contains(requis));

            if (tousValidés)
            {
                return StatutClient.Actif;
            }

            // Au moins un document requis validé
            var auMoinsUnValide = DOCUMENTS_REQUIS.Any(requis => 
                docsValides.Contains(requis));

            if (auMoinsUnValide)
            {
                return StatutClient.EnAttente; // Dossier en cours
            }

            // Aucun document validé
            return StatutClient.EnAttente; // Nouveau client
        }

        /// <summary>
        /// Obtient les documents manquants pour un client
        /// </summary>
        public async Task<List<string>> GetDocumentsManquantsAsync(int clientId)
        {
            var documents = await _documentRepository.GetByClientIdAsync(clientId);
            var docsValides = documents
                .Where(d => d.StatutValidation == "Validé")
                .Select(d => d.TypeDocument)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return DOCUMENTS_REQUIS
                .Where(requis => !docsValides.Contains(requis))
                .ToList();
        }

        /// <summary>
        /// Obtient un rapport détaillé du statut d'un client
        /// </summary>
        public async Task<ClientStatusReportDto> GetStatusReportAsync(int clientId)
        {
            var client = await _clientRepository.GetByIdAsync(clientId);
            if (client == null)
            {
                throw new KeyNotFoundException($"Client {clientId} introuvable");
            }

            var documents = await _documentRepository.GetByClientIdAsync(clientId);
            var documentsManquants = await GetDocumentsManquantsAsync(clientId);

            var docsParStatut = documents
                .GroupBy(d => d.StatutValidation)
                .ToDictionary(g => g.Key, g => g.Count());

            return new ClientStatusReportDto
            {
                ClientId = clientId,
                CodeClient = client.CodeClient,
                StatutActuel = client.Statut.ToString(),
                DocumentsRequis = DOCUMENTS_REQUIS.Count,
                DocumentsValides = documents.Count(d => d.StatutValidation == "Validé"),
                DocumentsEnAttente = documents.Count(d => d.StatutValidation == "En attente"),
                DocumentsRejetes = documents.Count(d => d.StatutValidation == "Rejeté"),
                DocumentsManquants = documentsManquants,
                PeutEtreActif = documentsManquants.Count == 0,
                DerniereMiseAJour = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Bloque manuellement un client (décision administrative)
        /// </summary>
        public async Task BloquerClientAsync(int clientId, string raison)
        {
            var client = await _clientRepository.GetByIdAsync(clientId);
            if (client == null)
            {
                throw new KeyNotFoundException($"Client {clientId} introuvable");
            }

            var ancienStatut = client.Statut.ToString();

            _logger.LogWarning(
                "Blocage manuel du client {ClientId}: {Raison}",
                clientId, raison);

            client.Statut = StatutClient.Bloqué;
            client.MotifBlocage = raison;
            await _clientRepository.SaveChangesAsync();

            await _historiqueService.EnregistrerAsync(
                "Clients", clientId, "Modification",
                $"Statut: {ancienStatut}",
                $"Statut: Bloqué — Motif: {raison}",
                _currentUser.UserId, _currentUser.UserName);
        }

        /// <summary>
        /// Débloque un client
        /// </summary>
        public async Task DebloquerClientAsync(int clientId)
        {
            var client = await _clientRepository.GetByIdAsync(clientId);
            if (client == null)
            {
                throw new KeyNotFoundException($"Client {clientId} introuvable");
            }

            var ancienStatut = client.Statut.ToString();
            var ancienMotif = client.MotifBlocage;

            _logger.LogInformation("Déblocage du client {ClientId}", clientId);

            client.MotifBlocage = null;
            await _clientRepository.SaveChangesAsync();

            // Recalculer le statut en fonction des documents
            var nouveauStatut = await RecalculerStatutClientAsync(clientId);

            await _historiqueService.EnregistrerAsync(
                "Clients", clientId, "Modification",
                $"Statut: {ancienStatut}" + (ancienMotif != null ? $" — Motif: {ancienMotif}" : ""),
                $"Statut: {nouveauStatut} — Débloqué",
                _currentUser.UserId, _currentUser.UserName);
        }
    }

    /// <summary>
    /// Rapport détaillé du statut d'un client
    /// </summary>
    public class ClientStatusReportDto
    {
        public int ClientId { get; set; }
        public string CodeClient { get; set; } = string.Empty;
        public string StatutActuel { get; set; } = string.Empty;
        public int DocumentsRequis { get; set; }
        public int DocumentsValides { get; set; }
        public int DocumentsEnAttente { get; set; }
        public int DocumentsRejetes { get; set; }
        public List<string> DocumentsManquants { get; set; } = new();
        public bool PeutEtreActif { get; set; }
        public DateTime DerniereMiseAJour { get; set; }
    }
}

using Backend.DTO.Portal;

namespace Backend.BL.IBL
{
    public interface IClientPortalService
    {
        // ── Admin operations ──────────────────────────────────────────────────

        /// Génère un token sécurisé, révoque les anciens et envoie l'email d'invitation au client
        Task<GeneratePortalLinkResponseDto> GenerateAndSendPortalLinkAsync(int idClient, int dureeJours = 7);

        /// Révoque manuellement tous les tokens actifs d'un client
        Task RevokePortalAccessAsync(int idClient);

        /// Retourne l'état du token actif d'un client (pour l'admin)
        Task<ClientPortalTokenDto?> GetActiveTokenInfoAsync(int idClient);

        // ── Portal (unauthenticated) operations ───────────────────────────────

        /// Valide un token et retourne le clientId associé
        Task<ValidatePortalTokenResultDto> ValidateTokenAsync(string token, string ip);

        /// Vue complète du portail : documents requis + statut de chaque upload
        Task<ClientPortalViewDto> GetPortalViewAsync(string token, string ip);

        /// Upload d'un document depuis le portail client
        Task<PortalUploadResultDto> UploadDocumentAsync(string token, string typeDocument, string fileName, byte[] fileBytes, string ip);

        // ── Dashboard ─────────────────────────────────────────────────────────

        /// Taux de complétion documentaire par client (avec filtres optionnels)
        Task<DocumentCompletionSummaryDto> GetCompletionSummaryAsync(string? typeClient = null, string? ville = null);
    }
}

using System.ComponentModel.DataAnnotations;

namespace Backend.DTO.Portal
{
    // ── Token management ──────────────────────────────────────────────────────

    public class ClientPortalTokenDto
    {
        public int Id { get; set; }
        public int IdClient { get; set; }
        public string CodeClient { get; set; } = string.Empty;
        public string NomComplet { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public DateTime DateExpiration { get; set; }
        public bool EstRevoque { get; set; }
        public bool EstExpire => DateTime.UtcNow > DateExpiration;
        public int NbAcces { get; set; }
        public DateTime? DernierAcces { get; set; }
    }

    /// Réponse retournée à l'admin après génération d'un lien portail
    public class GeneratePortalLinkResponseDto
    {
        public string PortalLink { get; set; } = string.Empty;
        public DateTime DateExpiration { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool EmailEnvoye { get; set; }
    }

    /// Corps de la requête pour générer/renvoyer un lien portail
    public class SendPortalInviteDto
    {
        /// Durée de validité en jours (par défaut 7)
        [Range(1, 30, ErrorMessage = "La durée doit être entre 1 et 30 jours")]
        public int DureeJours { get; set; } = 7;
    }

    // ── Portal upload ─────────────────────────────────────────────────────────

    /// Résultat d'un upload depuis le portail client
    public class PortalUploadResultDto
    {
        public int DocumentId { get; set; }
        public string NomDocument { get; set; } = string.Empty;
        public string TypeDocument { get; set; } = string.Empty;
        public string StatutValidation { get; set; } = string.Empty;
        public DateTime DateUpload { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ── Token validation (internal) ───────────────────────────────────────────

    public class ValidatePortalTokenResultDto
    {
        public bool IsValid { get; set; }
        public int? IdClient { get; set; }
        public string? RaisonInvalidite { get; set; }
    }
}

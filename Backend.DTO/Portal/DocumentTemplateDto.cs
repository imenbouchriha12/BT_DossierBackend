using System.ComponentModel.DataAnnotations;

namespace Backend.DTO.Portal
{
    // ── Read ──────────────────────────────────────────────────────────────────

    public class DocumentTemplateDto
    {
        public int Id { get; set; }
        public string TypeClient { get; set; } = string.Empty;
        public string NomDocument { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool EstObligatoire { get; set; }
        public int Ordre { get; set; }
        public string? FormatsAcceptes { get; set; }
        public int? TailleMaxMo { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateCreation { get; set; }
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public class CreateDocumentTemplateDto
    {
        [Required(ErrorMessage = "Le type de client est requis")]
        [MaxLength(50)]
        public string TypeClient { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom du document est requis")]
        [MaxLength(100)]
        public string NomDocument { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool EstObligatoire { get; set; } = true;
        public int Ordre { get; set; } = 0;

        [MaxLength(200)]
        public string? FormatsAcceptes { get; set; }

        public int? TailleMaxMo { get; set; }
    }

    // ── Update ────────────────────────────────────────────────────────────────

    public class UpdateDocumentTemplateDto
    {
        [MaxLength(100)]
        public string? NomDocument { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool? EstObligatoire { get; set; }
        public int? Ordre { get; set; }

        [MaxLength(200)]
        public string? FormatsAcceptes { get; set; }

        public int? TailleMaxMo { get; set; }
        public bool? IsActive { get; set; }
    }

    // ── Portal view ───────────────────────────────────────────────────────────

    /// Résumé d'un document requis tel qu'affiché au client sur le portail
    public class RequiredDocumentStatusDto
    {
        public int TemplateId { get; set; }
        public string NomDocument { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool EstObligatoire { get; set; }
        public int Ordre { get; set; }
        public string? FormatsAcceptes { get; set; }
        public int? TailleMaxMo { get; set; }

        /// null = non soumis, "En attente" / "Validé" / "Rejeté"
        public string? Statut { get; set; }

        /// ID du DocumentClient uploadé (null si pas encore soumis)
        public int? DocumentId { get; set; }

        /// Date d'upload (null si pas encore soumis)
        public DateTime? DateUpload { get; set; }
    }

    /// Vue complète du portail client
    public class ClientPortalViewDto
    {
        public string CodeClient { get; set; } = string.Empty;
        public string NomComplet { get; set; } = string.Empty;
        public string TypeClient { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        /// Taux de complétion global (0-100)
        public int TauxCompletion { get; set; }

        /// Nombre de documents obligatoires fournis / total
        public int ObligatoiresFournis { get; set; }
        public int ObligatoiresTotal { get; set; }

        public List<RequiredDocumentStatusDto> Documents { get; set; } = new();
    }

    // ── Completion dashboard ──────────────────────────────────────────────────

    public class ClientDocumentCompletionDto
    {
        public int IdClient { get; set; }
        public string CodeClient { get; set; } = string.Empty;
        public string NomComplet { get; set; } = string.Empty;
        public string TypeClient { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Ville { get; set; } = string.Empty;
        public int TauxCompletion { get; set; }
        public int DocumentsFournis { get; set; }
        public int DocumentsTotal { get; set; }
        public int DocumentsObligatoiresFournis { get; set; }
        public int DocumentsObligatoiresTotal { get; set; }
        public int DocumentsEnAttente { get; set; }
        public int DocumentsValides { get; set; }
        public int DocumentsRejetes { get; set; }
        public bool PortalInviteEnvoye { get; set; }
        public DateTime? DernierAccesPortal { get; set; }
    }

    public class DocumentCompletionSummaryDto
    {
        public int TotalClients { get; set; }
        public int ClientsComplets { get; set; }           // 100 %
        public int ClientsEnCours { get; set; }            // 1-99 %
        public int ClientsNonCommences { get; set; }        // 0 %
        public double TauxMoyenCompletion { get; set; }
        public List<ClientDocumentCompletionDto> Clients { get; set; } = new();
    }
}

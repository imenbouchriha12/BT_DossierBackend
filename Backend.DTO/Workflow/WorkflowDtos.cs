namespace Backend.DTO.Workflow
{
    public class EtapeValidationDto
    {
        public int Id { get; set; }
        public int IdWorkflow { get; set; }
        public int Ordre { get; set; }
        public string RoleRequis { get; set; } = string.Empty;
        public string Libelle { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string? NomUser { get; set; }
        public DateTime? DecisionDate { get; set; }
        public string? Commentaire { get; set; }
    }

    public class WorkflowValidationDto
    {
        public int Id { get; set; }
        public int IdDocument { get; set; }
        public string StatutGlobal { get; set; } = string.Empty;
        public string? EtapeCourante { get; set; }
        public string? MotifRejet { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime DateMaj { get; set; }
        public List<EtapeValidationDto> Etapes { get; set; } = new();

        // Informations du document lié (lecture)
        public string? TypeDocument { get; set; }
        public string? NomFichier { get; set; }
        public string? StatutValidation { get; set; }
        public int? IdClient { get; set; }
        public string? CodeClient { get; set; }
        public string? NomClient { get; set; }
        public string? UploadedByNom { get; set; }
    }

    public class ValidateWorkflowDto
    {
        public string? Commentaire { get; set; }

        /// Dérogation explicite d'un Superviseur/Admin pour une étape qui ne lui est pas assignée.
        public bool Derogation { get; set; }
    }

    public class RejectWorkflowDto
    {
        public string Commentaire { get; set; } = string.Empty;

        /// Dérogation explicite d'un Superviseur/Admin pour une étape qui ne lui est pas assignée.
        public bool Derogation { get; set; }
    }

    public class ResubmitWorkflowDto
    {
        public string? Commentaire { get; set; }
    }

    public class WorkflowStatsDto
    {
        /// Nombre de workflows en attente pour l'utilisateur courant (selon ses rôles)
        public int TotalEnAttente { get; set; }

        /// Répartition par rôle (tous utilisateurs confondus)
        public Dictionary<string, int> ParRole { get; set; } = new();
    }
}

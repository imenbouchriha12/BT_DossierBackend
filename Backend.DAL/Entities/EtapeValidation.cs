using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// <summary>
    /// Étape de validation d'un workflow : un rôle requis + l'exécution de la décision.
    /// Statuts possibles : EnAttente | Valide | Rejete | NonApplicable
    /// </summary>
    [Table("EtapeValidation", Schema = "dbo")]
    public class EtapeValidation
    {
        [Key]
        [Column("IdEtape")]
        public int Id { get; set; }

        [Column("IdWorkflow")]
        public int IdWorkflow { get; set; }

        /// Ordre d'exécution dans la chaîne (1 = Agent, 2 = Superviseur, 3 = Admin)
        [Column("Ordre")]
        public int Ordre { get; set; }

        /// Rôle requis pour valider cette étape
        [Column("RoleRequis")]
        [MaxLength(30)]
        public string RoleRequis { get; set; } = string.Empty;

        /// Libellé affichable (ex: "Validation Superviseur")
        [Column("Libelle")]
        [MaxLength(100)]
        public string Libelle { get; set; } = string.Empty;

        /// EnAttente | Valide | Rejete | NonApplicable
        [Column("Statut")]
        [MaxLength(20)]
        public string Statut { get; set; } = "EnAttente";

        /// Utilisateur ayant pris la décision
        [Column("UserId")]
        public int? UserId { get; set; }

        [Column("NomUser")]
        [MaxLength(200)]
        public string? NomUser { get; set; }

        [Column("DecisionDate")]
        public DateTime? DecisionDate { get; set; }

        [Column("Commentaire")]
        [MaxLength(1000)]
        public string? Commentaire { get; set; }

        [ForeignKey("IdWorkflow")]
        public WorkflowValidation Workflow { get; set; } = null!;

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}

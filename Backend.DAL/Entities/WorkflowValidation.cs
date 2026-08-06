using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// <summary>
    /// Instance de workflow de validation multi-niveaux liée à un document.
    /// Chaque workflow possède une liste d'étapes (EtapeValidation) qui doivent être
    /// validées successivement (Agent → Superviseur → Admin) avant que le document
    /// soit considéré comme validé.
    /// Un document peut avoir plusieurs workflows dans son historique (rejets / relances).
    /// </summary>
    [Table("WorkflowValidation", Schema = "dbo")]
    public class WorkflowValidation
    {
        [Key]
        [Column("IdWorkflow")]
        public int Id { get; set; }

        [Column("IdDocument")]
        public int IdDocument { get; set; }

        /// Statut global : EnCours | Valide | Rejete
        [Column("StatutGlobal")]
        [MaxLength(20)]
        public string StatutGlobal { get; set; } = "EnCours";

        /// Rôle attendu pour l'étape en cours (Agent, Superviseur, Admin) — null si terminé
        [Column("EtapeCourante")]
        [MaxLength(30)]
        public string? EtapeCourante { get; set; }

        /// Motif du rejet (obligatoire lors d'un rejet)
        [Column("MotifRejet")]
        [MaxLength(1000)]
        public string? MotifRejet { get; set; }

        /// Utilisateur à l'origine de la soumission (null si upload via portail client)
        [Column("CreatedByUserId")]
        public int? CreatedByUserId { get; set; }

        [Column("DateCreation")]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [Column("DateMaj")]
        public DateTime DateMaj { get; set; } = DateTime.UtcNow;

        [ForeignKey("IdDocument")]
        public DocumentClient Document { get; set; } = null!;

        [ForeignKey("CreatedByUserId")]
        public User? CreatedBy { get; set; }

        public ICollection<EtapeValidation> Etapes { get; set; } = new List<EtapeValidation>();
    }

    /// <summary>
    /// Constantes partagées du workflow de validation documentaire.
    /// </summary>
    public static class WorkflowRoles
    {
        public const string Agent = "Agent";
        public const string Superviseur = "Superviseur";
        public const string Admin = "Admin";

        /// Chaîne de validation dans l'ordre (du plus bas au plus haut niveau)
        public static readonly string[] Chaine = { Agent, Superviseur, Admin };

        /// Rang d'un rôle dans la chaîne (1 = Agent, 3 = Admin, 0 = inconnu)
        public static int Rang(string role) => role switch
        {
            Agent => 1,
            Superviseur => 2,
            Admin => 3,
            _ => 0
        };
    }
}

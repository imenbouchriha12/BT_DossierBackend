using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// <summary>
    /// Template de document requis pour un type de client donné.
    /// Exemple : Particulier → CIN, Justificatif de domicile, Fiche de paie
    /// </summary>
    [Table("DocumentTemplates", Schema = "dbo")]
    public class DocumentTemplate
    {
        [Key]
        [Column("IdTemplate")]
        public int Id { get; set; }

        /// Type de client concerné (Particulier, Entreprise, VIP, etc.)
        [Column("TypeClient")]
        [MaxLength(50)]
        [Required]
        public string TypeClient { get; set; } = string.Empty;

        /// Nom du type de document requis (ex: CIN, Fiche de paie)
        [Column("NomDocument")]
        [MaxLength(100)]
        [Required]
        public string NomDocument { get; set; } = string.Empty;

        /// Description ou instructions pour le client
        [Column("Description")]
        [MaxLength(500)]
        public string? Description { get; set; }

        /// Indique si ce document est obligatoire ou optionnel
        [Column("EstObligatoire")]
        public bool EstObligatoire { get; set; } = true;

        /// Ordre d'affichage dans la liste
        [Column("Ordre")]
        public int Ordre { get; set; } = 0;

        /// Formats acceptés (ex: "pdf,jpg,png") — null = tous
        [Column("FormatsAcceptes")]
        [MaxLength(200)]
        public string? FormatsAcceptes { get; set; }

        /// Taille maximale en Mo (null = pas de limite spécifique)
        [Column("TailleMaxMo")]
        public int? TailleMaxMo { get; set; }

        /// Indique si ce template est actif
        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        [Column("DateCreation")]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// <summary>
    /// Contact associé à un dossier client (conjoint, garant, référence, héritier...).
    /// </summary>
    [Table("ContactClient", Schema = "dbo")]
    public class ContactClient
    {
        [Key]
        [Column("IdContact")]
        public int Id { get; set; }

        [Column("IdClient")]
        public int IdClient { get; set; }

        /// Type : Conjoint, Garant, Reference, Heritier, Autre
        [Column("TypeContact")]
        [MaxLength(30)]
        public string TypeContact { get; set; } = "Autre";

        [Column("Nom")]
        [MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Column("Prenom")]
        [MaxLength(100)]
        public string? Prenom { get; set; }

        [Column("Telephone")]
        [MaxLength(30)]
        public string? Telephone { get; set; }

        [Column("Email")]
        [MaxLength(150)]
        public string? Email { get; set; }

        [Column("Adresse")]
        [MaxLength(300)]
        public string? Adresse { get; set; }

        /// Nature du lien avec le client (ex: père, conjoint, employeur)
        [Column("Relation")]
        [MaxLength(100)]
        public string? Relation { get; set; }

        [Column("DateCreation")]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [ForeignKey("IdClient")]
        public Client Client { get; set; } = null!;
    }
}

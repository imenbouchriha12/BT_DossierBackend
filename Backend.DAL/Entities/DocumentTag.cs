using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// <summary>
    /// Table des tags pour les documents
    /// </summary>
    [Table("DocumentTags", Schema = "dbo")]
    public class DocumentTag
    {
        [Key]
        [Column("IdTag")]
        public int Id { get; set; }

        [Column("IdDocument")]
        public int IdDocument { get; set; }

        [Column("Tag")]
        [MaxLength(100)]
        public string Tag { get; set; } = string.Empty;

        [Column("CreatedBy")]
        public int CreatedBy { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("IdDocument")]
        public DocumentClient Document { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        public User User { get; set; } = null!;
    }
}

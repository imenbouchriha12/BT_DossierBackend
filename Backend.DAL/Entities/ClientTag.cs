using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// <summary>
    /// Table des tags pour les clients
    /// </summary>
    [Table("ClientTags", Schema = "dbo")]
    public class ClientTag
    {
        [Key]
        [Column("IdTag")]
        public int Id { get; set; }

        [Column("IdClient")]
        public int IdClient { get; set; }

        [Column("Tag")]
        [MaxLength(100)]
        public string Tag { get; set; } = string.Empty;

        [Column("CreatedBy")]
        public int CreatedBy { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("IdClient")]
        public Client Client { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        public User User { get; set; } = null!;
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    [Table("Notifications", Schema = "dbo")]
    public class Notification
    {
        [Key]
        [Column("IdNotification")]
        public int Id { get; set; }

        [Column("UserId")]
        public int UserId { get; set; }

        [Column("Titre")]
        [MaxLength(200)]
        public string Titre { get; set; } = string.Empty;

        [Column("Message")]
        public string Message { get; set; } = string.Empty;

        [Column("Type")]
        [MaxLength(50)]
        public string Type { get; set; } = "Info";

        [Column("Lien")]
        [MaxLength(500)]
        public string? Lien { get; set; }

        [Column("EstLu")]
        public bool EstLu { get; set; } = false;

        [Column("DateCreation")]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}

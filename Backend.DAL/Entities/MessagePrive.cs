using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// Message privé en temps réel entre utilisateurs de l'application (Admin, Agent, Superviseur).
    [Table("MessagesPrives", Schema = "dbo")]
    public class MessagePrive
    {
        [Key]
        [Column("IdMessage")]
        public int Id { get; set; }

        [Column("ExpediteurUserId")]
        public int ExpediteurUserId { get; set; }

        [Column("DestinataireUserId")]
        public int DestinataireUserId { get; set; }

        [Column("Contenu")]
        [MaxLength(4000)]
        public string Contenu { get; set; } = string.Empty;

        [Column("PieceJointeUrl")]
        [MaxLength(500)]
        public string? PieceJointeUrl { get; set; }

        [Column("PieceJointeNom")]
        [MaxLength(255)]
        public string? PieceJointeNom { get; set; }

        [Column("PieceJointeType")]
        [MaxLength(50)]
        public string? PieceJointeType { get; set; }

        [Column("DateEnvoi")]
        public DateTime DateEnvoi { get; set; } = DateTime.UtcNow;

        [Column("Lu")]
        public bool Lu { get; set; } = false;

        [Column("DateLecture")]
        public DateTime? DateLecture { get; set; }

        [ForeignKey("ExpediteurUserId")]
        public User? Expediteur { get; set; }

        [ForeignKey("DestinataireUserId")]
        public User? Destinataire { get; set; }
    }
}

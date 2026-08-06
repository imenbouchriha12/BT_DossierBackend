using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    public enum ReminderType
    {
        RenouvellementDocument,
        AppelSuivi,
        RendezVous,
        Tache,
        Autre
    }

    public enum ReminderPriority
    {
        Basse,
        Normale,
        Haute,
        Critique
    }

    public enum ReminderStatus
    {
        EnAttente,
        EnCours,
        Termine,
        Annule
    }

    [Table("Reminders", Schema = "dbo")]
    public class Reminder
    {
        [Key]
        [Column("IdReminder")]
        public int Id { get; set; }

        [Column("Title")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Column("Description")]
        public string? Description { get; set; }

        [Column("Type")]
        [MaxLength(30)]
        public string Type { get; set; } = "Autre";

        [Column("Priority")]
        [MaxLength(20)]
        public string Priority { get; set; } = "Normale";

        [Column("Status")]
        [MaxLength(20)]
        public string Status { get; set; } = "EnAttente";

        [Column("DueDate")]
        public DateTime DueDate { get; set; }

        [Column("CompletedAt")]
        public DateTime? CompletedAt { get; set; }

        [Column("NotifiedAt")]
        public DateTime? NotifiedAt { get; set; }

        [Column("ClientId")]
        public int? ClientId { get; set; }

        [Column("AssignedToUserId")]
        public int? AssignedToUserId { get; set; }

        [Column("CreatedByUserId")]
        public int CreatedByUserId { get; set; }

        [Column("DateCreation")]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [ForeignKey("ClientId")]
        public Client? Client { get; set; }

        [ForeignKey("AssignedToUserId")]
        public User? AssignedToUser { get; set; }

        [ForeignKey("CreatedByUserId")]
        public User? CreatedByUser { get; set; }
    }
}

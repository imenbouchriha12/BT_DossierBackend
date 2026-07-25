using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// <summary>
    /// Entité pour l'historique des recherches
    /// </summary>
    [Table("SearchHistory", Schema = "dbo")]
    public class SearchHistory
    {
        [Key]
        [Column("IdSearchHistory")]
        public int Id { get; set; }

        [Column("UserId")]
        public int UserId { get; set; }

        [Column("SearchQuery")]
        [MaxLength(500)]
        public string SearchQuery { get; set; } = string.Empty;

        [Column("SearchType")]
        [MaxLength(50)]
        public string SearchType { get; set; } = string.Empty; // Global, Client, Document, Advanced

        [Column("ResultsCount")]
        public int ResultsCount { get; set; }

        [Column("SearchDate")]
        public DateTime SearchDate { get; set; } = DateTime.UtcNow;

        [Column("ExecutionTimeMs")]
        public int ExecutionTimeMs { get; set; }

        [Column("Filters")]
        public string? Filters { get; set; } // JSON des filtres appliqués

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// <summary>
    /// Score de risque d'un client avec historique des calculs.
    /// </summary>
    [Table("ScoreRisque", Schema = "dbo")]
    public class ScoreRisque
    {
        [Key]
        [Column("IdScore")]
        public int Id { get; set; }

        [Column("IdClient")]
        public int IdClient { get; set; }

        /// Score global 0-100 (100 = risque maximum)
        [Column("Score")]
        public int Score { get; set; }

        /// Niveau : Faible, Modere, Eleve, Critique
        [Column("Niveau")]
        [MaxLength(20)]
        public string Niveau { get; set; } = "Faible";

        /// Décomposition du score (JSON : solde, mouvements, situation familiale, type client...)
        [Column("ComposantsJson")]
        public string? ComposantsJson { get; set; }

        /// Synthèse des principaux facteurs (texte)
        [Column("Synthese")]
        [MaxLength(2000)]
        public string Synthese { get; set; } = string.Empty;

        [Column("DateCalcul")]
        public DateTime DateCalcul { get; set; } = DateTime.UtcNow;

        [ForeignKey("IdClient")]
        public Client Client { get; set; } = null!;
    }
}

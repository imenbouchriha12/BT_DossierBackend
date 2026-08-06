using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// <summary>
    /// Règle de détection de fraude / anomalie, entièrement configurable.
    /// </summary>
    [Table("RegleAntifraude", Schema = "dbo")]
    public class RegleAntifraude
    {
        [Key]
        [Column("IdRegle")]
        public int Id { get; set; }

        /// Code court unique de la règle (ex : MONTANT_ANORMAL)
        [Column("Code")]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        /// Libellé affichable
        [Column("Nom")]
        [MaxLength(150)]
        public string Nom { get; set; } = string.Empty;

        [Column("Description")]
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// Type de règle : MontantMaximal, MontantAnormal, PicDebit, MultiComptes, MultiAgences, Frequence
        [Column("Type")]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        /// Seuil principal (montant, multiplicateur ou nombre selon le type)
        [Column("Seuil")]
        public decimal Seuil { get; set; }

        /// Seuil secondaire optionnel (ex : moyenne historique, période)
        [Column("SeuilSecondaire")]
        public decimal? SeuilSecondaire { get; set; }

        /// Fenêtre d'analyse en jours
        [Column("PeriodeJours")]
        public int PeriodeJours { get; set; } = 90;

        /// Poids de la règle dans le score global (0-100)
        [Column("Poids")]
        public int Poids { get; set; } = 10;

        [Column("Actif")]
        public bool Actif { get; set; } = true;

        /// Priorité d'affichage
        [Column("Priorite")]
        public int Priorite { get; set; } = 0;

        [Column("DateCreation")]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [Column("DateMaj")]
        public DateTime? DateMaj { get; set; }
    }
}

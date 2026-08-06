using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// Mouvement bancaire alimenté par le SISS (lecture seule).
    [Table("MVT", Schema = "dbo")]
    public class Mouvement
    {
        [Key]
        [Column("IdMouvement")]
        public int Id { get; set; }

        [Column("NumCompte")]
        [MaxLength(20)]
        public string NumCompte { get; set; } = string.Empty;

        [Column("Agence")]
        [MaxLength(10)]
        public string Agence { get; set; } = string.Empty;

        [Column("Montant")]
        public decimal Montant { get; set; }

        [Column("DateMvt")]
        public DateTime DateMvt { get; set; }

        [Column("DevMVT")]
        [MaxLength(3)]
        public string DevMVT { get; set; } = string.Empty;

        [Column("Sens")]
        [MaxLength(1)]
        public string Sens { get; set; } = string.Empty;
    }
}

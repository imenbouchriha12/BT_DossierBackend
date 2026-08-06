using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// Compte bancaire alimenté par le SISS (lecture seule côté application).
    [Table("Compte", Schema = "dbo")]
    public class Compte
    {
        [Key]
        [Column("IdCompte")]
        public int Id { get; set; }

        [Column("IdClient")]
        public int IdClient { get; set; }

        [Column("NumCompte")]
        [MaxLength(20)]
        public string NumCompte { get; set; } = string.Empty;

        [Column("Agence")]
        [MaxLength(10)]
        public string Agence { get; set; } = string.Empty;

        [Column("Devise")]
        [MaxLength(3)]
        public string Devise { get; set; } = string.Empty;

        [Column("Solde")]
        public decimal Solde { get; set; }

        [Column("SoldeContreValeur")]
        public decimal SoldeContreValeur { get; set; }

        /// Date d'ouverture du compte (renseignée lors de la synchronisation SISS).
        [Column("DateOuverture")]
        public DateTime? DateOuverture { get; set; }
    }
}

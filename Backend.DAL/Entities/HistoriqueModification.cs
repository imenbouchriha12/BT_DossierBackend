using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    [Table("HistoriqueModifications", Schema = "dbo")]
    public class HistoriqueModification
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("TableModifiee")]
        public string TableModifiee { get; set; } = string.Empty;

        [Column("EntiteId")]
        public int EntiteId { get; set; }

        [Column("Action")]
        public string Action { get; set; } = string.Empty;

        [Column("AncienneValeur")]
        public string? AncienneValeur { get; set; }

        [Column("NouvelleValeur")]
        public string? NouvelleValeur { get; set; }

        [Column("UtilisateurId")]
        public int UtilisateurId { get; set; }

        [Column("UtilisateurNom")]
        public string UtilisateurNom { get; set; } = string.Empty;

        [Column("DateHeure")]
        public DateTime DateHeure { get; set; }
    }
}

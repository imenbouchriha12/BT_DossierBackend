using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// Cours de change quotidien alimenté par le SISS (lecture seule).
    [Table("Cours", Schema = "dbo")]
    public class Cours
    {
        [Key]
        [Column("IdCours")]
        public int Id { get; set; }

        [Column("Journee")]
        public DateTime Journee { get; set; }

        [Column("Devise")]
        [MaxLength(3)]
        public string Devise { get; set; } = string.Empty;

        [Column("Cours")]
        public decimal Valeur { get; set; }

        [Column("Multiplicateur")]
        public int Multiplicateur { get; set; }
    }
}

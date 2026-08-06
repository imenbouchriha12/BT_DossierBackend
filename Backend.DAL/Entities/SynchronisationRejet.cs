using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// Lignes rejetées lors d'une synchronisation SISS (motif + contenu brut pour reprise).
    [Table("SynchronisationRejet")]
    public class SynchronisationRejet
    {
        [Key]
        public int Id { get; set; }

        [Column("IdSynchronisation")]
        public int IdSynchronisation { get; set; }

        [MaxLength(50)]
        public string Fichier { get; set; } = string.Empty;

        public int? NumeroLigne { get; set; }

        public string Motif { get; set; } = string.Empty;

        public string ContenuBrut { get; set; } = string.Empty;

        [ForeignKey("IdSynchronisation")]
        public SynchronisationLog Synchronisation { get; set; } = null!;
    }
}

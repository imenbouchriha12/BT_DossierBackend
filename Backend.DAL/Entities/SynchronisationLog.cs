using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// Journal des exécutions de synchronisation application ↔ SISS (traçabilité bancaire).
    [Table("SynchronisationLog")]
    public class SynchronisationLog
    {
        [Key]
        public int Id { get; set; }

        public DateTime DateDebut { get; set; }

        public DateTime? DateFin { get; set; }

        /// En cours | Reussie | Partielle | Echec
        [MaxLength(20)]
        public string Statut { get; set; } = "En cours";

        public int NbInsert { get; set; }

        public int NbUpdate { get; set; }

        public int NbRejet { get; set; }

        public long DureeMs { get; set; }

        /// Manuel | Planifie
        [MaxLength(20)]
        public string Declencheur { get; set; } = "Manuel";

        public int? UtilisateurId { get; set; }

        public string? MessageErreur { get; set; }
    }
}

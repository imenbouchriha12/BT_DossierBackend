using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    [Table("DocumentClients", Schema = "dbo")]
    public class DocumentClient
    {
        [Key]
        [Column("IdDocument")]
        public int Id { get; set; }

        [Column("IdClient")]
        public int IdClient { get; set; }

        [Column("TypeDocument")]
        public string TypeDocument { get; set; } = string.Empty;

        [Column("NomFichier")]
        public string NomFichier { get; set; } = string.Empty;

        [Column("CheminFichier")]
        public string CheminFichier { get; set; } = string.Empty;

        [Column("DateAjout")]
        public DateTime DateAjout { get; set; }

        [Column("StatutValidation")]
        public string StatutValidation { get; set; } = "En attente";

        [Column("UploadedByUserId")]
        public int? UploadedByUserId { get; set; }

        [ForeignKey("IdClient")]
        public Client Client { get; set; } = null!;

        [ForeignKey("UploadedByUserId")]
        public User? UploadedBy { get; set; }
    }
}

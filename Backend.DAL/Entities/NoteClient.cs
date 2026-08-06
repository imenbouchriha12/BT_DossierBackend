using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// <summary>
    /// Note interne attachée à un dossier client (visible uniquement par le personnel).
    /// </summary>
    [Table("NoteClient", Schema = "dbo")]
    public class NoteClient
    {
        [Key]
        [Column("IdNote")]
        public int Id { get; set; }

        [Column("IdClient")]
        public int IdClient { get; set; }

        /// Libellé court (optionnel) : ex "Appel du client", "Complément KYC"
        [Column("Titre")]
        [MaxLength(200)]
        public string? Titre { get; set; }

        [Column("Contenu")]
        [MaxLength(4000)]
        public string Contenu { get; set; } = string.Empty;

        /// Catégorie : Général, KYC, Relance, Prospection, Conformité, Autre
        [Column("Categorie")]
        [MaxLength(50)]
        public string Categorie { get; set; } = "Général";

        [Column("CreeParUserId")]
        public int CreeParUserId { get; set; }

        [Column("CreeParNom")]
        [MaxLength(200)]
        public string? CreeParNom { get; set; }

        [Column("DateCreation")]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [Column("ModifieParUserId")]
        public int? ModifieParUserId { get; set; }

        [Column("ModifieParNom")]
        [MaxLength(200)]
        public string? ModifieParNom { get; set; }

        [Column("DateMaj")]
        public DateTime? DateMaj { get; set; }

        /// Note épinglée en haut du fil
        [Column("EstEpinglee")]
        public bool EstEpinglee { get; set; } = false;

        [ForeignKey("IdClient")]
        public Client Client { get; set; } = null!;

        [ForeignKey("CreeParUserId")]
        public User CreePar { get; set; } = null!;
    }
}

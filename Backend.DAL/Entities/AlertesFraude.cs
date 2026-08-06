using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// <summary>
    /// Alerte de fraude / anomalie générée par le moteur de détection.
    /// </summary>
    [Table("AlertesFraude", Schema = "dbo")]
    public class AlertesFraude
    {
        [Key]
        [Column("IdAlerte")]
        public int Id { get; set; }

        [Column("IdClient")]
        public int IdClient { get; set; }

        /// Règle ayant déclenché l'alerte (null si analyse manuelle)
        [Column("IdRegle")]
        public int? IdRegle { get; set; }

        /// Mouvement en cause (si applicable)
        [Column("IdMouvement")]
        public int? IdMouvement { get; set; }

        [Column("NumCompte")]
        [MaxLength(20)]
        public string? NumCompte { get; set; }

        [Column("Agence")]
        [MaxLength(10)]
        public string? Agence { get; set; }

        [Column("Montant")]
        public decimal? Montant { get; set; }

        /// Type d'anomalie (MontantAnormal, PicDebit, MultiComptes...)
        [Column("Type")]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        /// Niveau de gravité : Critique, Haute, Moyenne, Basse
        [Column("Niveau")]
        [MaxLength(20)]
        public string Niveau { get; set; } = "Moyenne";

        /// Score de la règle (0-100)
        [Column("Score")]
        public int Score { get; set; }

        [Column("Description")]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        /// Statut : Nouvelle, EnCours, Traitee, Ignoree
        [Column("Statut")]
        [MaxLength(20)]
        public string Statut { get; set; } = "Nouvelle";

        /// Empreinte de déduplication (règle + client + jour + compte)
        [Column("Fingerprint")]
        [MaxLength(200)]
        public string Fingerprint { get; set; } = string.Empty;

        /// Données contextuelles (JSON)
        [Column("DonneesJson")]
        public string? DonneesJson { get; set; }

        [Column("DateDetection")]
        public DateTime DateDetection { get; set; } = DateTime.UtcNow;

        [Column("TraiteeParUserId")]
        public int? TraiteeParUserId { get; set; }

        [Column("DateTraitement")]
        public DateTime? DateTraitement { get; set; }

        [Column("Commentaire")]
        [MaxLength(2000)]
        public string? Commentaire { get; set; }

        [ForeignKey("IdClient")]
        public Client Client { get; set; } = null!;

        [ForeignKey("IdRegle")]
        public RegleAntifraude? Regle { get; set; }

        [ForeignKey("TraiteeParUserId")]
        public User? TraiteePar { get; set; }
    }
}

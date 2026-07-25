using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// <summary>
    /// Jeton d'accès sécurisé permettant à un client d'accéder au portail
    /// self-service pour uploader ses documents sans avoir de compte utilisateur.
    /// </summary>
    [Table("ClientPortalTokens", Schema = "dbo")]
    public class ClientPortalToken
    {
        [Key]
        [Column("IdToken")]
        public int Id { get; set; }

        /// Client concerné
        [Column("IdClient")]
        public int IdClient { get; set; }

        /// Valeur du token (GUID sécurisé, base64url)
        [Column("Token")]
        [MaxLength(200)]
        [Required]
        public string Token { get; set; } = string.Empty;

        /// Date de création du token
        [Column("DateCreation")]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        /// Date d'expiration (par défaut 7 jours)
        [Column("DateExpiration")]
        public DateTime DateExpiration { get; set; }

        /// Indique si le token a déjà été utilisé / révoqué
        [Column("EstRevoque")]
        public bool EstRevoque { get; set; } = false;

        /// Nombre de fois que le client a accédé au portail avec ce token
        [Column("NbAcces")]
        public int NbAcces { get; set; } = 0;

        /// Dernière IP d'accès (traçabilité)
        [Column("DerniereIp")]
        [MaxLength(50)]
        public string? DerniereIp { get; set; }

        /// Date du dernier accès
        [Column("DernierAcces")]
        public DateTime? DernierAcces { get; set; }

        [ForeignKey("IdClient")]
        public Client Client { get; set; } = null!;
    }
}

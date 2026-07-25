using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.DAL.Entities
{
    /// Entité représentant un utilisateur
    [Table("AppReg_Users", Schema = "dbo")]
    public class User
    {
        /// Identifiant unique
        [Column("UserId")]
        public int Id { get; set; }

        /// Nom d'utilisateur
        [Column("UserName")]
        public string Username { get; set; } = string.Empty;

        /// Matricule de l'utilisateur
        [Column("Matricule")]
        public string Matricule { get; set; } = string.Empty;

        /// Adresse email
        [Column("Email")]
        public string Email { get; set; } = string.Empty;

        /// Hash du mot de passe
        [Column("PasswordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        /// Indique si l'utilisateur est actif
        [Column("IsActive")]
        public bool IsActive { get; set; } = false;

        /// Indique si l'utilisateur a déjà été activé (permet de distinguer "en attente" vs "désactivé")
        [Column("HasBeenActivated")]
        public bool HasBeenActivated { get; set; } = false;

        /// Date de création du compte
        [Column("CreatedAt")]
        public DateTime? CreatedAt { get; set; }

        /// Refresh token JWT actuel
        [Column("RefreshToken")]
        public string? RefreshToken { get; set; }

        /// Date d'expiration du refresh token
        [Column("RefreshTokenExpiryTime")]
        public DateTime? RefreshTokenExpiryTime { get; set; }

        /// Token de réinitialisation de mot de passe
        [Column("PasswordResetToken")]
        public string? PasswordResetToken { get; set; }

        /// Date d'expiration du token de réinitialisation
        [Column("PasswordResetTokenExpiryTime")]
        public DateTime? PasswordResetTokenExpiryTime { get; set; }

        /// URL de la photo de profil
        [Column("PhotoUrl")]
        public string? PhotoUrl { get; set; }

        /// Navigation : rôles associés
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}

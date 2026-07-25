namespace Backend.DTO.Auth
{
    /// DTO représentant un utilisateur (lecture)
    public class UserDto
    {
        /// Identifiant unique de l'utilisateur
        public int Id { get; set; }

        /// Matricule de l'utilisateur
        public string Matricule { get; set; } = string.Empty;

        /// Nom d'utilisateur
        public string Username { get; set; } = string.Empty;

        /// Adresse email
        public string Email { get; set; } = string.Empty;

        /// Indique si le compte est actif
        public bool IsActive { get; set; }

        /// Liste des rôles assignés
        public List<string> Roles { get; set; } = new();

        /// URL de la photo de profil
        public string? PhotoUrl { get; set; }
    }
}

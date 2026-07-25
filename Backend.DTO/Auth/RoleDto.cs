namespace Backend.DTO.Auth
{
    /// DTO représentant un rôle (lecture)
    public class RoleDto
    {
        /// Identifiant unique du rôle
        public int Id { get; set; }

        /// Nom du rôle
        public string Name { get; set; } = string.Empty;

        /// Indique si le rôle est actif
        public bool IsActive { get; set; }
    }
}

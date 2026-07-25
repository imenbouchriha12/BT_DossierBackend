using System.ComponentModel.DataAnnotations;

namespace Backend.DTO.Auth
{
    /// DTO pour la mise à jour d'un rôle existant
    public class UpdateRoleDto
    {
        /// Nouveau nom du rôle
        [Required(ErrorMessage = "Le nom du rôle est requis")]
        [StringLength(50, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;
    }
}

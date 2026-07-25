using System.ComponentModel.DataAnnotations;

namespace Backend.DTO.Auth
{
    ///DTO pour la création d'un nouveau rôle.
    public class CreateRoleDto
    {
        /// Nom du rôle à créer.
        [Required(ErrorMessage = "Le nom du rôle est obligatoire.")]
        [MinLength(2, ErrorMessage = "Le nom doit contenir au moins 2 caractères.")]
        public string Name { get; set; } = string.Empty;
    }
}

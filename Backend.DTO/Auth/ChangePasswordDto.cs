using System.ComponentModel.DataAnnotations;

namespace Backend.DTO.Auth
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "L'ancien mot de passe est obligatoire.")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire.")]
        [MinLength(8, ErrorMessage = "Le nouveau mot de passe doit contenir au moins 8 caractères.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).+$",
            ErrorMessage = "Le mot de passe doit contenir au moins une majuscule, une minuscule et un chiffre.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}

using System.ComponentModel.DataAnnotations;

namespace Backend.DTO.Auth
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Format d'email invalide.")]
        public string Email { get; set; } = string.Empty;
    }
}

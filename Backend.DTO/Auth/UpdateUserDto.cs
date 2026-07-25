namespace Backend.DTO.Auth
{
    using System.ComponentModel.DataAnnotations;

    /// DTO pour la mise à jour d'un utilisateur
    public class UpdateUserDto
    {
        /// Matricule de l'utilisateur
        [Required(ErrorMessage = "Le matricule est obligatoire.")]
        [MinLength(3, ErrorMessage = "Le matricule doit contenir au moins 3 caractères.")]
        public string Matricule { get; set; } = string.Empty;

        /// Nom d'utilisateur
        [Required(ErrorMessage = "Le nom d'utilisateur est obligatoire.")]
        [MinLength(3, ErrorMessage = "Le nom d'utilisateur doit contenir au moins 3 caractères.")]
        public string Username { get; set; } = string.Empty;

        /// Adresse email de l'utilisateur
        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Format d'email invalide.")]
        public string Email { get; set; } = string.Empty;
    }
}

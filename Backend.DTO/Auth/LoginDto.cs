namespace Backend.DTO.Auth
{
    using System.ComponentModel.DataAnnotations;

    /// DTO pour la connexion d'un utilisateur existant.
    public class LoginDto
    {
        ///Matricule de l'utilisateur
        [Required(ErrorMessage = "Le matricule est obligatoire.")]
        public string Matricule { get; set; } = string.Empty;

        /// Mot de passe de l'utilisateur
        [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
        public string Password { get; set; } = string.Empty;
    }
}

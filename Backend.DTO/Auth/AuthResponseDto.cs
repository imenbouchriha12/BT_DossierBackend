namespace Backend.DTO.Auth
{
    /// DTO retourné après une connexion réussie
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public UserDto? User { get; set; }
    }
}

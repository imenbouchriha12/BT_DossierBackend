namespace Backend.DTO.Auth
{
    /// DTO pour le renouvellement d'un token JWT.
    public class RefreshTokenDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}

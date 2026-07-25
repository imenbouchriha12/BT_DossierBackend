using Backend.DTO.Auth;

namespace Backend.BL.IBL
{
    /// Service d'authentification 
    public interface IAuthService
    {
        Task<UserDto> RegisterAsync(RegisterDto dto);

        Task<AuthResponseDto> LoginAsync(LoginDto dto);

        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto);

        Task ChangePasswordAsync(int userId, ChangePasswordDto dto);

        Task RevokeRefreshTokenAsync(int userId);

        Task ForgotPasswordAsync(ForgotPasswordDto dto);

        Task ResetPasswordAsync(ResetPasswordDto dto);

        Task<UserDto> GetProfileAsync(int userId);

        Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileDto dto);

        Task<string> UploadPhotoAsync(int userId, string fileName, byte[] fileBytes);
    }
}

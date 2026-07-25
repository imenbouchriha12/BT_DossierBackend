using System.Security.Claims;
using Backend.BL.IBL;
using Backend.DTO.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    /// Contrôleur pour l'authentification
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;

        public AuthController(IAuthService authService, IUserService userService)
        {
            _authService = authService;
            _userService = userService;
        }

        /// Inscription d'un nouvel utilisateur
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var user = await _authService.RegisterAsync(dto);
            return Ok(new
            {
                user,
                message = "Compte créé avec succès. En attente d'activation par l'administrateur."
            });
        }

        /// Connexion d'un utilisateur existant
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }

        /// Renouvelle le token d'accès via le refresh token
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            var result = await _authService.RefreshTokenAsync(dto);
            return Ok(result);
        }

        /// Retourne les informations de l'utilisateur connecté
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _userService.GetByIdAsync(userId);
            return Ok(user);
        }

        /// Change le mot de passe de l'utilisateur connecté
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _authService.ChangePasswordAsync(userId, dto);
            return Ok(new { message = "Mot de passe changé avec succès." });
        }

        /// Déconnexion : révoque le refresh token
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _authService.RevokeRefreshTokenAsync(userId);
            return Ok(new { message = "Déconnexion réussie." });
        }

        /// Retourne le profil de l'utilisateur connecté
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var profile = await _authService.GetProfileAsync(userId);
            return Ok(profile);
        }

        /// Met à jour le profil (nom, email)
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var profile = await _authService.UpdateProfileAsync(userId, dto);
            return Ok(profile);
        }

        /// Upload la photo de profil
        [Authorize]
        [HttpPost("profile/photo")]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Fichier requis." });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".jfif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                return BadRequest(new { message = $"Format '{ext}' non supporté. Utilisez jpg, png, gif ou webp." });

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var photoUrl = await _authService.UploadPhotoAsync(userId, file.FileName, ms.ToArray());
            return Ok(new { photoUrl });
        }

        /// Demande de réinitialisation de mot de passe
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await _authService.ForgotPasswordAsync(dto);
            return Ok(new { message = "Si cette adresse email existe, un lien de réinitialisation vous a été envoyé." });
        }

        /// Réinitialisation du mot de passe avec le token
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            await _authService.ResetPasswordAsync(dto);
            return Ok(new { message = "Mot de passe réinitialisé avec succès." });
        }
    }
}

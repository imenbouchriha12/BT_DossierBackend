using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Backend.BL.Exceptions;
using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Auth;
using Backend.DTO.Notification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Backend.BL.BL
{
    /// Implémentation du service d'authentification
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        public AuthService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IMapper mapper,
            IConfiguration configuration,
            ILogger<AuthService> logger,
            IEmailService emailService,
            INotificationService notificationService)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _mapper = mapper;
            _configuration = configuration;
            _logger = logger;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        public async Task<UserDto> RegisterAsync(RegisterDto dto)
        {
            _logger.LogInformation("Tentative d'inscription pour {Email}", dto.Email);

            if (await _userRepository.EmailExistsAsync(dto.Email))
            {
                _logger.LogWarning("Email déjà existant : {Email}", dto.Email);
                throw new InvalidOperationException("Email already exists.");
            }

            var matricule = await GenerateUniqueMatriculeAsync();
            var tempPassword = GenerateRandomPassword();

            var user = new User
            {
                Matricule = matricule,
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };

            var userRole = await _roleRepository.GetByNameAsync("Agent");
            user.UserRoles.Add(new UserRole 
            { 
                RoleId = userRole.Id,
                AssignedAt = DateTime.UtcNow
            });

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Utilisateur créé avec succès : {UserId} ({Matricule})", user.Id, matricule);

            var allUsers = await _userRepository.GetAllWithRolesAsync();
            var admins = allUsers.Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Admin" || ur.Role.Name == "Superviseur"));
            foreach (var admin in admins)
            {
                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    UserId = admin.Id,
                    Titre = "Nouvel utilisateur en attente",
                    Message = $"L'utilisateur {dto.Username} ({dto.Email}) s'est inscrit et attend votre approbation.",
                    Type = "Info",
                    Lien = "/gestion-utilisateurs"
                });
            }

            return _mapper.Map<UserDto>(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            _logger.LogInformation("Tentative de connexion pour le matricule {Matricule}", dto.Matricule);

            var user = await _userRepository.GetByMatriculeWithRolesAsync(dto.Matricule);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Échec de connexion pour le matricule {Matricule}", dto.Matricule);
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Compte inactif : {UserId}", user.Id);
                throw new AccountDisabledException();
            }

            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Connexion réussie pour l'utilisateur {UserId}", user.Id);
            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = _mapper.Map<UserDto>(user)
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto)
        {
            var principal = GetPrincipalFromExpiredToken(dto.AccessToken);
            var userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var user = await _userRepository.GetByIdWithRolesAsync(userId);
            if (user == null || user.RefreshToken != dto.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token invalide ou expiré pour l'utilisateur {UserId}", userId);
                throw new UnauthorizedAccessException("Refresh token invalide ou expiré.");
            }

            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userRepository.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            _logger.LogInformation("Changement de mot de passe pour l'utilisateur {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("Utilisateur introuvable.");

            if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
                throw new UnauthorizedAccessException("L'ancien mot de passe est incorrect.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Mot de passe changé avec succès pour l'utilisateur {UserId}", userId);
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            _logger.LogInformation("Demande de réinitialisation de mot de passe pour {Email}", dto.Email);

            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                _logger.LogWarning("Email non trouvé : {Email}", dto.Email);
                return;
            }

            var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("/", "_").Replace("+", "-");
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiryTime = DateTime.UtcNow.AddHours(1);
            await _userRepository.SaveChangesAsync();

            var appUrl = _configuration["AllowedOrigins"]?.Split(',')?.FirstOrDefault() ?? "http://localhost:4200";
            var resetLink = $"{appUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(dto.Email)}";

            await _emailService.SendResetPasswordEmailAsync(dto.Email, user.Username, resetLink);
            _logger.LogInformation("Email de réinitialisation envoyé à {Email}", dto.Email);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            _logger.LogInformation("Tentative de réinitialisation de mot de passe pour {Email}", dto.Email);

            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null || user.PasswordResetToken != dto.Token || user.PasswordResetTokenExpiryTime <= DateTime.UtcNow)
            {
                _logger.LogWarning("Token de réinitialisation invalide ou expiré pour {Email}", dto.Email);
                throw new UnauthorizedAccessException("Le lien de réinitialisation est invalide ou a expiré.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiryTime = null;
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Mot de passe réinitialisé avec succès pour {Email}", dto.Email);
        }

        public async Task<UserDto> GetProfileAsync(int userId)
        {
            var user = await _userRepository.GetByIdWithRolesAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");
            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            if (user.Email != dto.Email && await _userRepository.EmailExistsAsync(dto.Email))
                throw new InvalidOperationException("Email already exists.");

            user.Username = dto.Username;
            user.Email = dto.Email;

            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Profil mis à jour pour l'utilisateur {UserId}", userId);

            var updated = await _userRepository.GetByIdWithRolesAsync(userId);
            return _mapper.Map<UserDto>(updated);
        }

        public async Task<string> UploadPhotoAsync(int userId, string fileName, byte[] fileBytes)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(fileName);
            var savedName = $"profile_{userId}{ext}";
            var filePath = Path.Combine(uploadsDir, savedName);

            await File.WriteAllBytesAsync(filePath, fileBytes);

            user.PhotoUrl = $"/uploads/profiles/{savedName}";
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Photo uploadée pour l'utilisateur {UserId}: {Url}", userId, user.PhotoUrl);
            return user.PhotoUrl;
        }

        public async Task RevokeRefreshTokenAsync(int userId)
        {
            _logger.LogInformation("Révocation du refresh token pour l'utilisateur {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("Utilisateur introuvable.");

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Refresh token révoqué pour l'utilisateur {UserId}", userId);
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.Username)
            };

            claims.AddRange(user.UserRoles.Select(ur => new Claim(ClaimTypes.Role, ur.Role.Name)));

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<string> GenerateUniqueMatriculeAsync()
        {
            string matricule;
            do
            {
                var digits = Random.Shared.Next(1000, 9999);
                var letter = (char)('A' + Random.Shared.Next(0, 26));
                matricule = $"EMP-{digits}-{letter}";
            }
            while (await _userRepository.MatriculeExistsAsync(matricule));
            return matricule;
        }

        private static string GenerateRandomPassword()
        {
            var random = Random.Shared;
            var upper = (char)('A' + random.Next(0, 26));
            var lower = (char)('a' + random.Next(0, 26));
            var digit = (char)('0' + random.Next(0, 10));
            var rest = Convert.ToBase64String(RandomNumberGenerator.GetBytes(6))
                .Replace("/", "").Replace("+", "").Replace("=", "")[..4];
            var all = (upper.ToString() + lower + digit + rest).ToCharArray();
            for (int i = all.Length - 1; i > 0; i--)
            {
                var j = random.Next(0, i + 1);
                (all[i], all[j]) = (all[j], all[i]);
            }
            return new string(all);
        }

        private static string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtToken || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Token invalide.");

            return principal;
        }
    }
}

using System.Security.Cryptography;
using AutoMapper;
using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Auth;
using Backend.DTO.Notification;
using Microsoft.Extensions.Logging;

namespace Backend.BL.BL
{
    // Implémentation du service de gestion des utilisateurs
    
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;
        private readonly IEmailService _emailService;
        private readonly IHistoriqueService _historiqueService;
        private readonly ICurrentUserService _currentUser;
        private readonly INotificationService _notificationService;

        public UserService(IUserRepository userRepository, IRoleRepository roleRepository, IMapper mapper, ILogger<UserService> logger, IEmailService emailService, IHistoriqueService historiqueService, ICurrentUserService currentUser, INotificationService notificationService)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _mapper = mapper;
            _logger = logger;
            _emailService = emailService;
            _historiqueService = historiqueService;
            _currentUser = currentUser;
            _notificationService = notificationService;
        }

        // Retourne tous les utilisateurs avec leurs rôles
        public async Task<List<UserDto>> GetAllAsync()
        {
            _logger.LogInformation("Récupération de tous les utilisateurs");
            var users = await _userRepository.GetAllWithRolesAsync();
            return _mapper.Map<List<UserDto>>(users);
        }

        //Retourne un utilisateur par son ID, null si introuvable
        public async Task<UserDto?> GetByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdWithRolesAsync(id);
            if (user == null)
            {
                _logger.LogWarning("Utilisateur introuvable : {UserId}", id);
                return null;
            }
            return _mapper.Map<UserDto>(user);
        }

        // Retourne les utilisateurs en attente d'activation
        public async Task<List<UserDto>> GetPendingAsync()
        {
            _logger.LogInformation("Récupération des utilisateurs en attente");
            var users = await _userRepository.GetAllWithRolesAsync();
            var pending = users.Where(u => !u.IsActive && !u.HasBeenActivated).ToList();
            return _mapper.Map<List<UserDto>>(pending);
        }

        public async Task ApproveAsync(int userId, int roleId)
        {
            var user = await _userRepository.GetByIdWithRolesAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            if (user.IsActive)
                throw new InvalidOperationException("User is already active.");

            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null) throw new KeyNotFoundException("Role not found.");
            if (!role.IsActive)
                throw new InvalidOperationException("Role is not active.");

            var password = GenerateRandomPassword();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.IsActive = true;
            user.HasBeenActivated = true;

            if (!await _userRepository.HasRoleAsync(userId, roleId))
            {
                _userRepository.AddUserRole(new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    AssignedAt = DateTime.UtcNow
                });
            }

            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Utilisateur {UserId} approuvé avec le rôle {RoleId}", userId, roleId);

            await _emailService.SendActivationEmailAsync(user.Email, user.Username, user.Matricule, password, role.Name);

            await _historiqueService.EnregistrerAsync("Utilisateurs", userId, "Activation", $"En attente", $"Actif (rôle: {role.Name})", _currentUser.UserId, _currentUser.UserName);

            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                UserId = userId,
                Titre = "Compte activé",
                Message = $"Votre compte a été activé avec le rôle {role.Name}. Votre matricule est {user.Matricule}.",
                Type = "Succès",
                Lien = "/tableau-de-bord"
            });
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

        // Active un utilisateur
        public async Task ActivateAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) throw new KeyNotFoundException("User not found.");

            user.IsActive = true;
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Utilisateur activé : {UserId}", id);

            await _historiqueService.EnregistrerAsync("Utilisateurs", id, "Activation", "Inactif", "Actif", _currentUser.UserId, _currentUser.UserName);

            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                UserId = id,
                Titre = "Compte activé",
                Message = "Votre compte a été activé.",
                Type = "Succès",
                Lien = "/tableau-de-bord"
            });
        }

        // Désactive un utilisateur
        public async Task DeactivateAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) throw new KeyNotFoundException("User not found.");

            user.IsActive = false;
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Utilisateur désactivé : {UserId}", id);

            await _historiqueService.EnregistrerAsync("Utilisateurs", id, "Désactivation", "Actif", "Inactif", _currentUser.UserId, _currentUser.UserName);

            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                UserId = id,
                Titre = "Compte désactivé",
                Message = "Votre compte a été désactivé.",
                Type = "Erreur",
                Lien = "/tableau-de-bord"
            });
        }

        // Assigne un rôle à un utilisateur 
        public async Task AssignRoleAsync(int userId, int roleId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role == null) throw new KeyNotFoundException("Role not found.");

            if (await _userRepository.HasRoleAsync(userId, roleId))
                throw new InvalidOperationException("User already has this role.");

            _userRepository.AddUserRole(new UserRole 
            { 
                UserId = userId, 
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow
            });
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Rôle {RoleId} assigné à l'utilisateur {UserId}", roleId, userId);
        }

        // Retire un rôle d'un utilisateur
        public async Task RemoveRoleAsync(int userId, int roleId)
        {
            var userRole = await _userRepository.GetUserRoleAsync(userId, roleId);
            if (userRole == null) throw new KeyNotFoundException("User does not have this role.");

            _userRepository.RemoveUserRole(userRole);
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Rôle {RoleId} retiré de l'utilisateur {UserId}", roleId, userId);
        }

        // Met à jour les informations d'un utilisateur
        public async Task<UserDto> UpdateAsync(int id, UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) throw new KeyNotFoundException("User not found.");

            // Vérifier si le matricule est déjà utilisé par un autre utilisateur
            if (user.Matricule != dto.Matricule && await _userRepository.MatriculeExistsAsync(dto.Matricule))
            {
                _logger.LogWarning("Matricule déjà utilisé : {Matricule}", dto.Matricule);
                throw new InvalidOperationException("Matricule already exists.");
            }

            // Vérifier si l'email est déjà utilisé par un autre utilisateur
            if (user.Email != dto.Email && await _userRepository.EmailExistsAsync(dto.Email))
            {
                _logger.LogWarning("Email déjà utilisé : {Email}", dto.Email);
                throw new InvalidOperationException("Email already exists.");
            }

            user.Matricule = dto.Matricule;
            user.Username = dto.Username;
            user.Email = dto.Email;

            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Utilisateur mis à jour : {UserId}", id);

            var updatedUser = await _userRepository.GetByIdWithRolesAsync(id);
            return _mapper.Map<UserDto>(updatedUser);
        }

        // Supprime un utilisateur
        public async Task DeleteAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) throw new KeyNotFoundException("User not found.");

            _userRepository.Delete(user);
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Utilisateur supprimé : {UserId}", id);
        }
    }
}

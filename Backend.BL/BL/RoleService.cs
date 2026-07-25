using AutoMapper;
using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Auth;
using Microsoft.Extensions.Logging;

namespace Backend.BL.BL
{
    /// Implémentation du service de gestion des rôles
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<RoleService> _logger;

        public RoleService(IRoleRepository roleRepository, IMapper mapper, ILogger<RoleService> logger)
        {
            _roleRepository = roleRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// Retourne tous les rôles
        public async Task<List<RoleDto>> GetAllAsync()
        {
            _logger.LogInformation("Récupération de tous les rôles");
            var roles = await _roleRepository.GetAllAsync();
            return _mapper.Map<List<RoleDto>>(roles);
        }

        /// Retourne un rôle par son ID, null si introuvable
        public async Task<RoleDto?> GetByIdAsync(int id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
            {
                _logger.LogWarning("Rôle introuvable : {RoleId}", id);
                return null;
            }
            return _mapper.Map<RoleDto>(role);
        }

        /// Crée un nouveau rôle avec statut actif par défaut
        public async Task<RoleDto> CreateAsync(CreateRoleDto dto)
        {
            var role = new Role
            {
                Name = dto.Name,
                IsActive = true
            };

            await _roleRepository.AddAsync(role);
            await _roleRepository.SaveChangesAsync();
            _logger.LogInformation("Rôle créé : {RoleName} (ID {RoleId})", role.Name, role.Id);

            return _mapper.Map<RoleDto>(role);
        }

        /// Met à jour le nom d'un rôle existant
        public async Task<RoleDto> UpdateAsync(int id, UpdateRoleDto dto)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null) throw new KeyNotFoundException("Role not found.");

            role.Name = dto.Name;
            await _roleRepository.SaveChangesAsync();
            _logger.LogInformation("Rôle mis à jour : {RoleId}", id);

            return _mapper.Map<RoleDto>(role);
        }

        /// Active un rôle
        public async Task ActivateAsync(int id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null) throw new KeyNotFoundException("Role not found.");

            role.IsActive = true;
            await _roleRepository.SaveChangesAsync();
            _logger.LogInformation("Rôle activé : {RoleId}", id);
        }

        /// Désactive un rôle
        public async Task DeactivateAsync(int id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null) throw new KeyNotFoundException("Role not found.");

            role.IsActive = false;
            await _roleRepository.SaveChangesAsync();
            _logger.LogInformation("Rôle désactivé : {RoleId}", id);
        }
    }
}

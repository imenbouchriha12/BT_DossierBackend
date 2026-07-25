using Backend.DTO.Auth;

namespace Backend.BL.IBL
{
    /// Service de gestion des rôles
    public interface IRoleService
    {
        /// Retourne tous les rôles
        Task<List<RoleDto>> GetAllAsync();

        /// Retourne un rôle par son ID
        /// <param name="id">ID du rôle.</param>
        Task<RoleDto?> GetByIdAsync(int id);

        /// Crée un nouveau rôle
        /// <param name="dto">Données du rôle.</param>
        Task<RoleDto> CreateAsync(CreateRoleDto dto);

        /// Met à jour un rôle existant
        /// <param name="id">ID du rôle.</param>
        /// <param name="dto">Nouvelles données.</param>
        Task<RoleDto> UpdateAsync(int id, UpdateRoleDto dto);

        /// Active un rôle
        /// <param name="id">ID du rôle.</param>
        Task ActivateAsync(int id);

        /// Désactive un rôle
        /// <param name="id">ID du rôle.</param>
        Task DeactivateAsync(int id);
    }
}

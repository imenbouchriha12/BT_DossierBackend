using Backend.DTO.Auth;

namespace Backend.BL.IBL
{
    /// Service de gestion des utilisateurs
    public interface IUserService
    {
        /// Retourne tous les utilisateurs avec leurs rôles
        Task<List<UserDto>> GetAllAsync();

        /// Retourne un utilisateur par son ID
        /// <param name="id">ID de l'utilisateur.</param>
        Task<UserDto?> GetByIdAsync(int id);

        /// Active un utilisateur
        /// <param name="id">ID de l'utilisateur.</param>
        Task ActivateAsync(int id);

        /// Désactive un utilisateur
        /// <param name="id">ID de l'utilisateur.</param>
        Task DeactivateAsync(int id);

        /// Retourne les utilisateurs en attente d'activation (inactifs)
        Task<List<UserDto>> GetPendingAsync();

        /// Approuve un utilisateur : active + assigne un rôle + envoie un email
        Task ApproveAsync(int userId, int roleId);

        /// Assigne un rôle à un utilisateur
        /// <param name="userId">ID de l'utilisateur.</param>
        /// <param name="roleId">ID du rôle.</param>
        Task AssignRoleAsync(int userId, int roleId);

        /// Retire un rôle d'un utilisateur
        /// <param name="userId">ID de l'utilisateur.</param>
        /// <param name="roleId">ID du rôle.</param>
        Task RemoveRoleAsync(int userId, int roleId);

        /// Met à jour les informations d'un utilisateur
        /// <param name="id">ID de l'utilisateur.</param>
        /// <param name="dto">Données de mise à jour.</param>
        Task<UserDto> UpdateAsync(int id, UpdateUserDto dto);

        /// Supprime un utilisateur
        /// <param name="id">ID de l'utilisateur.</param>
        Task DeleteAsync(int id);
    }
}

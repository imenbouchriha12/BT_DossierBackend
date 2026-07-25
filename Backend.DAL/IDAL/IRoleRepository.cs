using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    /// Interface du repository des rôles
    public interface IRoleRepository
    {
        /// Retourne un rôle par son ID
        Task<Role?> GetByIdAsync(int id);

        /// Retourne un rôle par son nom
        Task<Role> GetByNameAsync(string name);

        /// Retourne tous les rôles
        Task<List<Role>> GetAllAsync();

        /// Ajoute un nouveau rôle
        Task AddAsync(Role role);

        /// Persiste les modifications en base
        Task SaveChangesAsync();
    }
}

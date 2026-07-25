using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    /// Interface du repository des utilisateurs
    public interface IUserRepository
    {
        /// Retourne un utilisateur par son ID
        Task<User?> GetByIdAsync(int id);

        /// Retourne un utilisateur par son matricule
        Task<User?> GetByMatriculeAsync(string matricule);

        /// Retourne un utilisateur avec ses rôles par matricule
        Task<User?> GetByMatriculeWithRolesAsync(string matricule);

        /// Retourne un utilisateur par son email
        Task<User?> GetByEmailAsync(string email);

        /// Retourne un utilisateur avec ses rôles par email
        Task<User?> GetByEmailWithRolesAsync(string email);

        /// Retourne un utilisateur avec ses rôles par ID
        Task<User?> GetByIdWithRolesAsync(int id);

        /// Retourne tous les utilisateurs avec leurs rôles
        Task<List<User>> GetAllWithRolesAsync();

        /// Vérifie si un email existe déjà
        Task<bool> EmailExistsAsync(string email);

        /// Vérifie si un matricule existe déjà
        Task<bool> MatriculeExistsAsync(string matricule);

        /// Vérifie si un utilisateur possède un rôle
        Task<bool> HasRoleAsync(int userId, int roleId);

        /// Retourne l'association UserRole si elle existe
        Task<UserRole?> GetUserRoleAsync(int userId, int roleId);

        /// Ajoute un nouvel utilisateur
        Task AddAsync(User user);

        /// Supprime un utilisateur
        void Delete(User user);

        /// Ajoute une association User-Role
        void AddUserRole(UserRole userRole);

        /// Supprime une association User-Role
        void RemoveUserRole(UserRole userRole);

        /// Persiste les modifications en base
        Task SaveChangesAsync();
    }
}

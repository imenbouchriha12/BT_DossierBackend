using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL.DAL
{
    /// Repository des uses
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        /// Initialise le repository
        public UserRepository(AppDbContext context)
        {
            _context = context; // Injection du contexte
        }

        /// Retourne un utilisateur par son ID
        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id); // Recherche par clé primaire
        }

        /// Retourne un utilisateur par son matricule
        public async Task<User?> GetByMatriculeAsync(string matricule)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Matricule == matricule);
        }

        /// Retourne un utilisateur avec ses rôles par matricule
        public async Task<User?> GetByMatriculeWithRolesAsync(string matricule)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Matricule == matricule);
        }

        /// Retourne un utilisateur par son email
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email); // Filtre sur Email
        }

        /// Retourne un utilisateur avec ses rôles par email
        public async Task<User?> GetByEmailWithRolesAsync(string email)
        {
            return await _context.Users
                .Include(u => u.UserRoles)     // Charge les associations
                .ThenInclude(ur => ur.Role)    // Charge les rôles liés
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        /// Retourne un utilisateur avec ses rôles par ID
        public async Task<User?> GetByIdWithRolesAsync(int id)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        /// Retourne tous les utilisateurs avec leurs rôles
        public async Task<List<User>> GetAllWithRolesAsync()
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();
        }

        /// Vérifie si un email existe déjà
        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email); // True si trouvé
        }

        /// Vérifie si un matricule existe déjà
        public async Task<bool> MatriculeExistsAsync(string matricule)
        {
            return await _context.Users.AnyAsync(u => u.Matricule == matricule);
        }

        /// Vérifie si un utilisateur possède un rôle
        public async Task<bool> HasRoleAsync(int userId, int roleId)
        {
            return await _context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
        }

        /// Retourne l'association UserRole si elle existe
        public async Task<UserRole?> GetUserRoleAsync(int userId, int roleId)
        {
            return await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
        }

        /// Ajoute un nouvel utilisateur
        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        ///  Supprime un utilisateur
        public void Delete(User user)
        {
            _context.Users.Remove(user);
        }

        /// Ajoute une association User-Role
        public void AddUserRole(UserRole userRole)
        {
            _context.UserRoles.Add(userRole); // Marque pour insertion
        }

        /// Supprime une association User-Role
        public void RemoveUserRole(UserRole userRole)
        {
            _context.UserRoles.Remove(userRole); // Marque pour suppression
        }

        /// Persiste les modifications en base
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync(); // Écrit les changements en base
        }
    }
}

using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL.DAL
{
    /// Repository des rôles
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext _context;

        /// Initialise le repository 
        public RoleRepository(AppDbContext context)
        {
            _context = context; // Injection du contexte
        }

        /// Retourne un rôle par son ID
        public async Task<Role?> GetByIdAsync(int id)
        {
            return await _context.Roles.FindAsync(id);
        }

        /// Retourne un rôle par son nom (lève une exception si introuvable)
        public async Task<Role> GetByNameAsync(string name)
        {
            return await _context.Roles.FirstAsync(r => r.Name == name);
        }

        /// Retourne tous les rôles
        public async Task<List<Role>> GetAllAsync()
        {
            return await _context.Roles.ToListAsync(); // Liste complète
        }

        /// Ajoute un nouveau rôle (suivi par EF Core)
        public async Task AddAsync(Role role)
        {
            await _context.Roles.AddAsync(role);
        }

        /// Persiste les modifications en base
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL.DAL
{
    public class HistoriqueRepository : IHistoriqueRepository
    {
        private readonly AppDbContext _context;

        public HistoriqueRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(HistoriqueModification historique)
        {
            await _context.HistoriqueModifications.AddAsync(historique);
        }

        public async Task<HistoriqueModification?> GetByIdAsync(int id)
        {
            return await _context.HistoriqueModifications.FindAsync(id);
        }

        public async Task<(List<HistoriqueModification> Items, int Total)> SearchAsync(
            string? tableModifiee, string? utilisateurNom, string? action,
            DateTime? dateDebut, DateTime? dateFin, int page, int pageSize, int? userId = null)
        {
            var query = _context.HistoriqueModifications.AsQueryable();

            if (userId.HasValue)
                query = query.Where(h => h.UtilisateurId == userId.Value);
            if (!string.IsNullOrWhiteSpace(tableModifiee))
                query = query.Where(h => h.TableModifiee == tableModifiee);
            if (!string.IsNullOrWhiteSpace(utilisateurNom))
                query = query.Where(h => h.UtilisateurNom.Contains(utilisateurNom));
            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(h => h.Action == action);
            if (dateDebut.HasValue)
                query = query.Where(h => h.DateHeure >= dateDebut.Value);
            if (dateFin.HasValue)
                query = query.Where(h => h.DateHeure <= dateFin.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(h => h.DateHeure)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL.DAL
{
    public class RegleAntifraudeRepository : IRegleAntifraudeRepository
    {
        private readonly AppDbContext _context;

        public RegleAntifraudeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<RegleAntifraude>> GetAllAsync(bool activesSeulement = false)
        {
            var query = _context.ReglesAntifraude.AsQueryable();
            if (activesSeulement)
                query = query.Where(r => r.Actif);
            return await query
                .OrderBy(r => r.Priorite)
                .ThenBy(r => r.Nom)
                .ToListAsync();
        }

        public async Task<RegleAntifraude?> GetByIdAsync(int id)
        {
            return await _context.ReglesAntifraude.FindAsync(id);
        }

        public async Task<RegleAntifraude?> GetByCodeAsync(string code)
        {
            return await _context.ReglesAntifraude.FirstOrDefaultAsync(r => r.Code == code);
        }

        public async Task AddAsync(RegleAntifraude regle)
        {
            await _context.ReglesAntifraude.AddAsync(regle);
        }

        public async Task UpdateAsync(RegleAntifraude regle)
        {
            _context.ReglesAntifraude.Update(regle);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(RegleAntifraude regle)
        {
            _context.ReglesAntifraude.Remove(regle);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

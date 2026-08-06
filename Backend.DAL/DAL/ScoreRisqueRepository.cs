using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL.DAL
{
    public class ScoreRisqueRepository : IScoreRisqueRepository
    {
        private readonly AppDbContext _context;

        public ScoreRisqueRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ScoreRisque?> GetLatestByClientAsync(int idClient)
        {
            return await _context.ScoresRisque
                .Where(s => s.IdClient == idClient)
                .OrderByDescending(s => s.DateCalcul)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ScoreRisque>> GetHistoriqueAsync(int idClient, int page = 1, int pageSize = 50)
        {
            return await _context.ScoresRisque
                .Where(s => s.IdClient == idClient)
                .OrderByDescending(s => s.DateCalcul)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountByClientAsync(int idClient)
        {
            return await _context.ScoresRisque.CountAsync(s => s.IdClient == idClient);
        }

        public async Task AddAsync(ScoreRisque score)
        {
            await _context.ScoresRisque.AddAsync(score);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<ScoreRisque>> GetLatestForClientsAsync(IEnumerable<int> idClients)
        {
            var ids = idClients.ToList();
            if (ids.Count == 0) return new List<ScoreRisque>();

            var all = await _context.ScoresRisque
                .Where(s => ids.Contains(s.IdClient))
                .OrderByDescending(s => s.DateCalcul)
                .ToListAsync();

            return all
                .GroupBy(s => s.IdClient)
                .Select(g => g.First())
                .ToList();
        }
    }
}

using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL.DAL
{
    public class CoursRepository : ICoursRepository
    {
        private readonly AppDbContext _context;

        public CoursRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Cours>> GetLatestAsync(string? devise)
        {
            var query = _context.Cours.AsQueryable();
            if (!string.IsNullOrWhiteSpace(devise))
                query = query.Where(c => c.Devise == devise);

            var dernieres = await query
                .GroupBy(c => c.Devise)
                .Select(g => g.OrderByDescending(c => c.Journee).First())
                .ToListAsync();

            return dernieres.OrderBy(c => c.Devise).ToList();
        }

        public async Task<Cours?> GetLatestByDeviseAsync(string devise)
        {
            return await _context.Cours
                .Where(c => c.Devise == devise)
                .OrderByDescending(c => c.Journee)
                .FirstOrDefaultAsync();
        }

        public async Task<Cours?> GetByJourneeDeviseAsync(DateTime journee, string devise)
        {
            return await _context.Cours
                .Where(c => c.Journee == journee && c.Devise == devise)
                .OrderByDescending(c => c.Journee)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Cours>> GetHistoryAsync(string devise, DateTime? dateDebut, DateTime? dateFin)
        {
            var query = _context.Cours.Where(c => c.Devise == devise).AsQueryable();
            if (dateDebut.HasValue)
                query = query.Where(c => c.Journee >= dateDebut.Value);
            if (dateFin.HasValue)
                query = query.Where(c => c.Journee <= dateFin.Value);

            return await query.OrderBy(c => c.Journee).ToListAsync();
        }

        public async Task AddAsync(Cours cours)
        {
            await _context.Cours.AddAsync(cours);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}

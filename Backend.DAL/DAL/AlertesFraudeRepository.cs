using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL.DAL
{
    public class AlertesFraudeRepository : IAlertesFraudeRepository
    {
        private readonly AppDbContext _context;

        public AlertesFraudeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<AlertesFraude>> GetByClientAsync(int idClient, int page = 1, int pageSize = 50)
        {
            return await _context.AlertesFraudes
                .Where(a => a.IdClient == idClient)
                .Include(a => a.Client)
                .Include(a => a.Regle)
                .OrderByDescending(a => a.DateDetection)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountByClientAsync(int idClient)
        {
            return await _context.AlertesFraudes.CountAsync(a => a.IdClient == idClient);
        }

        public async Task<int> CountActivesByClientAsync(int idClient)
        {
            return await _context.AlertesFraudes.CountAsync(a => a.IdClient == idClient && a.Statut != "Traitee" && a.Statut != "Ignoree");
        }

        public async Task<(List<AlertesFraude> Items, int Total)> SearchAsync(int? idClient, string? statut, string? niveau, string? type, DateTime? dateDebut, DateTime? dateFin, int page, int pageSize)
        {
            var query = _context.AlertesFraudes
                .Include(a => a.Client)
                .Include(a => a.Regle)
                .AsQueryable();

            if (idClient.HasValue)
                query = query.Where(a => a.IdClient == idClient.Value);
            if (!string.IsNullOrWhiteSpace(statut))
                query = query.Where(a => a.Statut == statut);
            if (!string.IsNullOrWhiteSpace(niveau))
                query = query.Where(a => a.Niveau == niveau);
            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(a => a.Type == type);
            if (dateDebut.HasValue)
                query = query.Where(a => a.DateDetection >= dateDebut.Value);
            if (dateFin.HasValue)
                query = query.Where(a => a.DateDetection <= dateFin.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(a => a.DateDetection)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<AlertesFraude?> GetByIdAsync(int id)
        {
            return await _context.AlertesFraudes
                .Include(a => a.Client)
                .Include(a => a.Regle)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<AlertesFraude?> GetByFingerprintAsync(string fingerprint)
        {
            return await _context.AlertesFraudes
                .FirstOrDefaultAsync(a => a.Fingerprint == fingerprint && a.Statut != "Traitee" && a.Statut != "Ignoree");
        }

        public async Task AddAsync(AlertesFraude alerte)
        {
            await _context.AlertesFraudes.AddAsync(alerte);
        }

        public async Task UpdateAsync(AlertesFraude alerte)
        {
            _context.AlertesFraudes.Update(alerte);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<AlertesFraude>> GetRecentAsync(int count)
        {
            return await _context.AlertesFraudes
                .Include(a => a.Client)
                .OrderByDescending(a => a.DateDetection)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> CountActivesAsync()
        {
            return await _context.AlertesFraudes.CountAsync(a => a.Statut != "Traitee" && a.Statut != "Ignoree");
        }
    }
}

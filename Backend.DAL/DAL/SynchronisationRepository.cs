using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL.DAL
{
    public class SynchronisationRepository : ISynchronisationRepository
    {
        private readonly AppDbContext _context;

        public SynchronisationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SynchronisationLog?> GetLastAsync()
        {
            return await _context.SynchronisationLogs
                .OrderByDescending(s => s.DateDebut)
                .FirstOrDefaultAsync();
        }

        public async Task<SynchronisationLog?> GetEnCoursAsync()
        {
            return await _context.SynchronisationLogs
                .Where(s => s.Statut == "En cours")
                .OrderByDescending(s => s.DateDebut)
                .FirstOrDefaultAsync();
        }

        public async Task<List<SynchronisationLog>> GetHistoryAsync(int page, int pageSize)
        {
            return await _context.SynchronisationLogs
                .OrderByDescending(s => s.DateDebut)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<SynchronisationRejet>> GetRejetsAsync(int idSynchronisation)
        {
            return await _context.SynchronisationRejets
                .Where(r => r.IdSynchronisation == idSynchronisation)
                .OrderBy(r => r.Id)
                .ToListAsync();
        }

        public async Task AddLogAsync(SynchronisationLog log)
        {
            await _context.SynchronisationLogs.AddAsync(log);
        }

        public async Task AddRejetsAsync(IEnumerable<SynchronisationRejet> rejets)
        {
            await _context.SynchronisationRejets.AddRangeAsync(rejets);
        }

        public async Task<int> CountRejetsNonResolusAsync()
        {
            return await _context.SynchronisationRejets.CountAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

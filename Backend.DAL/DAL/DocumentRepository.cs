using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL.DAL
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly AppDbContext _context;

        public DocumentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DocumentClient?> GetByIdAsync(int id)
        {
            return await _context.DocumentClients
                .Include(d => d.Client)
                .Include(d => d.UploadedBy)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<List<DocumentClient>> GetByClientIdAsync(int clientId)
        {
            return await _context.DocumentClients
                .Include(d => d.Client)
                .Where(d => d.IdClient == clientId)
                .OrderByDescending(d => d.DateAjout)
                .ToListAsync();
        }

        public async Task<List<DocumentClient>> GetAllAsync()
        {
            return await _context.DocumentClients
                .Include(d => d.Client)
                .OrderByDescending(d => d.DateAjout)
                .ToListAsync();
        }

        public async Task<(List<DocumentClient> Items, int Total)> GetByClientIdPagedAsync(int clientId, int page, int pageSize)
        {
            var query = _context.DocumentClients
                .Include(d => d.Client)
                .Where(d => d.IdClient == clientId);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(d => d.DateAjout)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task AddAsync(DocumentClient document)
        {
            await _context.DocumentClients.AddAsync(document);
        }

        public void Delete(DocumentClient document)
        {
            _context.DocumentClients.Remove(document);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<int> CountAsync()
        {
            return await _context.DocumentClients.CountAsync();
        }

        public async Task<int> CountByStatutAsync(string statut)
        {
            return await _context.DocumentClients.CountAsync(d => d.StatutValidation == statut);
        }

        public async Task<List<DocumentClient>> GetRecentAsync(int count)
        {
            return await _context.DocumentClients
                .Include(d => d.Client)
                .OrderByDescending(d => d.DateAjout)
                .Take(count)
                .ToListAsync();
        }
    }
}

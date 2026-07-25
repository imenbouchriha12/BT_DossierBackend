using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL.DAL
{
    public class DocumentTemplateRepository : IDocumentTemplateRepository
    {
        private readonly AppDbContext _context;

        public DocumentTemplateRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DocumentTemplate>> GetAllAsync()
            => await _context.DocumentTemplates
                .OrderBy(t => t.TypeClient)
                .ThenBy(t => t.Ordre)
                .ToListAsync();

        public async Task<List<DocumentTemplate>> GetByTypeClientAsync(string typeClient)
            => await _context.DocumentTemplates
                .Where(t => t.TypeClient == typeClient && t.IsActive)
                .OrderBy(t => t.Ordre)
                .ToListAsync();

        public async Task<DocumentTemplate?> GetByIdAsync(int id)
            => await _context.DocumentTemplates.FindAsync(id);

        public async Task<bool> ExistsAsync(string typeClient, string nomDocument)
            => await _context.DocumentTemplates
                .AnyAsync(t => t.TypeClient == typeClient && t.NomDocument == nomDocument);

        public async Task AddAsync(DocumentTemplate template)
            => await _context.DocumentTemplates.AddAsync(template);

        public void Update(DocumentTemplate template)
            => _context.DocumentTemplates.Update(template);

        public void Delete(DocumentTemplate template)
            => _context.DocumentTemplates.Remove(template);

        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();
    }
}

using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL.DAL
{
    public class WorkflowRepository : IWorkflowRepository
    {
        private readonly AppDbContext _context;

        public WorkflowRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<WorkflowValidation?> GetByIdAsync(int id)
        {
            return await _context.WorkflowValidations
                .Include(w => w.Etapes)
                .Include(w => w.Document)
                    .ThenInclude(d => d.Client)
                .Include(w => w.Document)
                    .ThenInclude(d => d.UploadedBy)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<WorkflowValidation?> GetLatestByDocumentIdAsync(int documentId)
        {
            return await _context.WorkflowValidations
                .Include(w => w.Etapes)
                .Include(w => w.Document)
                    .ThenInclude(d => d.Client)
                .Include(w => w.Document)
                    .ThenInclude(d => d.UploadedBy)
                .Where(w => w.IdDocument == documentId)
                .OrderByDescending(w => w.DateCreation)
                .FirstOrDefaultAsync();
        }

        public async Task<List<WorkflowValidation>> GetLatestByDocumentIdsAsync(IEnumerable<int> documentIds)
        {
            var ids = documentIds.ToList();
            if (ids.Count == 0)
                return new List<WorkflowValidation>();

            var workflows = await _context.WorkflowValidations
                .Include(w => w.Etapes)
                .Include(w => w.Document)
                    .ThenInclude(d => d.Client)
                .Where(w => ids.Contains(w.IdDocument))
                .ToListAsync();

            return workflows
                .GroupBy(w => w.IdDocument)
                .Select(g => g.OrderByDescending(w => w.DateCreation).First())
                .ToList();
        }

        public async Task<List<WorkflowValidation>> GetHistoryByDocumentIdAsync(int documentId)
        {
            return await _context.WorkflowValidations
                .Include(w => w.Etapes)
                .Include(w => w.Document)
                    .ThenInclude(d => d.Client)
                .Where(w => w.IdDocument == documentId)
                .OrderByDescending(w => w.DateCreation)
                .ToListAsync();
        }

        public async Task<(List<WorkflowValidation> Items, int Total)> GetEnAttenteByRolesAsync(
            List<string> roles, int page, int pageSize)
        {
            var query = _context.WorkflowValidations
                .Include(w => w.Etapes)
                .Include(w => w.Document)
                    .ThenInclude(d => d.Client)
                .Where(w => w.StatutGlobal == "EnCours")
                .Where(w => w.EtapeCourante != null && roles.Contains(w.EtapeCourante));

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(w => w.DateMaj)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<int> CountEnAttenteByRolesAsync(List<string> roles)
        {
            return await _context.WorkflowValidations
                .Where(w => w.StatutGlobal == "EnCours")
                .Where(w => w.EtapeCourante != null && roles.Contains(w.EtapeCourante))
                .CountAsync();
        }

        public async Task<Dictionary<string, int>> CountEnAttenteParRoleAsync()
        {
            return await _context.WorkflowValidations
                .Where(w => w.StatutGlobal == "EnCours" && w.EtapeCourante != null)
                .GroupBy(w => w.EtapeCourante!)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task AddAsync(WorkflowValidation workflow)
        {
            await _context.WorkflowValidations.AddAsync(workflow);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL.DAL
{
    public class ReminderRepository : IReminderRepository
    {
        private readonly AppDbContext _context;

        public ReminderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Reminder>> GetAllAsync(int userId, int page = 1, int pageSize = 50)
        {
            return await _context.Reminders
                .Where(r => r.CreatedByUserId == userId || r.AssignedToUserId == userId)
                .Include(r => r.Client)
                .Include(r => r.AssignedToUser)
                .OrderBy(r => r.DueDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Reminder>> GetByFilterAsync(int userId, string? type, string? priority, string? status, int? clientId, int? assignedToUserId, DateTime? dateFrom, DateTime? dateTo)
        {
            var query = _context.Reminders
                .Where(r => r.CreatedByUserId == userId || r.AssignedToUserId == userId)
                .Include(r => r.Client)
                .Include(r => r.AssignedToUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(type))
                query = query.Where(r => r.Type == type);

            if (!string.IsNullOrEmpty(priority))
                query = query.Where(r => r.Priority == priority);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            if (clientId.HasValue)
                query = query.Where(r => r.ClientId == clientId.Value);

            if (assignedToUserId.HasValue)
                query = query.Where(r => r.AssignedToUserId == assignedToUserId.Value);

            if (dateFrom.HasValue)
                query = query.Where(r => r.DueDate >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(r => r.DueDate <= dateTo.Value);

            return await query.OrderBy(r => r.DueDate).ToListAsync();
        }

        public async Task<Reminder?> GetByIdAsync(int id)
        {
            return await _context.Reminders
                .Include(r => r.Client)
                .Include(r => r.AssignedToUser)
                .Include(r => r.CreatedByUser)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<int> GetCountAsync(int userId)
        {
            return await _context.Reminders
                .CountAsync(r => (r.CreatedByUserId == userId || r.AssignedToUserId == userId) && r.Status != "Termine" && r.Status != "Annule");
        }

        public async Task<int> GetCountByStatusAsync(int userId, string status)
        {
            return await _context.Reminders
                .CountAsync(r => (r.CreatedByUserId == userId || r.AssignedToUserId == userId) && r.Status == status);
        }

        public async Task<int> GetOverdueCountAsync(int userId)
        {
            return await _context.Reminders
                .CountAsync(r => (r.CreatedByUserId == userId || r.AssignedToUserId == userId)
                    && r.Status != "Termine" && r.Status != "Annule"
                    && r.DueDate < DateTime.UtcNow);
        }

        public async Task<int> GetUrgentPendingCountAsync(int userId)
        {
            return await _context.Reminders
                .CountAsync(r => (r.CreatedByUserId == userId || r.AssignedToUserId == userId)
                    && r.Priority == "Critique"
                    && r.Status != "Termine" && r.Status != "Annule");
        }

        public async Task<List<Reminder>> GetOverdueUnnotifiedAsync()
        {
            return await _context.Reminders
                .Where(r => r.Status != "Termine" && r.Status != "Annule"
                    && r.DueDate < DateTime.UtcNow
                    && r.NotifiedAt == null)
                .Include(r => r.Client)
                .Include(r => r.AssignedToUser)
                .Include(r => r.CreatedByUser)
                .ToListAsync();
        }

        public async Task AddAsync(Reminder reminder)
        {
            await _context.Reminders.AddAsync(reminder);
        }

        public async Task UpdateAsync(Reminder reminder)
        {
            await Task.Run(() => _context.Reminders.Update(reminder));
        }

        public async Task DeleteAsync(Reminder reminder)
        {
            await Task.Run(() => _context.Reminders.Remove(reminder));
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<List<Reminder>> GetAllTeamAsync(string? type, string? priority, string? status, int? clientId, int? assignedToUserId, DateTime? dateFrom, DateTime? dateTo)
        {
            var query = _context.Reminders
                .Include(r => r.Client)
                .Include(r => r.AssignedToUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(type))
                query = query.Where(r => r.Type == type);

            if (!string.IsNullOrEmpty(priority))
                query = query.Where(r => r.Priority == priority);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            if (clientId.HasValue)
                query = query.Where(r => r.ClientId == clientId.Value);

            if (assignedToUserId.HasValue)
                query = query.Where(r => r.AssignedToUserId == assignedToUserId.Value);

            if (dateFrom.HasValue)
                query = query.Where(r => r.DueDate >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(r => r.DueDate <= dateTo.Value);

            return await query.OrderBy(r => r.DueDate).ToListAsync();
        }

        public async Task<int> GetAllTeamCountAsync(string? status)
        {
            var query = _context.Reminders.AsQueryable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);
            return await query.CountAsync();
        }

        public async Task<int> GetAllTeamOverdueCountAsync()
        {
            return await _context.Reminders
                .Where(r => r.Status != "Termine" && r.DueDate < DateTime.UtcNow)
                .CountAsync();
        }

        public async Task<int> GetAllTeamUrgentPendingCountAsync()
        {
            var now = DateTime.UtcNow;
            var soon = now.AddDays(2);
            return await _context.Reminders
                .Where(r => r.Status != "Termine" && r.Priority == "Critique" && r.DueDate <= soon)
                .CountAsync();
        }
    }
}

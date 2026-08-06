using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    public interface IReminderRepository
    {
        Task<List<Reminder>> GetAllAsync(int userId, int page = 1, int pageSize = 50);
        Task<List<Reminder>> GetByFilterAsync(int userId, string? type, string? priority, string? status, int? clientId, int? assignedToUserId, DateTime? dateFrom, DateTime? dateTo);
        Task<List<Reminder>> GetAllTeamAsync(string? type, string? priority, string? status, int? clientId, int? assignedToUserId, DateTime? dateFrom, DateTime? dateTo);
        Task<int> GetAllTeamCountAsync(string? status);
        Task<int> GetAllTeamOverdueCountAsync();
        Task<int> GetAllTeamUrgentPendingCountAsync();
        Task<Reminder?> GetByIdAsync(int id);
        Task<int> GetCountAsync(int userId);
        Task<int> GetCountByStatusAsync(int userId, string status);
        Task<int> GetOverdueCountAsync(int userId);
        Task<int> GetUrgentPendingCountAsync(int userId);
        Task<List<Reminder>> GetOverdueUnnotifiedAsync();
        Task AddAsync(Reminder reminder);
        Task UpdateAsync(Reminder reminder);
        Task DeleteAsync(Reminder reminder);
        Task SaveChangesAsync();
    }
}

using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    public interface INotificationRepository
    {
        Task<List<Notification>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 20);
        Task<Notification?> GetByIdAsync(int id);
        Task<int> GetNonLuCountAsync(int userId);
        Task AddAsync(Notification notification);
        Task SaveChangesAsync();
    }
}

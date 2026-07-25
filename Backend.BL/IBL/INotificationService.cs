using Backend.DTO.Notification;

namespace Backend.BL.IBL
{
    public interface INotificationService
    {
        Task<List<NotificationDto>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 20);
        Task<int> GetNonLuCountAsync(int userId);
        Task<NotificationDto> CreateAsync(CreateNotificationDto dto);
        Task<bool> MarkAsLuAsync(int id, int userId);
        Task MarkAllAsLuAsync(int userId);
    }
}

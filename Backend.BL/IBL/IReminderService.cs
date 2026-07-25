using Backend.DTO.Reminder;

namespace Backend.BL.IBL
{
    public interface IReminderService
    {
        Task<List<ReminderDto>> GetAllAsync(int userId, int page = 1, int pageSize = 50);
        Task<List<ReminderDto>> GetFilteredAsync(int userId, ReminderFilterDto filter);
        Task<ReminderDto?> GetByIdAsync(int id);
        Task<ReminderStatsDto> GetStatsAsync(int userId);
        Task<ReminderDto> CreateAsync(int userId, CreateReminderDto dto);
        Task<ReminderDto?> UpdateAsync(int id, UpdateReminderDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> CompleteAsync(int id);
    }
}

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
        Task<bool> CompleteAsync(int id, int completedByUserId);
        Task<bool> SignAsync(int id, int userId);
        Task<bool> UpdateStatusAsync(int id, string status, int userId);
        Task<ReminderDto> CreateDocumentReminderAsync(int userId, int clientId, string title);
        Task<List<ReminderDto>> GetTeamFilteredAsync(ReminderFilterDto filter);
        Task<ReminderStatsDto> GetTeamStatsAsync();
        Task<List<ReminderDto>> GetAssignedToMeAsync(int userId, ReminderFilterDto filter);
    }
}

using AutoMapper;
using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Reminder;
using Microsoft.Extensions.Logging;

namespace Backend.BL.BL
{
    public class ReminderService : IReminderService
    {
        private readonly IReminderRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<ReminderService> _logger;

        public ReminderService(IReminderRepository repository, IMapper mapper, ILogger<ReminderService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<ReminderDto>> GetAllAsync(int userId, int page = 1, int pageSize = 50)
        {
            var reminders = await _repository.GetAllAsync(userId, page, pageSize);
            return reminders.Select(MapToDto).ToList();
        }

        public async Task<List<ReminderDto>> GetFilteredAsync(int userId, ReminderFilterDto filter)
        {
            var reminders = await _repository.GetByFilterAsync(
                userId, filter.Type, filter.Priority, filter.Status,
                filter.ClientId, filter.AssignedToUserId, filter.DateFrom, filter.DateTo);
            return reminders.Select(MapToDto).ToList();
        }

        public async Task<ReminderDto?> GetByIdAsync(int id)
        {
            var reminder = await _repository.GetByIdAsync(id);
            return reminder == null ? null : MapToDto(reminder);
        }

        public async Task<ReminderStatsDto> GetStatsAsync(int userId)
        {
            return new ReminderStatsDto
            {
                Total = await _repository.GetCountAsync(userId),
                Pending = await _repository.GetCountByStatusAsync(userId, "Pending"),
                InProgress = await _repository.GetCountByStatusAsync(userId, "InProgress"),
                Completed = await _repository.GetCountByStatusAsync(userId, "Completed"),
                Overdue = await _repository.GetOverdueCountAsync(userId),
                Urgent = await _repository.GetUrgentPendingCountAsync(userId)
            };
        }

        public async Task<ReminderDto> CreateAsync(int userId, CreateReminderDto dto)
        {
            var reminder = new Reminder
            {
                Title = dto.Title,
                Description = dto.Description,
                Type = dto.Type,
                Priority = dto.Priority,
                Status = "Pending",
                DueDate = dto.DueDate,
                ClientId = dto.ClientId,
                AssignedToUserId = dto.AssignedToUserId,
                CreatedByUserId = userId,
                DateCreation = DateTime.UtcNow
            };

            await _repository.AddAsync(reminder);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Rappel créé: {Title} par l'utilisateur {UserId}", dto.Title, userId);

            var created = await _repository.GetByIdAsync(reminder.Id);
            return MapToDto(created!);
        }

        public async Task<ReminderDto?> UpdateAsync(int id, UpdateReminderDto dto)
        {
            var reminder = await _repository.GetByIdAsync(id);
            if (reminder == null) return null;

            if (dto.Title != null) reminder.Title = dto.Title;
            if (dto.Description != null) reminder.Description = dto.Description;
            if (dto.Type != null) reminder.Type = dto.Type;
            if (dto.Priority != null) reminder.Priority = dto.Priority;
            if (dto.Status != null)
            {
                reminder.Status = dto.Status;
                if (dto.Status == "Completed")
                    reminder.CompletedAt = DateTime.UtcNow;
            }
            if (dto.DueDate.HasValue) reminder.DueDate = dto.DueDate.Value;
            if (dto.AssignedToUserId.HasValue) reminder.AssignedToUserId = dto.AssignedToUserId.Value;

            await _repository.UpdateAsync(reminder);
            await _repository.SaveChangesAsync();

            var updated = await _repository.GetByIdAsync(id);
            return MapToDto(updated!);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var reminder = await _repository.GetByIdAsync(id);
            if (reminder == null) return false;

            await _repository.DeleteAsync(reminder);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompleteAsync(int id)
        {
            var reminder = await _repository.GetByIdAsync(id);
            if (reminder == null) return false;

            reminder.Status = "Completed";
            reminder.CompletedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(reminder);
            await _repository.SaveChangesAsync();
            return true;
        }

        private static ReminderDto MapToDto(Reminder r)
        {
            return new ReminderDto
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                Type = r.Type,
                Priority = r.Priority,
                Status = r.Status,
                DueDate = r.DueDate,
                CompletedAt = r.CompletedAt,
                ClientId = r.ClientId,
                ClientName = r.Client != null ? $"{r.Client.Prenom} {r.Client.Nom}" : null,
                ClientCode = r.Client?.CodeClient,
                AssignedToUserId = r.AssignedToUserId,
                AssignedToUserName = r.AssignedToUser != null ? r.AssignedToUser.Username : null,
                CreatedByUserId = r.CreatedByUserId,
                CreatedByUserName = r.CreatedByUser?.Username,
                DateCreation = r.DateCreation,
                NotifiedAt = r.NotifiedAt
            };
        }
    }
}

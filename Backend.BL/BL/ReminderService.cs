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
        private readonly IActivityService _activityService;

        public ReminderService(IReminderRepository repository, IMapper mapper, ILogger<ReminderService> logger, IActivityService activityService)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
            _activityService = activityService;
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
                Pending = await _repository.GetCountByStatusAsync(userId, "EnAttente"),
                InProgress = await _repository.GetCountByStatusAsync(userId, "EnCours"),
                Completed = await _repository.GetCountByStatusAsync(userId, "Termine"),
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
                Status = "EnAttente",
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

            if (reminder.ClientId.HasValue)
            {
                await _activityService.EnregistrerEvenementAsync(
                    reminder.ClientId.Value, EvenementTypes.Rappel,
                    $"Rappel « {reminder.Title} » créé (échéance : {reminder.DueDate:dd/MM/yyyy}).",
                    idLien: reminder.Id, typeLien: nameof(Reminder));
            }

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
                if (dto.Status == "Termine")
                    reminder.CompletedAt = DateTime.UtcNow;
            }
            if (dto.DueDate.HasValue) reminder.DueDate = dto.DueDate.Value;
            if (dto.AssignedToUserId.HasValue) reminder.AssignedToUserId = dto.AssignedToUserId.Value;

            await _repository.UpdateAsync(reminder);
            await _repository.SaveChangesAsync();

            var updated = await _repository.GetByIdAsync(id);

            if (reminder.ClientId.HasValue)
            {
                await _activityService.EnregistrerEvenementAsync(
                    reminder.ClientId.Value, EvenementTypes.Rappel,
                    $"Rappel « {reminder.Title} » mis à jour (statut : {reminder.Status}).",
                    idLien: reminder.Id, typeLien: nameof(Reminder));
            }

            return MapToDto(updated!);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var reminder = await _repository.GetByIdAsync(id);
            if (reminder == null) return false;

            if (reminder.ClientId.HasValue)
            {
                await _activityService.EnregistrerEvenementAsync(
                    reminder.ClientId.Value, EvenementTypes.Rappel,
                    $"Rappel « {reminder.Title} » supprimé.",
                    idLien: reminder.Id, typeLien: nameof(Reminder));
            }

            await _repository.DeleteAsync(reminder);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompleteAsync(int id)
        {
            var reminder = await _repository.GetByIdAsync(id);
            if (reminder == null) return false;

            reminder.Status = "Termine";
            reminder.CompletedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(reminder);
            await _repository.SaveChangesAsync();

            if (reminder.ClientId.HasValue)
            {
                await _activityService.EnregistrerEvenementAsync(
                    reminder.ClientId.Value, EvenementTypes.Rappel,
                    $"Rappel « {reminder.Title} » terminé.",
                    idLien: reminder.Id, typeLien: nameof(Reminder));
            }

            return true;
        }

        public async Task<bool> CompleteAsync(int id, int completedByUserId)
        {
            var reminder = await _repository.GetByIdAsync(id);
            if (reminder == null) return false;

            reminder.Status = "Termine";
            reminder.CompletedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(reminder);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SignAsync(int id, int userId)
        {
            var reminder = await _repository.GetByIdAsync(id);
            if (reminder == null) return false;

            if (reminder.Status == "Termine") return false;
            if (reminder.AssignedToUserId != userId) return false;

            reminder.Status = "Termine";
            reminder.CompletedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(reminder);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<List<ReminderDto>> GetAssignedToMeAsync(int userId, ReminderFilterDto filter)
        {
            filter.AssignedToUserId = userId;
            var reminders = await _repository.GetAllTeamAsync(
                filter.Type, filter.Priority, filter.Status,
                filter.ClientId, filter.AssignedToUserId, filter.DateFrom, filter.DateTo);
            return reminders.Select(MapToDto).ToList();
        }

        public async Task<bool> UpdateStatusAsync(int id, string status, int userId)
        {
            var reminder = await _repository.GetByIdAsync(id);
            if (reminder == null) return false;

            if (reminder.Status == "Termine") return false;

            if (reminder.AssignedToUserId != userId && reminder.CreatedByUserId != userId)
                return false;

            reminder.Status = status;
            if (status == "Termine")
                reminder.CompletedAt = DateTime.UtcNow;
            else
                reminder.CompletedAt = null;

            await _repository.UpdateAsync(reminder);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<ReminderDto> CreateDocumentReminderAsync(int userId, int clientId, string title)
        {
            var reminder = new Reminder
            {
                Title = title,
                Type = "Document",
                Priority = "Haute",
                Status = "EnAttente",
                DueDate = DateTime.UtcNow.AddDays(2),
                ClientId = clientId,
                CreatedByUserId = userId,
                DateCreation = DateTime.UtcNow
            };

            await _repository.AddAsync(reminder);
            await _repository.SaveChangesAsync();

            var created = await _repository.GetByIdAsync(reminder.Id);
            return MapToDto(created!);
        }

        public async Task<List<ReminderDto>> GetTeamFilteredAsync(ReminderFilterDto filter)
        {
            var reminders = await _repository.GetAllTeamAsync(
                filter.Type, filter.Priority, filter.Status,
                filter.ClientId, filter.AssignedToUserId, filter.DateFrom, filter.DateTo);
            return reminders.Select(MapToDto).ToList();
        }

        public async Task<ReminderStatsDto> GetTeamStatsAsync()
        {
            return new ReminderStatsDto
            {
                Total = await _repository.GetAllTeamCountAsync(null),
                Pending = await _repository.GetAllTeamCountAsync("EnAttente"),
                InProgress = await _repository.GetAllTeamCountAsync("EnCours"),
                Completed = await _repository.GetAllTeamCountAsync("Termine"),
                Overdue = await _repository.GetAllTeamOverdueCountAsync(),
                Urgent = await _repository.GetAllTeamUrgentPendingCountAsync()
            };
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

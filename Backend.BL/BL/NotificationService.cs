using AutoMapper;
using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Notification;
using Microsoft.Extensions.Logging;

namespace Backend.BL.BL
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(INotificationRepository repository, IMapper mapper, ILogger<NotificationService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<NotificationDto>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 20)
        {
            var notifications = await _repository.GetByUserIdAsync(userId, page, pageSize);
            return _mapper.Map<List<NotificationDto>>(notifications);
        }

        public async Task<int> GetNonLuCountAsync(int userId)
        {
            return await _repository.GetNonLuCountAsync(userId);
        }

        public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto)
        {
            var notification = new Notification
            {
                UserId = dto.UserId,
                Titre = dto.Titre,
                Message = dto.Message,
                Type = dto.Type,
                Lien = dto.Lien,
                DateCreation = DateTime.UtcNow
            };
            await _repository.AddAsync(notification);
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Notification créée pour l'utilisateur {UserId}: {Titre}", dto.UserId, dto.Titre);
            return _mapper.Map<NotificationDto>(notification);
        }

        public async Task<bool> MarkAsLuAsync(int id, int userId)
        {
            var notification = await _repository.GetByIdAsync(id);
            if (notification == null || notification.UserId != userId)
                return false;

            notification.EstLu = true;
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task MarkAllAsLuAsync(int userId)
        {
            var notifications = await _repository.GetByUserIdAsync(userId, 1, int.MaxValue);
            foreach (var n in notifications.Where(n => !n.EstLu))
            {
                n.EstLu = true;
            }
            await _repository.SaveChangesAsync();
        }
    }
}

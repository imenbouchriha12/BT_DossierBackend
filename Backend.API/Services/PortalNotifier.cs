using Backend.API.Hubs;
using Backend.BL.IBL;
using Backend.DTO.Notification;
using Microsoft.AspNetCore.SignalR;

namespace Backend.API.Services
{
    /// <summary>
    /// Implémentation de IPortalNotifier utilisant SignalR.
    /// Vit dans Backend.API pour accéder au Hub sans créer de dépendance circulaire.
    /// </summary>
    public class PortalNotifier : IPortalNotifier
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<PortalNotifier> _logger;

        public PortalNotifier(IHubContext<NotificationHub> hubContext, ILogger<PortalNotifier> logger)
        {
            _hubContext = hubContext;
            _logger     = logger;
        }

        public async Task NotifyNotificationAsync(NotificationDto notif)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"user_{notif.UserId}")
                    .SendAsync("ReceiveNotification", notif);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Échec notification SignalR → user_{UserId}", notif.UserId);
            }
        }

        public async Task NotifyUserAsync(int userId, string titre, string message, string type, string lien)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"user_{userId}")
                    .SendAsync("ReceiveNotification", new
                    {
                        titre,
                        message,
                        type,
                        lien,
                        dateCreation = DateTime.UtcNow
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Échec notification SignalR → user_{UserId}", userId);
            }
        }

        public async Task NotifyGroupAsync(string groupName, string titre, string message, string type, string lien)
        {
            try
            {
                await _hubContext.Clients
                    .Group(groupName)
                    .SendAsync("ReceiveNotification", new
                    {
                        titre,
                        message,
                        type,
                        lien,
                        dateCreation = DateTime.UtcNow
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Échec notification SignalR → groupe {GroupName}", groupName);
            }
        }
    }
}

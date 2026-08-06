using Backend.BL.BL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.API.Hubs
{
    /// <summary>
    /// Hub SignalR de la messagerie interne.
    /// Chaque utilisateur authentifié rejoint le groupe "user_{id}" pour recevoir ses messages en temps réel.
    /// La présence (en ligne / hors ligne) est suivie en mémoire.
    /// </summary>
    [Authorize]
    public class MessagesHub : Hub
    {
        private readonly ILogger<MessagesHub> _logger;

        public MessagesHub(ILogger<MessagesHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = ExtraireUserId();
            if (userId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
                PresenceUtilisateurs.Connecter(userId.Value);
                await NotifierPresence(userId.Value, enLigne: true);
                _logger.LogInformation("Messagerie: utilisateur {UserId} connecté ({ConnectionId})", userId.Value, Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = ExtraireUserId();
            if (userId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
                PresenceUtilisateurs.Deconnecter(userId.Value);
                await NotifierPresence(userId.Value, enLigne: false);
                _logger.LogInformation("Messagerie: utilisateur {UserId} déconnecté ({ConnectionId})", userId.Value, Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
        }

        private int? ExtraireUserId()
        {
            var value = Context.User?.FindFirst("userId")?.Value
                        ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }

        private async Task NotifierPresence(int userId, bool enLigne)
        {
            await Clients.Group($"user_{userId}").SendAsync("PresenceChanged", new { userId, enLigne });
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.API.Hubs
{
    /// <summary>
    /// Hub SignalR pour la diffusion temps réel des cours de change.
    /// Le service d'arrière-plan publie les nouvelles valeurs sur l'événement « CoursMaj »,
    /// reçu par toutes les pages connectées (cours de change, tableau de bord).
    /// </summary>
    [Authorize]
    public class CoursHub : Hub
    {
        private readonly ILogger<CoursHub> _logger;

        public CoursHub(ILogger<CoursHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("userId")?.Value
                      ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            _logger.LogInformation("SignalR cours: {User} connecté ({ConnectionId})", userId ?? "anonyme", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("userId")?.Value
                      ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            _logger.LogInformation("SignalR cours: {User} déconnecté ({ConnectionId})", userId ?? "anonyme", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}

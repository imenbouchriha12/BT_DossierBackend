using Backend.API.Hubs;
using Backend.BL.IBL;
using Backend.DTO.Message;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUser;
        private readonly IHubContext<MessagesHub> _hubContext;

        public MessagesController(IMessageService messageService, ICurrentUserService currentUser, IHubContext<MessagesHub> hubContext)
        {
            _messageService = messageService;
            _currentUser = currentUser;
            _hubContext = hubContext;
        }

        /// Liste des conversations du connecté (dernier message + non lus + présence)
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var conversations = await _messageService.GetConversationsAsync(_currentUser.UserId);
            return Ok(conversations);
        }

        /// Nombre total de messages non lus du connecté
        [HttpGet("non-lu")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _messageService.CountUnreadAsync(_currentUser.UserId);
            return Ok(new { totalNonLu = count });
        }

        /// Historique paginé des messages entre le connecté et un autre utilisateur
        [HttpGet("avec/{userId}")]
        public async Task<IActionResult> GetWithUser(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var resultat = await _messageService.GetMessagesAsync(_currentUser.UserId, userId, page, pageSize);
            return Ok(resultat);
        }

        /// Envoi d'un message : sauvegarde + diffusion temps réel (SignalR)
        [HttpPost("envoyer")]
        public async Task<IActionResult> Send([FromBody] SendMessageDto dto)
        {
            var expediteurId = _currentUser.UserId;
            MessageDto message;
            try
            {
                message = await _messageService.SendAsync(expediteurId, dto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            var nonLuDestinataire = await _messageService.CountUnreadAsync(dto.DestinataireUserId);

            // Diffusion au destinataire (et à ses autres onglets) + mise à jour du compteur non lu
            await _hubContext.Clients.Group($"user_{dto.DestinataireUserId}").SendAsync("RecevoirMessage", message);
            await _hubContext.Clients.Group($"user_{dto.DestinataireUserId}").SendAsync("MettreAJourNonLu", new { totalNonLu = nonLuDestinataire });

            // Diffusion à l'expéditeur (multi-onglets) pour rafraîchir sa liste de conversations
            await _hubContext.Clients.Group($"user_{expediteurId}").SendAsync("RecevoirMessage", message);

            return Ok(message);
        }

        /// Marque la conversation avec un utilisateur comme lue (les deux sens), et notifie l'autre partie
        [HttpPut("lire/{userId}")]
        public async Task<IActionResult> MarkRead(int userId)
        {
            var lecteurId = _currentUser.UserId;
            var nonLuRestant = await _messageService.MarkConversationReadAsync(lecteurId, userId);

            await _hubContext.Clients.Group($"user_{userId}").SendAsync("MessagesLus", new { userId = lecteurId });

            return Ok(new { totalNonLu = nonLuRestant });
        }
    }
}

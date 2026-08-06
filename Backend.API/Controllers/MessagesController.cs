using Backend.API.Hubs;
using Backend.BL.BL;
using Backend.BL.IBL;
using Backend.DTO.Message;
using Backend.DTO.Notification;
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
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUser;
        private readonly IHubContext<MessagesHub> _hubContext;
        private readonly IHubContext<NotificationHub> _notificationHubContext;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(
            IMessageService messageService,
            INotificationService notificationService,
            ICurrentUserService currentUser,
            IHubContext<MessagesHub> hubContext,
            IHubContext<NotificationHub> notificationHubContext,
            ILogger<MessagesController> logger)
        {
            _messageService = messageService;
            _notificationService = notificationService;
            _currentUser = currentUser;
            _hubContext = hubContext;
            _notificationHubContext = notificationHubContext;
            _logger = logger;
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

        /// Identifiants des utilisateurs actuellement connectés à la messagerie
        [HttpGet("en-ligne")]
        public IActionResult GetEnLigne()
        {
            return Ok(PresenceUtilisateurs.UtilisateursEnLigne());
        }

        /// Téléversement d'une pièce jointe (image ou document) avant envoi du message
        [HttpPost("upload")]
        [RequestSizeLimit(15 * 1024 * 1024)]
        public async Task<IActionResult> UploadPieceJointe(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Fichier requis." });
            if (file.Length > 15 * 1024 * 1024)
                return BadRequest(new { message = "Le fichier ne doit pas dépasser 15 Mo." });

            var allowed = new Dictionary<string, string>
            {
                [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png",
                [".gif"] = "image/gif", [".webp"] = "image/webp", [".bmp"] = "image/bmp",
                [".pdf"] = "application/pdf", [".doc"] = "application/msword",
                [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                [".xls"] = "application/vnd.ms-excel",
                [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                [".ppt"] = "application/vnd.ms-powerpoint",
                [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                [".txt"] = "text/plain", [".zip"] = "application/zip"
            };

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.TryGetValue(ext, out var contentType))
                return BadRequest(new { message = $"Format '{ext}' non autorisé (image ou document)." });

            try
            {
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "messages");
                Directory.CreateDirectory(uploadDir);

                var nomFichier = $"{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadDir, nomFichier);

                await using (var stream = System.IO.File.Create(filePath))
                {
                    await file.CopyToAsync(stream);
                }

                var resultat = new UploadPieceJointeDto
                {
                    Url = $"/uploads/messages/{nomFichier}",
                    Nom = file.FileName,
                    Type = contentType
                };
                return Ok(resultat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Échec de l'upload de pièce jointe");
                return StatusCode(500, new { message = "Erreur pendant le téléversement du fichier." });
            }
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

            // Notification in-app au destinataire (cloche + temps réel SignalR)
            try
            {
                var apercu = string.IsNullOrWhiteSpace(message.Contenu)
                    ? (message.PieceJointeNom != null ? "📎 " + message.PieceJointeNom : "Pièce jointe")
                    : message.Contenu;
                if (apercu.Length > 120) apercu = apercu[..120] + "…";

                var notif = await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    UserId = dto.DestinataireUserId,
                    Titre = "Nouveau message",
                    Message = $"{(message.ExpediteurNom ?? "Un utilisateur")} : {apercu}",
                    Type = "Message",
                    Lien = "/messagerie"
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Échec de la création/diffusion de la notification de message");
            }

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

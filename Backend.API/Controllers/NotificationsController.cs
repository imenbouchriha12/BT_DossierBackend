using Backend.BL.IBL;
using Backend.DTO.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUser;

        public NotificationsController(INotificationService notificationService, ICurrentUserService currentUser)
        {
            _notificationService = notificationService;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = _currentUser.UserId;
            var notifications = await _notificationService.GetByUserIdAsync(userId, page, pageSize);
            return Ok(notifications);
        }

        [HttpGet("non-lu/count")]
        public async Task<IActionResult> GetNonLuCount()
        {
            var userId = _currentUser.UserId;
            var count = await _notificationService.GetNonLuCountAsync(userId);
            return Ok(new NotificationCountDto { TotalNonLu = count });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> Create([FromBody] CreateNotificationDto dto)
        {
            var notification = await _notificationService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetAll), new { id = notification.Id }, notification);
        }

        [HttpPut("{id}/lu")]
        public async Task<IActionResult> MarkAsLu(int id)
        {
            var userId = _currentUser.UserId;
            var success = await _notificationService.MarkAsLuAsync(id, userId);
            if (!success)
                return NotFound(new { message = "Notification introuvable ou accès refusé." });
            return Ok(new { message = "Notification marquée comme lue." });
        }

        [HttpPut("marquer-tout-lu")]
        public async Task<IActionResult> MarkAllAsLu()
        {
            var userId = _currentUser.UserId;
            await _notificationService.MarkAllAsLuAsync(userId);
            return Ok(new { message = "Toutes les notifications ont été marquées comme lues." });
        }
    }
}

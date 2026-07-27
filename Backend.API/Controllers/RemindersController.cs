using Backend.BL.IBL;
using Backend.DTO.Reminder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RemindersController : ControllerBase
    {
        private readonly IReminderService _reminderService;
        private readonly ICurrentUserService _currentUser;

        public RemindersController(IReminderService reminderService, ICurrentUserService currentUser)
        {
            _reminderService = reminderService;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var userId = _currentUser.UserId;
            var reminders = await _reminderService.GetAllAsync(userId, page, pageSize);
            return Ok(reminders);
        }

        [HttpGet("filtered")]
        public async Task<IActionResult> GetFiltered([FromQuery] ReminderFilterDto filter)
        {
            var userId = _currentUser.UserId;
            var reminders = await _reminderService.GetFilteredAsync(userId, filter);
            return Ok(reminders);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var userId = _currentUser.UserId;
            var stats = await _reminderService.GetStatsAsync(userId);
            return Ok(stats);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var reminder = await _reminderService.GetByIdAsync(id);
            if (reminder == null) return NotFound(new { message = "Rappel introuvable." });
            return Ok(reminder);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReminderDto dto)
        {
            var userId = _currentUser.UserId;
            var reminder = await _reminderService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = reminder.Id }, reminder);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateReminderDto dto)
        {
            var reminder = await _reminderService.UpdateAsync(id, dto);
            if (reminder == null) return NotFound(new { message = "Rappel introuvable." });
            return Ok(reminder);
        }

        [HttpPut("{id}/complete")]
        public async Task<IActionResult> Complete(int id)
        {
            var userId = _currentUser.UserId;
            var success = await _reminderService.CompleteAsync(id, userId);
            if (!success) return NotFound(new { message = "Rappel introuvable." });
            return Ok(new { message = "Rappel marqué comme terminé." });
        }

        [HttpPut("{id}/sign")]
        public async Task<IActionResult> Sign(int id)
        {
            var userId = _currentUser.UserId;
            var success = await _reminderService.SignAsync(id, userId);
            if (!success) return NotFound(new { message = "Tâche introuvable ou non assignée." });
            return Ok(new { message = "Tâche signée et marquée comme terminée." });
        }

        [HttpGet("assigned-to-me")]
        public async Task<IActionResult> GetAssignedToMe([FromQuery] ReminderFilterDto filter)
        {
            var userId = _currentUser.UserId;
            var reminders = await _reminderService.GetAssignedToMeAsync(userId, filter);
            return Ok(reminders);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateReminderStatusDto dto)
        {
            var userId = _currentUser.UserId;
            var success = await _reminderService.UpdateStatusAsync(id, dto.Status, userId);
            if (!success) return NotFound(new { message = "Rappel introuvable ou non assigné à vous." });
            return Ok(new { message = "Statut mis à jour.", status = dto.Status });
        }

        [HttpPost("from-document")]
        public async Task<IActionResult> CreateFromDocument([FromBody] CreateDocumentReminderDto dto)
        {
            var userId = _currentUser.UserId;
            var reminder = await _reminderService.CreateDocumentReminderAsync(userId, dto.ClientId, dto.Title);
            return CreatedAtAction(nameof(GetById), new { id = reminder.Id }, reminder);
        }

        [HttpGet("team")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> GetTeam([FromQuery] ReminderFilterDto filter)
        {
            var reminders = await _reminderService.GetTeamFilteredAsync(filter);
            return Ok(reminders);
        }

        [HttpGet("team/stats")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> GetTeamStats()
        {
            var stats = await _reminderService.GetTeamStatsAsync();
            return Ok(stats);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _reminderService.DeleteAsync(id);
            if (!success) return NotFound(new { message = "Rappel introuvable." });
            return Ok(new { message = "Rappel supprimé." });
        }
    }
}

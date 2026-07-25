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
            var success = await _reminderService.CompleteAsync(id);
            if (!success) return NotFound(new { message = "Rappel introuvable." });
            return Ok(new { message = "Rappel marqué comme terminé." });
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

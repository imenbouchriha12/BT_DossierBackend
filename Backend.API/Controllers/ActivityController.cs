using Backend.BL.IBL;
using Backend.DTO.Activity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/clients/{clientId}/activity")]
    [Authorize]
    public class ActivityController : ControllerBase
    {
        private readonly IActivityService _activityService;
        private readonly ILogger<ActivityController> _logger;

        public ActivityController(IActivityService activityService, ILogger<ActivityController> logger)
        {
            _activityService = activityService;
            _logger = logger;
        }

        /// Fil d'activité (timeline) paginé du client
        [HttpGet("evenements")]
        public async Task<IActionResult> GetEvenements(int clientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _activityService.GetEvenementsAsync(clientId, page, pageSize);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Client introuvable." });
            }
        }

        /// Notes internes du client
        [HttpGet("notes")]
        public async Task<IActionResult> GetNotes(int clientId)
        {
            try
            {
                var notes = await _activityService.GetNotesAsync(clientId);
                return Ok(notes);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Client introuvable." });
            }
        }

        [HttpPost("notes")]
        public async Task<IActionResult> CreateNote(int clientId, [FromBody] CreateNoteClientDto dto)
        {
            try
            {
                dto.IdClient = clientId;
                if (string.IsNullOrWhiteSpace(dto.Contenu))
                    return BadRequest(new { message = "Le contenu de la note est obligatoire." });

                var note = await _activityService.CreateNoteAsync(dto);
                return CreatedAtAction(nameof(GetNotes), new { clientId }, note);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Client introuvable." });
            }
        }

        [HttpPut("notes/{noteId}")]
        public async Task<IActionResult> UpdateNote(int clientId, int noteId, [FromBody] UpdateNoteClientDto dto)
        {
            try
            {
                var note = await _activityService.UpdateNoteAsync(noteId, dto);
                return Ok(note);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Note introuvable." });
            }
        }

        [HttpDelete("notes/{noteId}")]
        public async Task<IActionResult> DeleteNote(int clientId, int noteId)
        {
            var deleted = await _activityService.DeleteNoteAsync(noteId);
            if (!deleted)
                return NotFound(new { message = "Note introuvable." });
            return NoContent();
        }

        /// Contacts du client
        [HttpGet("contacts")]
        public async Task<IActionResult> GetContacts(int clientId)
        {
            try
            {
                var contacts = await _activityService.GetContactsAsync(clientId);
                return Ok(contacts);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Client introuvable." });
            }
        }

        [HttpPost("contacts")]
        public async Task<IActionResult> CreateContact(int clientId, [FromBody] CreateContactClientDto dto)
        {
            try
            {
                dto.IdClient = clientId;
                if (string.IsNullOrWhiteSpace(dto.Nom))
                    return BadRequest(new { message = "Le nom du contact est obligatoire." });

                var contact = await _activityService.CreateContactAsync(dto);
                return CreatedAtAction(nameof(GetContacts), new { clientId }, contact);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Client introuvable." });
            }
        }

        [HttpPut("contacts/{contactId}")]
        public async Task<IActionResult> UpdateContact(int clientId, int contactId, [FromBody] UpdateContactClientDto dto)
        {
            try
            {
                var contact = await _activityService.UpdateContactAsync(contactId, dto);
                return Ok(contact);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Contact introuvable." });
            }
        }

        [HttpDelete("contacts/{contactId}")]
        public async Task<IActionResult> DeleteContact(int clientId, int contactId)
        {
            var deleted = await _activityService.DeleteContactAsync(contactId);
            if (!deleted)
                return NotFound(new { message = "Contact introuvable." });
            return NoContent();
        }
    }
}

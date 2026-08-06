using Backend.BL.IBL;
using Backend.DTO.Fraud;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FraudController : ControllerBase
    {
        private readonly IFraudDetectionService _fraudService;
        private readonly ICurrentUserService _currentUser;

        public FraudController(IFraudDetectionService fraudService, ICurrentUserService currentUser)
        {
            _fraudService = fraudService;
            _currentUser = currentUser;
        }

        // ---------------------------------------------------------------- Alertes

        [HttpGet("alertes")]
        public async Task<IActionResult> GetAlertes([FromQuery] AlertesFraudeSearchDto search)
        {
            var (items, total) = await _fraudService.GetAlertesAsync(search);
            return Ok(new { items, total, page = search.Page, pageSize = search.PageSize });
        }

        [HttpGet("alertes/client/{clientId}")]
        public async Task<IActionResult> GetAlertesByClient(int clientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var items = await _fraudService.GetAlertesByClientAsync(clientId, page, pageSize);
            return Ok(items);
        }

        [HttpGet("alertes/recentes")]
        public async Task<IActionResult> GetAlertesRecentes([FromQuery] int count = 10)
        {
            var items = await _fraudService.GetAlertesRecentesAsync(count);
            return Ok(items);
        }

        [HttpGet("alertes/{id}")]
        public async Task<IActionResult> GetAlerte(int id)
        {
            var alerte = await _fraudService.GetAlerteAsync(id);
            if (alerte == null) return NotFound(new { message = "Alerte introuvable." });
            return Ok(alerte);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _fraudService.GetStatsAsync();
            return Ok(stats);
        }

        [HttpPut("alertes/{id}/traiter")]
        public async Task<IActionResult> Traiter(int id, [FromBody] TraiterAlerteFraudeDto dto)
        {
            var success = await _fraudService.TraiterAlerteAsync(id, dto.Statut, dto.Commentaire, _currentUser.UserId);
            if (!success) return NotFound(new { message = "Alerte introuvable." });
            return Ok(new { message = "Alerte traitée." });
        }

        // ---------------------------------------------------------------- Analyse

        [HttpPost("analyser/{clientId}")]
        public async Task<IActionResult> AnalyserClient(int clientId)
        {
            var resultat = await _fraudService.AnalyserClientAsync(clientId, _currentUser.UserId);
            return Ok(resultat);
        }

        [HttpPost("analyser")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> AnalyserTous()
        {
            var alertesCrees = await _fraudService.AnalyserTousAsync();
            return Ok(new { alertesCrees, message = $"Analyse terminée : {alertesCrees} nouvelle(s) alerte(s)." });
        }

        // ---------------------------------------------------------------- Règles

        [HttpGet("regles")]
        public async Task<IActionResult> GetRegles()
        {
            var regles = await _fraudService.GetReglesAsync();
            return Ok(regles);
        }

        [HttpPost("regles")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> CreateRegle([FromBody] CreateRegleAntifraudeDto dto)
        {
            var regle = await _fraudService.CreateRegleAsync(dto);
            return CreatedAtAction(nameof(GetRegles), new { }, regle);
        }

        [HttpPut("regles/{id}")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> UpdateRegle(int id, [FromBody] UpdateRegleAntifraudeDto dto)
        {
            var regle = await _fraudService.UpdateRegleAsync(id, dto);
            if (regle == null) return NotFound(new { message = "Règle introuvable." });
            return Ok(regle);
        }

        [HttpPut("regles/{id}/toggle")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> ToggleRegle(int id)
        {
            var success = await _fraudService.ToggleRegleAsync(id);
            if (!success) return NotFound(new { message = "Règle introuvable." });
            return Ok(new { message = "Règle activée/désactivée." });
        }

        [HttpDelete("regles/{id}")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> DeleteRegle(int id)
        {
            var success = await _fraudService.DeleteRegleAsync(id);
            if (!success) return NotFound(new { message = "Règle introuvable." });
            return Ok(new { message = "Règle supprimée." });
        }
    }
}

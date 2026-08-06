using Backend.BL.IBL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RiskController : ControllerBase
    {
        private readonly IRiskScoreService _riskService;

        public RiskController(IRiskScoreService riskService)
        {
            _riskService = riskService;
        }

        /// Rapport de risque du dernier calcul (null si non calculé)
        [HttpGet("{clientId}/rapport")]
        public async Task<IActionResult> GetRapport(int clientId)
        {
            var rapport = await _riskService.GetRapportAsync(clientId);
            if (rapport == null) return NotFound(new { message = "Score de risque non calculé pour ce client." });
            return Ok(rapport);
        }

        /// Calcule (ou recalcule) le score de risque du client
        [HttpPost("{clientId}/calculer")]
        public async Task<IActionResult> Calculer(int clientId)
        {
            var rapport = await _riskService.CalculerAsync(clientId);
            return Ok(rapport);
        }

        [HttpGet("{clientId}/historique")]
        public async Task<IActionResult> GetHistorique(int clientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var historique = await _riskService.GetHistoriqueAsync(clientId, page, pageSize);
            return Ok(historique);
        }

        /// Rapports de risque (dernier) pour une liste d'ids clients
        [HttpPost("rapports")]
        public async Task<IActionResult> GetRapportsPourClients([FromBody] List<int> idClients)
        {
            var rapports = await _riskService.GetRapportsPourClientsAsync(idClients ?? new List<int>());
            return Ok(rapports);
        }
    }
}

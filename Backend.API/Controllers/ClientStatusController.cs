using Backend.BL.BL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/clients/{clientId}/[controller]")]
    [Authorize]
    public class ClientStatusController : ControllerBase
    {
        private readonly ClientStatusManager _statusManager;
        private readonly ILogger<ClientStatusController> _logger;

        public ClientStatusController(
            ClientStatusManager statusManager,
            ILogger<ClientStatusController> logger)
        {
            _statusManager = statusManager;
            _logger = logger;
        }

        /// <summary>
        /// Obtenir le rapport de statut détaillé d'un client
        /// GET /api/clients/15/clientstatus/report
        /// </summary>
        [HttpGet("report")]
        public async Task<IActionResult> GetStatusReport(int clientId)
        {
            try
            {
                var report = await _statusManager.GetStatusReportAsync(clientId);
                return Ok(report);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtenir la liste des documents manquants
        /// GET /api/clients/15/clientstatus/missing-documents
        /// </summary>
        [HttpGet("missing-documents")]
        public async Task<IActionResult> GetMissingDocuments(int clientId)
        {
            try
            {
                var missing = await _statusManager.GetDocumentsManquantsAsync(clientId);
                return Ok(new { clientId, documentsManquants = missing });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Recalculer manuellement le statut d'un client
        /// POST /api/clients/15/clientstatus/recalculate
        /// </summary>
        [HttpPost("recalculate")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> RecalculateStatus(int clientId)
        {
            try
            {
                var newStatus = await _statusManager.RecalculerStatutClientAsync(clientId);
                return Ok(new { 
                    clientId, 
                    nouveauStatut = newStatus.ToString(),
                    message = "Statut recalculé avec succès" 
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Bloquer manuellement un client
        /// POST /api/clients/15/clientstatus/block
        /// </summary>
        [HttpPost("block")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> BlockClient(int clientId, [FromBody] BlockClientDto dto)
        {
            try
            {
                await _statusManager.BloquerClientAsync(clientId, dto.Raison);
                return Ok(new { 
                    clientId, 
                    statut = "Bloqué",
                    message = "Client bloqué avec succès" 
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Débloquer un client
        /// POST /api/clients/15/clientstatus/unblock
        /// </summary>
        [HttpPost("unblock")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> UnblockClient(int clientId)
        {
            try
            {
                await _statusManager.DebloquerClientAsync(clientId);
                return Ok(new { 
                    clientId,
                    message = "Client débloqué - statut recalculé automatiquement" 
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }

    public class BlockClientDto
    {
        public string Raison { get; set; } = string.Empty;
    }
}

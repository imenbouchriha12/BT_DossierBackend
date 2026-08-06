using Backend.BL.IBL;
using Backend.DTO.Workflow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/workflow")]
    [Authorize]
    public class WorkflowController : ControllerBase
    {
        private readonly IWorkflowService _workflowService;
        private readonly ILogger<WorkflowController> _logger;

        public WorkflowController(IWorkflowService workflowService, ILogger<WorkflowController> logger)
        {
            _workflowService = workflowService;
            _logger = logger;
        }

        /// Workflow le plus récent d'un document
        [HttpGet("document/{documentId}")]
        public async Task<IActionResult> GetByDocument(int documentId)
        {
            var workflow = await _workflowService.GetByDocumentIdAsync(documentId);
            if (workflow == null)
                return NotFound(new { message = "Aucun workflow pour ce document." });
            return Ok(workflow);
        }

        /// Historique complet des validations d'un document
        [HttpGet("document/{documentId}/history")]
        public async Task<IActionResult> GetHistory(int documentId)
        {
            var history = await _workflowService.GetHistoryByDocumentIdAsync(documentId);
            return Ok(history);
        }

        /// Derniers workflows d'une liste de documents (ids séparés par des virgules)
        [HttpGet("by-documents")]
        public async Task<IActionResult> GetByDocuments([FromQuery] string ids)
        {
            var parsed = new List<int>();
            foreach (var part in (ids ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part.Trim(), out var id))
                    parsed.Add(id);
            }
            var result = await _workflowService.GetByDocumentIdsAsync(parsed);
            return Ok(result);
        }

        /// Inbox : workflows en attente pour l'utilisateur connecté
        [HttpGet("en-attente")]
        public async Task<IActionResult> GetEnAttente([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _workflowService.GetEnAttenteAsync(page, pageSize);
            return Ok(result);
        }

        /// Nombre de workflows en attente pour l'utilisateur connecté
        [HttpGet("en-attente/count")]
        public async Task<IActionResult> GetEnAttenteCount()
        {
            var count = await _workflowService.GetEnAttenteCountAsync();
            return Ok(new { count });
        }

        /// Statistiques globales des workflows
        [HttpGet("stats")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _workflowService.GetStatsAsync();
            return Ok(stats);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var workflow = await _workflowService.GetByIdAsync(id);
            if (workflow == null)
                return NotFound(new { message = "Workflow introuvable." });
            return Ok(workflow);
        }

        /// Valide l'étape courante du workflow
        [HttpPost("{id}/valider")]
        [Authorize(Roles = "Admin,Agent,Superviseur")]
        public async Task<IActionResult> Valider(int id, [FromBody] ValidateWorkflowDto dto)
        {
            try
            {
                var workflow = await _workflowService.ValiderAsync(id, dto.Commentaire);
                return Ok(workflow);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Workflow introuvable." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// Rejette le workflow (motif obligatoire)
        [HttpPost("{id}/rejeter")]
        [Authorize(Roles = "Admin,Agent,Superviseur")]
        public async Task<IActionResult> Rejeter(int id, [FromBody] RejectWorkflowDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Commentaire))
                    return BadRequest(new { message = "Le motif de rejet est obligatoire." });

                var workflow = await _workflowService.RejeterAsync(id, dto.Commentaire);
                return Ok(workflow);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Workflow introuvable." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// Relance un workflow rejeté (nouvelle instance, historique conservé)
        [HttpPost("{id}/relancer")]
        [Authorize(Roles = "Admin,Agent,Superviseur")]
        public async Task<IActionResult> Relancer(int id, [FromBody] ResubmitWorkflowDto dto)
        {
            try
            {
                var workflow = await _workflowService.RelancerAsync(id, dto.Commentaire);
                return Ok(workflow);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Workflow introuvable." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

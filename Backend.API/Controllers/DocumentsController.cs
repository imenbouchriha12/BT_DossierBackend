using Backend.BL.IBL;
using Backend.DTO.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly IWorkflowService _workflowService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(IDocumentService documentService, IWorkflowService workflowService, ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _workflowService = workflowService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> GetAll()
        {
            var docs = await _documentService.GetAllAsync();
            return Ok(docs);
        }

        [HttpGet("client/{clientId}")]
        [Authorize(Roles = "Admin,Superviseur,Agent")]
        public async Task<IActionResult> GetByClient(int clientId)
        {
            var docs = await _documentService.GetByClientIdAsync(clientId);
            return Ok(docs);
        }

        [HttpGet("client/{clientId}/paged")]
        [Authorize(Roles = "Admin,Superviseur,Agent")]
        public async Task<IActionResult> GetByClientPaged(int clientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _documentService.GetByClientIdPagedAsync(clientId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var doc = await _documentService.GetByIdAsync(id);
            if (doc == null)
                return NotFound(new { message = "Document introuvable." });
            return Ok(doc);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Agent,Superviseur")]
        public async Task<IActionResult> Create([FromForm] int idClient, [FromForm] string typeDocument, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Fichier requis." });

            var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return BadRequest(new { message = $"Format '{ext}' non autorisé." });

            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var statut = User.IsInRole("Admin") ? "Validé" : "En attente";
                var doc = await _documentService.CreateAsync(idClient, typeDocument, file.FileName, ms.ToArray(), statut);

                return CreatedAtAction(nameof(GetById), new { id = doc.Id }, doc);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _documentService.DeleteAsync(id);
                return Ok(new { message = "Document supprimé avec succès." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Document introuvable." });
            }
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(int id)
        {
            try
            {
                var (bytes, fileName, contentType) = await _documentService.DownloadAsync(id);
                return File(bytes, contentType, fileName);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Document introuvable." });
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { message = "Fichier physique introuvable." });
            }
        }

        [HttpPut("{id}/validation")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> UpdateValidation(int id, [FromBody] UpdateDocumentValidationDto dto)
        {
            if (dto.StatutValidation != "Validé" && dto.StatutValidation != "Rejeté")
                return BadRequest(new { message = "Statut invalide. Utilisez : Validé ou Rejeté" });

            try
            {
                var workflow = await _workflowService.GetByDocumentIdAsync(id);
                if (workflow == null)
                    return BadRequest(new { message = "Aucun workflow de validation pour ce document." });

                if (dto.StatutValidation == "Validé")
                    await _workflowService.ValiderAsync(workflow.Id, null);
                else
                    await _workflowService.RejeterAsync(workflow.Id, "Document rejeté.");

                var doc = await _documentService.GetByIdAsync(id);
                return Ok(doc);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Document introuvable." });
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
    }
}

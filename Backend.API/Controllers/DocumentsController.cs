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

        public DocumentsController(IDocumentService documentService)
        {
            _documentService = documentService;
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
            if (dto.StatutValidation != "Validé" && dto.StatutValidation != "En attente" && dto.StatutValidation != "Rejeté")
                return BadRequest(new { message = "Statut invalide. Utilisez : Validé, En attente, Rejeté" });

            try
            {
                var doc = await _documentService.UpdateValidationAsync(id, dto.StatutValidation);
                return Ok(doc);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Document introuvable." });
            }
        }
    }
}

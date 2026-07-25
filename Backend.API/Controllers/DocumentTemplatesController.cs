using Backend.BL.IBL;
using Backend.DTO.Portal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    /// <summary>
    /// Gestion des templates de documents requis par type de client.
    /// Réservé aux admins et superviseurs.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Superviseur")]
    public class DocumentTemplatesController : ControllerBase
    {
        private readonly IDocumentTemplateService _templateService;

        public DocumentTemplatesController(IDocumentTemplateService templateService)
        {
            _templateService = templateService;
        }

        /// GET api/documenttemplates — tous les templates
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var templates = await _templateService.GetAllAsync();
            return Ok(templates);
        }

        /// GET api/documenttemplates/types — liste des TypeClient distincts
        [HttpGet("types")]
        public async Task<IActionResult> GetTypesClients()
        {
            var types = await _templateService.GetTypesClientsAsync();
            return Ok(types);
        }

        /// GET api/documenttemplates/by-type/{typeClient}
        [HttpGet("by-type/{typeClient}")]
        public async Task<IActionResult> GetByTypeClient(string typeClient)
        {
            var templates = await _templateService.GetByTypeClientAsync(typeClient);
            return Ok(templates);
        }

        /// GET api/documenttemplates/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var template = await _templateService.GetByIdAsync(id);
            if (template == null)
                return NotFound(new { message = "Template introuvable." });
            return Ok(template);
        }

        /// POST api/documenttemplates
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateDocumentTemplateDto dto)
        {
            try
            {
                var created = await _templateService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// PUT api/documenttemplates/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDocumentTemplateDto dto)
        {
            try
            {
                var updated = await _templateService.UpdateAsync(id, dto);
                return Ok(updated);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Template introuvable." });
            }
        }

        /// DELETE api/documenttemplates/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _templateService.DeleteAsync(id);
                return Ok(new { message = "Template supprimé avec succès." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Template introuvable." });
            }
        }
    }
}

using Backend.BL.IBL;
using Backend.DTO.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _clientService;

        public ClientsController(IClientService clientService)
        {
            _clientService = clientService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ClientSearchDto search)
        {
            var result = await _clientService.SearchAsync(search);
            return Ok(result);
        }

        [HttpGet("archived")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> GetArchived([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _clientService.GetArchivedAsync(page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var client = await _clientService.GetByIdAsync(id);
            if (client == null)
                return NotFound(new { message = "Client introuvable." });
            return Ok(client);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Agent,Superviseur")]
        public async Task<IActionResult> Create([FromBody] CreateClientDto dto)
        {
            var client = await _clientService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Agent,Superviseur")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateClientDto dto)
        {
            var client = await _clientService.UpdateAsync(id, dto);
            return Ok(client);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Archive(int id)
        {
            await _clientService.ArchiveAsync(id);
            return Ok(new { message = "Client archivé avec succès." });
        }

        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restore(int id)
        {
            await _clientService.RestoreAsync(id);
            return Ok(new { message = "Client restauré avec succès." });
        }

        [HttpPost("import")]
        [Authorize(Roles = "Admin,Agent,Superviseur")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Aucun fichier fourni." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest(new { message = "Format non supporté. Utilisez .xlsx ou .xls." });

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                var imported = await _clientService.ImportExcelAsync(stream, file.FileName);
                return Ok(new { imported, message = $"{imported} client(s) importé(s) avec succès." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] ClientSearchDto search)
        {
            var fileBytes = await _clientService.ExportExcelAsync(search);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"clients_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _clientService.GetStatsAsync();
            return Ok(stats);
        }

        [HttpGet("villes")]
        public async Task<IActionResult> GetVilles()
        {
            var villes = await _clientService.GetAllVillesAsync();
            return Ok(villes);
        }
    }
}

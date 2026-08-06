using Backend.BL.IBL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MouvementsController : ControllerBase
    {
        private readonly IMouvementService _mouvementService;

        public MouvementsController(IMouvementService mouvementService)
        {
            _mouvementService = mouvementService;
        }

        [HttpGet("client/{idClient}")]
        public async Task<IActionResult> GetByClient(int idClient, [FromQuery] DateTime? dateDebut, [FromQuery] DateTime? dateFin, [FromQuery] string? sens, [FromQuery] decimal? montantMin, [FromQuery] decimal? montantMax, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _mouvementService.GetByClientAsync(idClient, dateDebut, dateFin, sens, montantMin, montantMax, page, pageSize);
            return Ok(result);
        }
    }
}

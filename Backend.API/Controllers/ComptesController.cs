using Backend.BL.IBL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ComptesController : ControllerBase
    {
        private readonly ICompteService _compteService;
        private readonly IMouvementService _mouvementService;

        public ComptesController(ICompteService compteService, IMouvementService mouvementService)
        {
            _compteService = compteService;
            _mouvementService = mouvementService;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string? numCompte, [FromQuery] string? agence, [FromQuery] string? devise, [FromQuery] string? codeClient, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _compteService.SearchAsync(numCompte, agence, devise, codeClient, page, pageSize);
            return Ok(result);
        }

        [HttpGet("client/{idClient}")]
        public async Task<IActionResult> GetByClient(int idClient)
        {
            var comptes = await _compteService.GetByClientIdAsync(idClient);
            return Ok(comptes);
        }

        [HttpGet("client/{idClient}/totaux")]
        public async Task<IActionResult> GetTotaux(int idClient)
        {
            var totaux = await _compteService.GetTotauxAsync(idClient);
            return Ok(totaux);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var compte = await _compteService.GetByIdAsync(id);
            if (compte == null)
                return NotFound(new { message = "Compte introuvable." });
            return Ok(compte);
        }

        [HttpGet("{id}/mouvements")]
        public async Task<IActionResult> GetMouvements(int id, [FromQuery] DateTime? dateDebut, [FromQuery] DateTime? dateFin, [FromQuery] string? sens, [FromQuery] decimal? montantMin, [FromQuery] decimal? montantMax, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var compte = await _compteService.GetByIdAsync(id);
            if (compte == null)
                return NotFound(new { message = "Compte introuvable." });

            var result = await _mouvementService.GetByCompteAsync(compte.NumCompte, compte.Agence, dateDebut, dateFin, sens, montantMin, montantMax, page, pageSize);
            return Ok(result);
        }
    }
}

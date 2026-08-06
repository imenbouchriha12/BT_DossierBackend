using Backend.BL.IBL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SynchronisationController : ControllerBase
    {
        private readonly ISynchronisationService _synchronisationService;
        private readonly ICurrentUserService _currentUser;

        public SynchronisationController(ISynchronisationService synchronisationService, ICurrentUserService currentUser)
        {
            _synchronisationService = synchronisationService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Déclenche manuellement le package SSIS via dtexec.
        /// SSIS est responsable du chargement complet (Client, DetailClient,
        /// DocumentClient, Compte, Cours, MVT). Ce service attend la fin
        /// du package et retourne le résultat de l'exécution.
        /// </summary>
        [HttpPost("declencher")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> Declencher()
        {
            try
            {
                var result = await _synchronisationService.DeclencherAsync("Manuel", _currentUser.UserId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                // Synchronisation déjà en cours
                return Conflict(new { message = ex.Message });
            }
            catch (TimeoutException ex)
            {
                return StatusCode(504, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("etat")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> GetEtat()
        {
            var etat = await _synchronisationService.GetEtatAsync();
            return Ok(etat);
        }

        [HttpGet("historique")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> GetHistorique([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var historique = await _synchronisationService.GetHistoriqueAsync(page, pageSize);
            return Ok(historique);
        }

        [HttpGet("rejets/{id}")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> GetRejets(int id)
        {
            var rejets = await _synchronisationService.GetRejetsAsync(id);
            return Ok(rejets);
        }
    }
}

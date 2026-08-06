using Backend.BL.IBL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CoursController : ControllerBase
    {
        private readonly ICoursService _coursService;
        private readonly ICoursTempsReelService _coursTempsReelService;

        public CoursController(ICoursService coursService, ICoursTempsReelService coursTempsReelService)
        {
            _coursService = coursService;
            _coursTempsReelService = coursTempsReelService;
        }

        [HttpGet("temps-reel")]
        public async Task<IActionResult> GetTempsReel()
        {
            var cours = await _coursTempsReelService.GetCoursDuJourAsync();
            return Ok(cours);
        }

        [HttpGet]
        public async Task<IActionResult> GetLatest([FromQuery] string? devise)
        {
            var cours = await _coursService.GetLatestAsync(devise);
            return Ok(cours);
        }

        [HttpGet("{devise}/historique")]
        public async Task<IActionResult> GetHistory(string devise, [FromQuery] DateTime? dateDebut, [FromQuery] DateTime? dateFin)
        {
            var cours = await _coursService.GetHistoryAsync(devise, dateDebut, dateFin);
            return Ok(cours);
        }
    }
}

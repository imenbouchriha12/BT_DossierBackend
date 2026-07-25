using Backend.BL.IBL;
using Backend.DTO.Historique;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Superviseur")]
    public class HistoriqueController : ControllerBase
    {
        private readonly IHistoriqueService _historiqueService;

        public HistoriqueController(IHistoriqueService historiqueService)
        {
            _historiqueService = historiqueService;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] HistoriqueSearchDto search)
        {
            var result = await _historiqueService.SearchAsync(search);
            return Ok(result);
        }

        [HttpGet("tables")]
        public async Task<IActionResult> GetTables()
        {
            var tables = await _historiqueService.GetTablesListAsync();
            return Ok(tables);
        }

        [HttpGet("actions")]
        public async Task<IActionResult> GetActions()
        {
            var actions = await _historiqueService.GetActionsListAsync();
            return Ok(actions);
        }
    }
}

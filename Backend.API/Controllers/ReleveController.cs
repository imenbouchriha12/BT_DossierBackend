using Backend.BL.IBL;
using Backend.DTO.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReleveController : ControllerBase
    {
        private readonly IReleveService _releveService;

        public ReleveController(IReleveService releveService)
        {
            _releveService = releveService;
        }

        [HttpPost("pdf")]
        public async Task<IActionResult> GenerateRelevePdf([FromBody] ReleveRequestDto request)
        {
            try
            {
                var fileBytes = await _releveService.GenerateRelevePdfAsync(request);
                return File(fileBytes, "application/pdf", $"releve_compte_{request.CompteId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("excel")]
        public async Task<IActionResult> GenerateReleveExcel([FromBody] ReleveRequestDto request)
        {
            try
            {
                var fileBytes = await _releveService.GenerateReleveExcelAsync(request);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"releve_compte_{request.CompteId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}

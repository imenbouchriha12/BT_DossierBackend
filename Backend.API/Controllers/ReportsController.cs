using Backend.BL.IBL;
using Backend.DTO.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Superviseur")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpPost("clients/pdf")]
        public async Task<IActionResult> GenerateClientListPdf([FromBody] ReportRequestDto request)
        {
            var fileBytes = await _reportService.GenerateClientListPdfAsync(request);
            return File(fileBytes, "application/pdf", $"clients_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }

        [HttpPost("clients/excel")]
        public async Task<IActionResult> GenerateClientListExcel([FromBody] ReportRequestDto request)
        {
            var fileBytes = await _reportService.GenerateClientListExcelAsync(request);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"clients_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        [HttpGet("stats/pdf")]
        public async Task<IActionResult> GenerateStatsPdf()
        {
            var fileBytes = await _reportService.GenerateStatsPdfAsync();
            return File(fileBytes, "application/pdf", $"statistiques_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }
    }
}

using Backend.DTO.Reports;

namespace Backend.BL.IBL
{
    public interface IReportService
    {
        Task<byte[]> GenerateClientListPdfAsync(ReportRequestDto request);
        Task<byte[]> GenerateStatsPdfAsync();
        Task<byte[]> GenerateClientListExcelAsync(ReportRequestDto request);
    }
}

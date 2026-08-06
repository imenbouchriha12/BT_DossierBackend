using Backend.DTO.Reports;

namespace Backend.BL.IBL
{
    public interface IReleveService
    {
        Task<byte[]> GenerateRelevePdfAsync(ReleveRequestDto request);
        Task<byte[]> GenerateReleveExcelAsync(ReleveRequestDto request);
    }
}

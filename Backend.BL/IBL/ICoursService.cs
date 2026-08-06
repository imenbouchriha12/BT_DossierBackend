using Backend.DTO.Finance;

namespace Backend.BL.IBL
{
    public interface ICoursService
    {
        Task<List<CoursDto>> GetLatestAsync(string? devise);
        Task<List<CoursDto>> GetHistoryAsync(string devise, DateTime? dateDebut, DateTime? dateFin);
    }
}

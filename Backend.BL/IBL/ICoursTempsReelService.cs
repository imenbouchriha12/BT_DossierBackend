using Backend.DTO.Finance;

namespace Backend.BL.IBL
{
    /// <summary>
    /// Fournit les cours de change du jour en temps réel (source externe),
    /// avec repli sur les cours SISS en cas d'indisponibilité de l'API.
    /// </summary>
    public interface ICoursTempsReelService
    {
        Task<List<CoursTempsReelDto>> GetCoursDuJourAsync();
    }
}

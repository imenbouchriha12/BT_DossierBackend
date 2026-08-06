using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    public interface ICoursRepository
    {
        Task<List<Cours>> GetLatestAsync(string? devise);
        Task<Cours?> GetLatestByDeviseAsync(string devise);
        Task<List<Cours>> GetHistoryAsync(string devise, DateTime? dateDebut, DateTime? dateFin);
    }
}

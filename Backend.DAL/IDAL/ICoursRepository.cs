using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    public interface ICoursRepository
    {
        Task<List<Cours>> GetLatestAsync(string? devise);
        Task<Cours?> GetLatestByDeviseAsync(string devise);
        Task<Cours?> GetByJourneeDeviseAsync(DateTime journee, string devise);
        Task<List<Cours>> GetHistoryAsync(string devise, DateTime? dateDebut, DateTime? dateFin);
        Task AddAsync(Cours cours);
        Task SaveChangesAsync();
    }
}

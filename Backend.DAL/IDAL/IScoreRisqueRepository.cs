using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    public interface IScoreRisqueRepository
    {
        Task<ScoreRisque?> GetLatestByClientAsync(int idClient);
        Task<List<ScoreRisque>> GetHistoriqueAsync(int idClient, int page = 1, int pageSize = 50);
        Task<int> CountByClientAsync(int idClient);
        Task AddAsync(ScoreRisque score);
        Task SaveChangesAsync();
        Task<List<ScoreRisque>> GetLatestForClientsAsync(IEnumerable<int> idClients);
    }
}

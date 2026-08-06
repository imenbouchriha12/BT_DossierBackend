using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    public interface IAlertesFraudeRepository
    {
        Task<List<AlertesFraude>> GetByClientAsync(int idClient, int page = 1, int pageSize = 50);
        Task<int> CountByClientAsync(int idClient);
        Task<int> CountActivesByClientAsync(int idClient);
        Task<(List<AlertesFraude> Items, int Total)> SearchAsync(int? idClient, string? statut, string? niveau, string? type, DateTime? dateDebut, DateTime? dateFin, int page, int pageSize);
        Task<AlertesFraude?> GetByIdAsync(int id);
        Task<AlertesFraude?> GetByFingerprintAsync(string fingerprint);
        Task AddAsync(AlertesFraude alerte);
        Task UpdateAsync(AlertesFraude alerte);
        Task SaveChangesAsync();
        Task<List<AlertesFraude>> GetRecentAsync(int count);
        Task<int> CountActivesAsync();
    }
}

using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    public interface IRegleAntifraudeRepository
    {
        Task<List<RegleAntifraude>> GetAllAsync(bool activesSeulement = false);
        Task<RegleAntifraude?> GetByIdAsync(int id);
        Task<RegleAntifraude?> GetByCodeAsync(string code);
        Task AddAsync(RegleAntifraude regle);
        Task UpdateAsync(RegleAntifraude regle);
        Task DeleteAsync(RegleAntifraude regle);
        Task SaveChangesAsync();
    }
}

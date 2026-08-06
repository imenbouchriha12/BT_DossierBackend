using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    public interface ICompteRepository
    {
        Task<Compte?> GetByIdAsync(int id);
        Task<List<Compte>> GetByClientIdAsync(int idClient);
        Task<Compte?> GetByNumeroAgenceAsync(string numCompte, string agence);
        Task<List<Compte>> GetAllAsync();
        Task<List<Compte>> GetByIdsAsync(IEnumerable<int> idClients);
        Task<(List<CompteSearchItem> Items, int Total)> SearchAsync(string? numCompte, string? agence, string? devise, string? codeClient, int page, int pageSize);
    }
}

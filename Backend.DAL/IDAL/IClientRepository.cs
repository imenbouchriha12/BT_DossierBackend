using Backend.DAL.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.DAL.IDAL
{
    public interface IClientRepository
    {
        Task<Client?> GetByIdAsync(int id);
        Task<Client?> GetByIdWithDetailAsync(int id);
        Task<Client?> GetByCodeClientAsync(string codeClient);
        Task<List<Client>> GetAllWithDetailAsync();
        Task<List<Client>> GetAllAsync();
        Task<(List<Client> Items, int Total)> SearchAsync(string? codeClient, string? nom, string? prenom, string? ville, StatutClient? statut, string? typeClient, DateTime? dateDebut, DateTime? dateFin, int page, int pageSize);
        Task<(List<Client> Items, int Total)> GetArchivedAsync(int page, int pageSize);
        Task<bool> CodeClientExistsAsync(string codeClient);
        Task AddAsync(Client client);
        void Delete(Client client);
        Task SaveChangesAsync();
        Task<int> CountAsync();
        Task<int> CountArchivedAsync();
        Task<int> CountByStatutAsync(StatutClient statut);
        Task<int> CountByTypeAndStatutAsync(string? typeClient, StatutClient? statut);
        Task<int> CountByDateRangeAsync(DateTime debut, DateTime fin);
        Task<List<IGrouping<string, Client>>> GroupByVilleAsync();
        Task<List<IGrouping<StatutClient, Client>>> GroupByStatutAsync();
        Task<List<IGrouping<string, Client>>> GroupByTypeAsync();
        Task<List<IGrouping<string, Client>>> GroupByMonthAsync();
        Task<List<Client>> GetRecentAsync(int count);
        Task<int> CountBySexeAsync(string sexe);
        Task<List<string>> GetDistinctVillesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}

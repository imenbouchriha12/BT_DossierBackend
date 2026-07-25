using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    public interface IDocumentRepository
    {
        Task<DocumentClient?> GetByIdAsync(int id);
        Task<List<DocumentClient>> GetByClientIdAsync(int clientId);
        Task<List<DocumentClient>> GetAllAsync();
        Task<(List<DocumentClient> Items, int Total)> GetByClientIdPagedAsync(int clientId, int page, int pageSize);
        Task AddAsync(DocumentClient document);
        void Delete(DocumentClient document);
        Task SaveChangesAsync();
        Task<int> CountAsync();
        Task<int> CountByStatutAsync(string statut);
        Task<List<DocumentClient>> GetRecentAsync(int count);
    }
}

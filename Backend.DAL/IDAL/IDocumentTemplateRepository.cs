using Backend.DAL.Entities;

namespace Backend.DAL.IDAL
{
    public interface IDocumentTemplateRepository
    {
        Task<List<DocumentTemplate>> GetAllAsync();
        Task<List<DocumentTemplate>> GetByTypeClientAsync(string typeClient);
        Task<DocumentTemplate?> GetByIdAsync(int id);
        Task<bool> ExistsAsync(string typeClient, string nomDocument);
        Task AddAsync(DocumentTemplate template);
        void Update(DocumentTemplate template);
        void Delete(DocumentTemplate template);
        Task SaveChangesAsync();
    }
}

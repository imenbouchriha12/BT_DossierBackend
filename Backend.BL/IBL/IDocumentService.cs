using Backend.DTO.Client;

namespace Backend.BL.IBL
{
    public interface IDocumentService
    {
        Task<List<DocumentDto>> GetByClientIdAsync(int clientId);
        Task<PagedResult<DocumentDto>> GetByClientIdPagedAsync(int clientId, int page, int pageSize);
        Task<DocumentDto?> GetByIdAsync(int id);
        Task<DocumentDto> CreateAsync(int idClient, string typeDocument, string fileName, byte[] fileBytes, string statutValidation = "En attente");
        Task DeleteAsync(int id);
        Task<(byte[] FileBytes, string FileName, string ContentType)> DownloadAsync(int id);
        Task<DocumentDto> UpdateValidationAsync(int id, string statut);
        Task<List<DocumentDto>> GetAllAsync();
    }
}

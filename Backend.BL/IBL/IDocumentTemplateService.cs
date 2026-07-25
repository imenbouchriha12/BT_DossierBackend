using Backend.DTO.Portal;

namespace Backend.BL.IBL
{
    public interface IDocumentTemplateService
    {
        Task<List<DocumentTemplateDto>> GetAllAsync();
        Task<List<DocumentTemplateDto>> GetByTypeClientAsync(string typeClient);
        Task<DocumentTemplateDto?> GetByIdAsync(int id);
        Task<DocumentTemplateDto> CreateAsync(CreateDocumentTemplateDto dto);
        Task<DocumentTemplateDto> UpdateAsync(int id, UpdateDocumentTemplateDto dto);
        Task DeleteAsync(int id);

        /// Retourne les types de clients distincts ayant des templates actifs
        Task<List<string>> GetTypesClientsAsync();
    }
}

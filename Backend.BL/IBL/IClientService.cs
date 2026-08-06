using Backend.DTO.Client;

namespace Backend.BL.IBL
{
    public interface IClientService
    {
        Task<PagedResult<ClientDto>> SearchAsync(ClientSearchDto search);
        Task<PagedResult<ClientDto>> GetArchivedAsync(int page, int pageSize);
        Task<ClientDto?> GetByIdAsync(int id);
        Task<ClientDto> CreateAsync(CreateClientDto dto);
        Task<ClientDto> UpdateAsync(int id, UpdateClientDto dto);
        Task ArchiveAsync(int id);
        Task RestoreAsync(int id);
        Task<byte[]> ExportExcelAsync(ClientSearchDto search);
        Task<int> ImportExcelAsync(Stream fileStream, string fileName);
        Task<byte[]> GenerateImportTemplateAsync();
        Task<ClientStatsDto> GetStatsAsync();
        Task<List<string>> GetAllVillesAsync();
    }
}

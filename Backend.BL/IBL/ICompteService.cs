using Backend.DTO.Client;
using Backend.DTO.Finance;

namespace Backend.BL.IBL
{
    public interface ICompteService
    {
        Task<List<CompteDto>> GetByClientIdAsync(int idClient);
        Task<CompteDto?> GetByIdAsync(int id);
        Task<List<CompteTotalDto>> GetTotauxAsync(int idClient);
        Task<PagedResult<CompteDto>> SearchAsync(string? numCompte, string? agence, string? devise, string? codeClient, int page, int pageSize);
    }
}

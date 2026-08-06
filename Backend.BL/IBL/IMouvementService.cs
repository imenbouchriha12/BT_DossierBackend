using Backend.DTO.Client;
using Backend.DTO.Finance;

namespace Backend.BL.IBL
{
    public interface IMouvementService
    {
        Task<PagedResult<MouvementDto>> GetByCompteAsync(string numCompte, string agence, DateTime? dateDebut, DateTime? dateFin, string? sens, decimal? montantMin, decimal? montantMax, int page, int pageSize);
        Task<PagedResult<MouvementDto>> GetByClientAsync(int idClient, DateTime? dateDebut, DateTime? dateFin, string? sens, decimal? montantMin, decimal? montantMax, int page, int pageSize);
    }
}

using Backend.DTO.Synchronisation;

namespace Backend.BL.IBL
{
    public interface ISynchronisationService
    {
        Task<SynchronisationResultDto> DeclencherAsync(string declencheur, int? utilisateurId);
        Task<SynchronisationStateDto> GetEtatAsync();
        Task<List<SynchronisationResultDto>> GetHistoriqueAsync(int page, int pageSize);
        Task<List<SynchronisationRejetDto>> GetRejetsAsync(int idSynchronisation);
    }
}

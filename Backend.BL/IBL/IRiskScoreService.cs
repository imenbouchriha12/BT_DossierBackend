using Backend.DTO.Risk;

namespace Backend.BL.IBL
{
    /// <summary>
    /// Profil de risque client basé sur le solde, les mouvements, la situation familiale
    /// et le type de client. Chaque calcul est archivé dans la table ScoreRisque.
    /// </summary>
    public interface IRiskScoreService
    {
        Task<RapportRisqueDto> CalculerAsync(int idClient);
        Task<RapportRisqueDto?> GetRapportAsync(int idClient);
        Task<HistoriqueRisqueDto> GetHistoriqueAsync(int idClient, int page = 1, int pageSize = 50);
        Task<List<ScoreRisqueDto>> GetRapportsPourClientsAsync(IEnumerable<int> idClients);
    }
}

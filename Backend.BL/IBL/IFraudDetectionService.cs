using Backend.DTO.Fraud;

namespace Backend.BL.IBL
{
    /// <summary>
    /// Moteur de détection de fraude / anomalie sur les mouvements bancaires.
    /// Applique des règles configurables (RegleAntifraude) et génère des alertes (AlertesFraude).
    /// </summary>
    public interface IFraudDetectionService
    {
        Task<AnalyseFraudeResultatDto> AnalyserClientAsync(int idClient, int utilisateurId = 0);
        Task<int> AnalyserTousAsync();
        Task<(List<AlertesFraudeDto> Items, int Total)> GetAlertesAsync(AlertesFraudeSearchDto search);
        Task<List<AlertesFraudeDto>> GetAlertesByClientAsync(int idClient, int page = 1, int pageSize = 50);
        Task<AlertesFraudeDto?> GetAlerteAsync(int id);
        Task<FraudStatsDto> GetStatsAsync();
        Task<List<AlertesFraudeDto>> GetAlertesRecentesAsync(int count = 10);

        Task<List<RegleAntifraudeDto>> GetReglesAsync();
        Task<RegleAntifraudeDto> CreateRegleAsync(CreateRegleAntifraudeDto dto);
        Task<RegleAntifraudeDto?> UpdateRegleAsync(int id, UpdateRegleAntifraudeDto dto);
        Task<bool> ToggleRegleAsync(int id);
        Task<bool> DeleteRegleAsync(int id);

        Task<bool> TraiterAlerteAsync(int id, string statut, string? commentaire, int utilisateurId);
    }
}

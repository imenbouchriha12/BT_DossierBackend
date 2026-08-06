using Backend.DTO.Dashboard;

namespace Backend.BL.IBL
{
    /// <summary>
    /// Interface pour le service de tableau de bord
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Récupère toutes les statistiques du tableau de bord
        /// </summary>
        Task<DashboardStatsDto> GetDashboardStatsAsync();

        /// <summary>
        /// Récupère les agrégats financiers (Comptes, MVT, Cours) pour le tableau de bord bancaire.
        /// </summary>
        Task<FinanceDashboardDto> GetFinanceDashboardAsync();

        /// <summary>
        /// Récupère les KPIs principaux
        /// </summary>
        Task<KpiSummaryDto> GetKpisAsync();

        /// <summary>
        /// Récupère l'évolution des clients sur les N derniers mois
        /// </summary>
        Task<ClientEvolutionDto> GetClientEvolutionAsync(int months = 6);

        /// <summary>
        /// Récupère la répartition géographique des clients
        /// </summary>
        Task<List<GeographicDistributionDto>> GetGeographicDistributionAsync(int topN = 10);

        /// <summary>
        /// Récupère le top N des clients par revenu mensuel
        /// </summary>
        Task<List<TopRevenueClientDto>> GetTopRevenueClientsAsync(int topN = 10);

        /// <summary>
        /// Récupère les documents en attente de validation
        /// </summary>
        Task<DocumentsPendingDto> GetPendingDocumentsAsync();

        /// <summary>
        /// Récupère la performance des agents
        /// </summary>
        Task<List<AgentPerformanceDto>> GetAgentPerformanceAsync();

        /// <summary>
        /// Récupère les alertes actives
        /// </summary>
        Task<List<DashboardAlertDto>> GetAlertsAsync();

        /// <summary>
        /// Sauvegarde la configuration personnalisée du tableau de bord
        /// </summary>
        Task SaveDashboardConfigAsync(DashboardConfigDto config);

        /// <summary>
        /// Récupère la configuration personnalisée de l'utilisateur
        /// </summary>
        Task<DashboardConfigDto?> GetDashboardConfigAsync(int userId);
    }
}

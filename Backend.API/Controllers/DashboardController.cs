using Backend.BL.IBL;
using Backend.DTO.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    /// <summary>
    /// Contrôleur pour le tableau de bord analytique
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IDashboardService dashboardService,
            ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Récupère toutes les statistiques du tableau de bord
        /// </summary>
        /// <returns>Statistiques complètes</returns>
        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
        {
            try
            {
                var stats = await _dashboardService.GetDashboardStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques");
                return StatusCode(500, new { message = "Erreur serveur lors de la récupération des statistiques" });
            }
        }

        /// <summary>
        /// Récupère les KPIs principaux
        /// </summary>
        /// <returns>KPIs du tableau de bord</returns>
        [HttpGet("kpis")]
        public async Task<ActionResult<KpiSummaryDto>> GetKpis()
        {
            try
            {
                var kpis = await _dashboardService.GetKpisAsync();
                return Ok(kpis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des KPIs");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Récupère l'évolution des clients sur plusieurs mois
        /// </summary>
        /// <param name="months">Nombre de mois (par défaut: 6)</param>
        /// <returns>Données d'évolution</returns>
        [HttpGet("client-evolution")]
        public async Task<ActionResult<ClientEvolutionDto>> GetClientEvolution([FromQuery] int months = 6)
        {
            try
            {
                if (months < 1 || months > 24)
                    return BadRequest(new { message = "Le nombre de mois doit être entre 1 et 24" });

                var evolution = await _dashboardService.GetClientEvolutionAsync(months);
                return Ok(evolution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'évolution des clients");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Récupère la répartition géographique des clients
        /// </summary>
        /// <param name="topN">Nombre de villes à retourner (par défaut: 10)</param>
        /// <returns>Répartition géographique</returns>
        [HttpGet("geographic-distribution")]
        public async Task<ActionResult<List<GeographicDistributionDto>>> GetGeographicDistribution([FromQuery] int topN = 10)
        {
            try
            {
                if (topN < 1 || topN > 50)
                    return BadRequest(new { message = "Le paramètre topN doit être entre 1 et 50" });

                var distribution = await _dashboardService.GetGeographicDistributionAsync(topN);
                return Ok(distribution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la répartition géographique");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Récupère le top des clients par revenu mensuel
        /// </summary>
        /// <param name="topN">Nombre de clients à retourner (par défaut: 10)</param>
        /// <returns>Top clients par revenu</returns>
        [HttpGet("top-revenue-clients")]
        public async Task<ActionResult<List<TopRevenueClientDto>>> GetTopRevenueClients([FromQuery] int topN = 10)
        {
            try
            {
                if (topN < 1 || topN > 100)
                    return BadRequest(new { message = "Le paramètre topN doit être entre 1 et 100" });

                var topClients = await _dashboardService.GetTopRevenueClientsAsync(topN);
                return Ok(topClients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des top clients");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Récupère les documents en attente de validation
        /// </summary>
        /// <returns>Documents en attente</returns>
        [HttpGet("pending-documents")]
        public async Task<ActionResult<DocumentsPendingDto>> GetPendingDocuments()
        {
            try
            {
                var pendingDocs = await _dashboardService.GetPendingDocumentsAsync();
                return Ok(pendingDocs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des documents en attente");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Récupère la performance des agents
        /// </summary>
        /// <returns>Performance des agents</returns>
        [HttpGet("agent-performance")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<ActionResult<List<AgentPerformanceDto>>> GetAgentPerformance()
        {
            try
            {
                var performance = await _dashboardService.GetAgentPerformanceAsync();
                return Ok(performance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la performance des agents");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Récupère les alertes actives
        /// </summary>
        /// <returns>Liste des alertes</returns>
        [HttpGet("alerts")]
        public async Task<ActionResult<List<DashboardAlertDto>>> GetAlerts()
        {
            try
            {
                var alerts = await _dashboardService.GetAlertsAsync();
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des alertes");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Sauvegarde la configuration personnalisée du tableau de bord
        /// </summary>
        /// <param name="config">Configuration à sauvegarder</param>
        /// <returns>Résultat de la sauvegarde</returns>
        [HttpPost("config")]
        public async Task<ActionResult> SaveDashboardConfig([FromBody] DashboardConfigDto config)
        {
            try
            {
                await _dashboardService.SaveDashboardConfigAsync(config);
                return Ok(new { message = "Configuration sauvegardée avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la sauvegarde de la configuration");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Récupère la configuration personnalisée de l'utilisateur connecté
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <returns>Configuration du tableau de bord</returns>
        [HttpGet("config/{userId}")]
        public async Task<ActionResult<DashboardConfigDto>> GetDashboardConfig(int userId)
        {
            try
            {
                var config = await _dashboardService.GetDashboardConfigAsync(userId);
                if (config == null)
                    return NotFound(new { message = "Configuration non trouvée" });

                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la configuration");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }
    }
}

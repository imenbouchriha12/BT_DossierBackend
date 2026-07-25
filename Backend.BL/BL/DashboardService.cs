using AutoMapper;
using Backend.BL.IBL;
using Backend.DAL;
using Backend.DAL.Entities;
using Backend.DTO.Dashboard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.BL.BL
{
    /// <summary>
    /// Service de gestion du tableau de bord analytique
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<DashboardService> _logger;
        private readonly IMapper _mapper;

        public DashboardService(
            AppDbContext context,
            ICurrentUserService currentUserService,
            ILogger<DashboardService> logger,
            IMapper mapper)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            try
            {
                var stats = new DashboardStatsDto
                {
                    Kpis = await GetKpisAsync(),
                    ClientEvolution = await GetClientEvolutionAsync(6),
                    GeographicDistribution = await GetGeographicDistributionAsync(10),
                    TopRevenueClients = await GetTopRevenueClientsAsync(10),
                    DocumentsPending = await GetPendingDocumentsAsync(),
                    AgentPerformance = await GetAgentPerformanceAsync(),
                    Alerts = await GetAlertsAsync()
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques du tableau de bord");
                throw;
            }
        }

        public async Task<KpiSummaryDto> GetKpisAsync()
        {
            var now = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            var firstDayOfLastMonth = firstDayOfMonth.AddMonths(-1);

            // Requête optimisée pour récupérer tous les KPIs en une seule passe
            var clients = await _context.Clients
                .Include(c => c.Detail)
                .Where(c => !c.EstArchive)
                .ToListAsync();

            var totalActiveClients = clients.Count(c => c.Statut == StatutClient.Actif);
            var newClientsThisMonth = clients.Count(c => c.DateCreation >= firstDayOfMonth);
            var newClientsLastMonth = clients.Count(c => c.DateCreation >= firstDayOfLastMonth && c.DateCreation < firstDayOfMonth);
            
            var percentageChange = newClientsLastMonth > 0 
                ? ((double)(newClientsThisMonth - newClientsLastMonth) / newClientsLastMonth) * 100 
                : 0;

            var blockedClients = clients.Count(c => c.Statut == StatutClient.Bloqué);
            
            // Clients inactifs (pas de nouvelle création depuis 30 jours)
            var thirtyDaysAgo = now.AddDays(-30);
            var inactiveClients = clients.Count(c => c.DateCreation < thirtyDaysAgo);

            // Documents
            var documents = await _context.DocumentClients.ToListAsync();
            var totalDocuments = documents.Count;
            var pendingDocuments = documents.Count(d => d.StatutValidation == "En attente");
            var validatedThisMonth = documents.Count(d => 
                d.StatutValidation == "Validé" && 
                d.DateAjout >= firstDayOfMonth);

            // Revenus
            var clientsWithRevenue = clients.Where(c => c.Detail != null).ToList();
            var avgRevenue = clientsWithRevenue.Any() 
                ? clientsWithRevenue.Average(c => c.Detail!.RevenuMensuel) 
                : 0;
            var totalRevenue = clientsWithRevenue.Sum(c => c.Detail!.RevenuMensuel);

            // Utilisateurs actifs
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive);

            return new KpiSummaryDto
            {
                TotalActiveClients = totalActiveClients,
                NewClientsThisMonth = newClientsThisMonth,
                NewClientsPercentageChange = Math.Round(percentageChange, 2),
                BlockedClients = blockedClients,
                InactiveClients = inactiveClients,
                TotalDocuments = totalDocuments,
                PendingDocuments = pendingDocuments,
                ValidatedDocumentsThisMonth = validatedThisMonth,
                AverageMonthlyRevenue = Math.Round(avgRevenue, 2),
                TotalMonthlyRevenue = Math.Round(totalRevenue, 2),
                TotalActiveUsers = activeUsers
            };
        }

        public async Task<ClientEvolutionDto> GetClientEvolutionAsync(int months = 6)
        {
            var now = DateTime.UtcNow;
            var startDate = now.AddMonths(-months);

            var clients = await _context.Clients
                .Where(c => !c.EstArchive && c.DateCreation >= startDate)
                .ToListAsync();

            var evolution = new ClientEvolutionDto();
            
            // Générer les labels des mois
            for (int i = months - 1; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                evolution.Labels.Add(monthDate.ToString("MMM yyyy"));
            }

            // Calculer les statistiques pour chaque mois
            for (int i = months - 1; i >= 0; i--)
            {
                var monthStart = now.AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);

                var monthClients = clients.Where(c => c.DateCreation >= monthStart && c.DateCreation < monthEnd);

                evolution.NewClients.Add(monthClients.Count());
                evolution.ActiveClients.Add(monthClients.Count(c => c.Statut == StatutClient.Actif));
                evolution.BlockedClients.Add(monthClients.Count(c => c.Statut == StatutClient.Bloqué));
                evolution.SuspendedClients.Add(monthClients.Count(c => c.Statut == StatutClient.EnAttente));
            }

            return evolution;
        }

        public async Task<List<GeographicDistributionDto>> GetGeographicDistributionAsync(int topN = 10)
        {
            var clients = await _context.Clients
                .Include(c => c.Detail)
                .Where(c => !c.EstArchive && !string.IsNullOrEmpty(c.Ville))
                .ToListAsync();

            var totalClients = clients.Count;

            var distribution = clients
                .GroupBy(c => c.Ville)
                .Select(g => new GeographicDistributionDto
                {
                    City = g.Key!,
                    ClientCount = g.Count(),
                    Percentage = totalClients > 0 ? Math.Round((double)g.Count() / totalClients * 100, 2) : 0,
                    TotalRevenue = g
                        .Where(c => c.Detail != null)
                        .Sum(c => c.Detail!.RevenuMensuel)
                })
                .OrderByDescending(g => g.ClientCount)
                .Take(topN)
                .ToList();

            return distribution;
        }

        public async Task<List<TopRevenueClientDto>> GetTopRevenueClientsAsync(int topN = 10)
        {
            var topClients = await _context.Clients
                .Include(c => c.Detail)
                .Where(c => !c.EstArchive && c.Detail != null && c.Detail.RevenuMensuel > 0)
                .OrderByDescending(c => c.Detail!.RevenuMensuel)
                .Take(topN)
                .Select(c => new TopRevenueClientDto
                {
                    ClientId = c.Id,
                    CodeClient = c.CodeClient,
                    FullName = $"{c.Nom} {c.Prenom}",
                    City = c.Ville ?? "",
                    MonthlyRevenue = c.Detail!.RevenuMensuel,
                    Profession = c.Detail.Profession ?? "",
                    SectorActivite = c.Detail.SecteurActivite ?? ""
                })
                .ToListAsync();

            return topClients;
        }

        public async Task<DocumentsPendingDto> GetPendingDocumentsAsync()
        {
            var pendingDocs = await _context.DocumentClients
                .Include(d => d.Client)
                .Where(d => d.StatutValidation == "En attente")
                .ToListAsync();

            var totalPending = pendingDocs.Count;
            var now = DateTime.UtcNow;
            var urgentCount = pendingDocs.Count(d => (now - d.DateAjout).TotalDays > 7);

            var byType = pendingDocs
                .GroupBy(d => d.TypeDocument)
                .Select(g => new DocumentTypeCountDto
                {
                    DocumentType = g.Key,
                    Count = g.Count(),
                    Percentage = totalPending > 0 ? Math.Round((double)g.Count() / totalPending * 100, 2) : 0
                })
                .ToList();

            var recentPending = pendingDocs
                .OrderBy(d => d.DateAjout)
                .Take(10)
                .Select(d => new PendingDocumentItemDto
                {
                    DocumentId = d.Id,
                    ClientCode = d.Client?.CodeClient ?? "",
                    ClientName = d.Client != null ? $"{d.Client.Nom} {d.Client.Prenom}" : "",
                    DocumentType = d.TypeDocument,
                    FileName = d.NomFichier,
                    DateAdded = d.DateAjout,
                    DaysPending = (int)(now - d.DateAjout).TotalDays,
                    IsUrgent = (now - d.DateAjout).TotalDays > 7
                })
                .ToList();

            return new DocumentsPendingDto
            {
                TotalPending = totalPending,
                ByType = byType,
                UrgentCount = urgentCount,
                RecentPending = recentPending
            };
        }

        public async Task<List<AgentPerformanceDto>> GetAgentPerformanceAsync()
        {
            var now = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);

            // Récupérer tous les agents (rôle Agent)
            var agents = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.Role.Name == "Agent"))
                .ToListAsync();

            var performance = new List<AgentPerformanceDto>();

            foreach (var agent in agents)
            {
                // Clients créés par cet agent
                var clientsCreated = await _context.HistoriqueModifications
                    .Where(h => h.UtilisateurId == agent.Id && 
                                h.Action.Contains("Création") && 
                                h.DateHeure >= firstDayOfMonth)
                    .CountAsync();

                var totalClients = await _context.HistoriqueModifications
                    .Where(h => h.UtilisateurId == agent.Id && h.Action.Contains("Création"))
                    .CountAsync();

                var docsProcessed = await _context.HistoriqueModifications
                    .Where(h => h.UtilisateurId == agent.Id && 
                                h.DateHeure >= firstDayOfMonth &&
                                h.TableModifiee == "DocumentClients")
                    .CountAsync();

                var modificationsCount = await _context.HistoriqueModifications
                    .Where(h => h.UtilisateurId == agent.Id)
                    .CountAsync();

                // Score de performance simple (peut être amélioré)
                var score = CalculatePerformanceScore(clientsCreated, docsProcessed, modificationsCount);

                performance.Add(new AgentPerformanceDto
                {
                    UserId = agent.Id,
                    Username = agent.Username,
                    Matricule = agent.Matricule,
                    ClientsCreatedThisMonth = clientsCreated,
                    TotalClientsManaged = totalClients,
                    DocumentsProcessedThisMonth = docsProcessed,
                    ModificationsCount = modificationsCount,
                    PerformanceScore = score
                });
            }

            return performance.OrderByDescending(p => p.PerformanceScore).ToList();
        }

        public async Task<List<DashboardAlertDto>> GetAlertsAsync()
        {
            var alerts = new List<DashboardAlertDto>();
            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);

            // Alerte : Clients inactifs (créés il y a plus de 30 jours)
            var inactiveCount = await _context.Clients
                .CountAsync(c => !c.EstArchive && c.DateCreation < thirtyDaysAgo);

            if (inactiveCount > 0)
            {
                alerts.Add(new DashboardAlertDto
                {
                    Type = AlertType.InactiveClients,
                    Severity = inactiveCount > 50 ? AlertSeverity.Warning : AlertSeverity.Info,
                    Title = "Clients inactifs",
                    Message = $"{inactiveCount} client(s) sans activité depuis plus de 30 jours",
                    Count = inactiveCount,
                    ActionUrl = "/clients?filter=inactive"
                });
            }

            // Alerte : Documents en attente
            var pendingDocsCount = await _context.DocumentClients
                .CountAsync(d => d.StatutValidation == "En attente");

            if (pendingDocsCount > 0)
            {
                var sevenDaysAgo = now.AddDays(-7);
                var urgentDocs = await _context.DocumentClients
                    .CountAsync(d => d.StatutValidation == "En attente" && d.DateAjout < sevenDaysAgo);

                alerts.Add(new DashboardAlertDto
                {
                    Type = AlertType.PendingDocuments,
                    Severity = urgentDocs > 10 ? AlertSeverity.Error : AlertSeverity.Warning,
                    Title = "Documents en attente",
                    Message = $"{pendingDocsCount} document(s) en attente de validation ({urgentDocs} urgent(s))",
                    Count = pendingDocsCount,
                    ActionUrl = "/documents?filter=pending"
                });
            }

            // Alerte : Clients bloqués
            var blockedCount = await _context.Clients
                .CountAsync(c => !c.EstArchive && c.Statut == StatutClient.Bloqué);

            if (blockedCount > 0)
            {
                alerts.Add(new DashboardAlertDto
                {
                    Type = AlertType.BlockedClients,
                    Severity = AlertSeverity.Warning,
                    Title = "Clients bloqués",
                    Message = $"{blockedCount} client(s) actuellement bloqué(s)",
                    Count = blockedCount,
                    ActionUrl = "/clients?filter=blocked"
                });
            }

            return alerts.OrderByDescending(a => a.Severity).ToList();
        }

        public async Task SaveDashboardConfigAsync(DashboardConfigDto config)
        {
            // Note: Il faudrait créer une entité DashboardConfig dans la base
            // Pour l'instant, on peut stocker en JSON ou créer la table
            // Implementation à compléter selon les besoins
            await Task.CompletedTask;
        }

        public async Task<DashboardConfigDto?> GetDashboardConfigAsync(int userId)
        {
            // Note: Récupérer la configuration depuis la base
            // Implementation à compléter selon les besoins
            await Task.CompletedTask;
            return null;
        }

        private double CalculatePerformanceScore(int clientsCreated, int docsProcessed, int modifications)
        {
            // Score simple basé sur l'activité (peut être amélioré)
            var score = (clientsCreated * 10) + (docsProcessed * 5) + (modifications * 2);
            return Math.Min(Math.Round((double)score, 2), 100);
        }
    }
}

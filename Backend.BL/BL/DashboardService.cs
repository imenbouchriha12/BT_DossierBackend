using AutoMapper;
using Backend.BL.IBL;
using Backend.DAL;
using Backend.DAL.Entities;
using Backend.DTO.Dashboard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

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
        private readonly ICoursTempsReelService _coursTempsReelService;

        public DashboardService(
            AppDbContext context,
            ICurrentUserService currentUserService,
            ILogger<DashboardService> logger,
            IMapper mapper,
            ICoursTempsReelService coursTempsReelService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _logger = logger;
            _mapper = mapper;
            _coursTempsReelService = coursTempsReelService;
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
                    TypeDistribution = await GetTypeDistributionAsync(),
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

        public async Task<FinanceDashboardDto> GetFinanceDashboardAsync()
        {
            var comptes = await _context.Comptes.ToListAsync();
            var mouvements = await _context.Mouvements.ToListAsync();
            var cours = await _context.Cours.ToListAsync();
            var clients = await _context.Clients
                .Select(c => new { c.Id, c.CodeClient, c.Nom, c.Prenom })
                .ToListAsync();

            var clientsParId = clients.ToDictionary(c => c.Id);

            // Dernier cours valide par devise (conversion des montants en TND)
            var dernierCours = cours
                .Where(c => !string.IsNullOrWhiteSpace(c.Devise) && c.Devise.Trim() != "ZZ")
                .GroupBy(c => c.Devise.Trim())
                .Select(g => new { Devise = g.Key, Dernier = g.OrderByDescending(c => c.Journee).First() })
                .ToDictionary(g => g.Devise, g => g.Dernier);

            decimal ToTnd(string devise, decimal montant)
            {
                if (string.IsNullOrWhiteSpace(devise) || devise.Trim() == "TND")
                    return montant;
                if (dernierCours.TryGetValue(devise.Trim(), out var c))
                {
                    // Convention SISS : contre-valeur = montant × Valeur ÷ Multiplicateur
                    var multiplicateur = c.Multiplicateur > 0 ? c.Multiplicateur : 1;
                    return montant * c.Valeur / multiplicateur;
                }
                return montant;
            }

            var totalActifs = comptes.Sum(c => c.SoldeContreValeur);

            var kpis = new FinanceKpisDto
            {
                TotalActifsTND = totalActifs,
                NbComptes = comptes.Count,
                NbClientsDetenteurs = comptes.Select(c => c.IdClient).Distinct().Count(),
                NbComptesDecouverts = comptes.Count(c => c.Solde < 0),
                NbMouvements = mouvements.Count,
                VolumeCreditsTND = mouvements.Where(m => m.Sens == "C").Sum(m => ToTnd(m.DevMVT, m.Montant)),
                VolumeDebitsTND = mouvements.Where(m => m.Sens == "D").Sum(m => ToTnd(m.DevMVT, m.Montant)),
                PeriodeReference = mouvements.Any() ? mouvements.Max(m => m.DateMvt) : (DateTime?)null
            };
            kpis.TauxDecouvert = kpis.NbComptes > 0 ? Math.Round((double)kpis.NbComptesDecouverts / kpis.NbComptes * 100, 2) : 0;
            kpis.FluxNet = kpis.VolumeCreditsTND - kpis.VolumeDebitsTND;

            // Répartition des actifs par devise
            var repartition = comptes
                .GroupBy(c => c.Devise.Trim())
                .Select(g => new RepartitionDeviseDto
                {
                    Devise = g.Key,
                    NombreComptes = g.Count(),
                    SoldeContreValeur = g.Sum(c => c.SoldeContreValeur),
                    Part = totalActifs != 0 ? Math.Round((double)(g.Sum(c => c.SoldeContreValeur) / totalActifs) * 100, 2) : 0
                })
                .OrderByDescending(g => g.SoldeContreValeur)
                .ToList();

            // État des comptes
            var etats = new List<EtatCompteDto>();
            var groupeCrediteur = comptes.Where(c => c.Solde > 0).ToList();
            var groupeSolde = comptes.Where(c => c.Solde == 0).ToList();
            var groupeDecouvert = comptes.Where(c => c.Solde < 0).ToList();
            etats.Add(new EtatCompteDto { Etat = "Créditeur", Nombre = groupeCrediteur.Count, SoldeContreValeur = groupeCrediteur.Sum(c => c.SoldeContreValeur) });
            etats.Add(new EtatCompteDto { Etat = "Soldé", Nombre = groupeSolde.Count, SoldeContreValeur = groupeSolde.Sum(c => c.SoldeContreValeur) });
            etats.Add(new EtatCompteDto { Etat = "Découvert", Nombre = groupeDecouvert.Count, SoldeContreValeur = groupeDecouvert.Sum(c => c.SoldeContreValeur) });

            // Évolution mensuelle des flux
            var evolution = mouvements
                .GroupBy(m => new { m.DateMvt.Year, m.DateMvt.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new EvolutionMouvementDto
                {
                    Mois = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy", CultureInfo.GetCultureInfo("fr-FR")),
                    Credits = g.Where(m => m.Sens == "C").Sum(m => ToTnd(m.DevMVT, m.Montant)),
                    Debits = g.Where(m => m.Sens == "D").Sum(m => ToTnd(m.DevMVT, m.Montant))
                })
                .ToList();
            foreach (var e in evolution)
                e.FluxNet = e.Credits - e.Debits;

            // Top des agences
            var topAgences = comptes
                .GroupBy(c => c.Agence.Trim())
                .Select(g => new TopAgenceDto
                {
                    Agence = g.Key,
                    NombreComptes = g.Count(),
                    SoldeContreValeur = g.Sum(c => c.SoldeContreValeur),
                    NbMouvements = mouvements.Count(m => m.Agence == g.Key)
                })
                .OrderByDescending(a => a.SoldeContreValeur)
                .ToList();

            // Derniers mouvements (avec client)
            var compteParCle = comptes.ToDictionary(c => (c.NumCompte, c.Agence));
            var derniersMouvements = mouvements
                .OrderByDescending(m => m.DateMvt)
                .ThenByDescending(m => m.Id)
                .Take(10)
                .Select(m =>
                {
                    var compte = compteParCle.GetValueOrDefault((m.NumCompte, m.Agence));
                    var client = compte != null && clientsParId.TryGetValue(compte.IdClient, out var cl) ? cl : null;
                    return new MouvementRecenteDto
                    {
                        Id = m.Id,
                        NumCompte = m.NumCompte,
                        Agence = m.Agence,
                        DateMvt = m.DateMvt,
                        Montant = m.Montant,
                        DevMVT = m.DevMVT,
                        Sens = m.Sens,
                        MontantTND = Math.Round(ToTnd(m.DevMVT, m.Montant), 3),
                        CodeClient = client?.CodeClient,
                        NomClient = client != null ? $"{client.Nom} {client.Prenom}".Trim() : null
                    };
                })
                .ToList();

            // Comptes en découvert (avec client)
            var comptesDecouverts = groupeDecouvert
                .Select(c =>
                {
                    var client = clientsParId.TryGetValue(c.IdClient, out var cl) ? cl : null;
                    return new CompteDecouvertDto
                    {
                        NumCompte = c.NumCompte,
                        Agence = c.Agence,
                        Devise = c.Devise.Trim(),
                        Solde = c.Solde,
                        SoldeContreValeur = c.SoldeContreValeur,
                        CodeClient = client?.CodeClient,
                        NomClient = client != null ? $"{client.Nom} {client.Prenom}".Trim() : null
                    };
                })
                .OrderBy(c => c.Solde)
                .ToList();

            // Cours de change du jour (temps réel, repli SISS)
            var coursJour = await _coursTempsReelService.GetCoursDuJourAsync();

            return new FinanceDashboardDto
            {
                Kpis = kpis,
                RepartitionParDevise = repartition,
                EtatsComptes = etats,
                EvolutionMouvements = evolution,
                TopAgences = topAgences,
                DerniersMouvements = derniersMouvements,
                ComptesDecouverts = comptesDecouverts,
                CoursJour = coursJour
            };
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
                evolution.Labels.Add(monthDate.ToString("MMM yyyy", CultureInfo.GetCultureInfo("fr-FR")));
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
                .GroupBy(c => c.Ville!.Trim())
                .Select(g => new GeographicDistributionDto
                {
                    City = g.Key,
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

        public async Task<List<ClientTypeDistributionDto>> GetTypeDistributionAsync()
        {
            var clients = await _context.Clients
                .Where(c => !c.EstArchive)
                .Select(c => c.TypeClient)
                .ToListAsync();

            var total = clients.Count;

            var distribution = clients
                .GroupBy(t => t)
                .Select(g => new ClientTypeDistributionDto
                {
                    Type = g.Key,
                    Count = g.Count(),
                    Percentage = total > 0 ? Math.Round((double)g.Count() / total * 100, 2) : 0
                })
                .OrderByDescending(d => d.Count)
                .ToList();

            // Garantir qu'un type sans client apparaît avec un compte à 0
            foreach (var type in Backend.DTO.Client.ClientTypes.All)
            {
                if (!distribution.Any(d => d.Type == type))
                    distribution.Add(new ClientTypeDistributionDto { Type = type, Count = 0, Percentage = 0 });
            }

            return distribution.OrderByDescending(d => d.Count).ToList();
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
                    ClientId = d.Client?.Id ?? 0,
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
                // Toute l'activité de l'agent, récupérée en une seule requête
                var activites = await _context.HistoriqueModifications
                    .Where(h => h.UtilisateurId == agent.Id)
                    .ToListAsync();

                var clientsCreatedThisMonth = activites.Count(h =>
                    h.Action.Contains("Création") && h.DateHeure >= firstDayOfMonth);
                var totalClientsManaged = activites.Count(h => h.Action.Contains("Création"));

                var docsProcessedThisMonth = activites.Count(h =>
                    h.TableModifiee.Contains("Document") && h.DateHeure >= firstDayOfMonth);
                var docsProcessedTotal = activites.Count(h => h.TableModifiee.Contains("Document"));

                var modificationsCount = activites.Count;
                var lastActivity = activites.Count > 0 ? (DateTime?)activites.Max(h => h.DateHeure) : null;

                // Score : pondère les clients créés ce mois, les documents traités et l'ancienneté d'activité
                var score = CalculatePerformanceScore(clientsCreatedThisMonth, docsProcessedThisMonth, modificationsCount, lastActivity, now);

                performance.Add(new AgentPerformanceDto
                {
                    UserId = agent.Id,
                    Username = agent.Username,
                    Matricule = agent.Matricule,
                    ClientsCreatedThisMonth = clientsCreatedThisMonth,
                    TotalClientsManaged = totalClientsManaged,
                    DocumentsProcessedThisMonth = docsProcessedThisMonth,
                    DocumentsProcessedTotal = docsProcessedTotal,
                    ModificationsCount = modificationsCount,
                    LastActivity = lastActivity,
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

        private double CalculatePerformanceScore(int clientsCreated, int docsProcessed, int modifications, DateTime? lastActivity, DateTime now)
        {
            // Composante "activité récente" : 40 pts max si activité dans les 7 derniers jours
            double recencyScore = 0;
            if (lastActivity.HasValue)
            {
                var jours = (now - lastActivity.Value).TotalDays;
                recencyScore = Math.Max(0, 40 - (jours / 7) * 10);
            }

            // Composante "volume" : 60 pts max (clients × 12 + documents × 8 + modifications × 2)
            var volumeScore = Math.Min(60, (clientsCreated * 12) + (docsProcessed * 8) + (modifications * 2));

            return Math.Round(Math.Min(recencyScore + volumeScore, 100), 1);
        }
    }
}

namespace Backend.DTO.Dashboard
{
    /// <summary>
    /// DTO pour les statistiques complètes du tableau de bord
    /// </summary>
    public class DashboardStatsDto
    {
        /// KPIs généraux
        public KpiSummaryDto Kpis { get; set; } = new();

        /// Évolution des clients dans le temps
        public ClientEvolutionDto ClientEvolution { get; set; } = new();

        /// Répartition géographique
        public List<GeographicDistributionDto> GeographicDistribution { get; set; } = new();

        /// Répartition des clients par type
        public List<ClientTypeDistributionDto> TypeDistribution { get; set; } = new();

        /// Top revenus mensuels
        public List<TopRevenueClientDto> TopRevenueClients { get; set; } = new();

        /// Documents en attente de validation
        public DocumentsPendingDto DocumentsPending { get; set; } = new();

        /// Performance des agents
        public List<AgentPerformanceDto> AgentPerformance { get; set; } = new();

        /// Alertes actives
        public List<DashboardAlertDto> Alerts { get; set; } = new();
    }

    /// <summary>
    /// KPIs principaux du tableau de bord
    /// </summary>
    public class KpiSummaryDto
    {
        /// Nombre total de clients actifs
        public int TotalActiveClients { get; set; }

        /// Nombre de nouveaux clients ce mois
        public int NewClientsThisMonth { get; set; }

        /// Variation en % par rapport au mois dernier
        public double NewClientsPercentageChange { get; set; }

        /// Nombre de clients bloqués
        public int BlockedClients { get; set; }

        /// Nombre de clients inactifs (>30 jours sans activité)
        public int InactiveClients { get; set; }

        /// Nombre total de documents
        public int TotalDocuments { get; set; }

        /// Documents en attente de validation
        public int PendingDocuments { get; set; }

        /// Documents validés ce mois
        public int ValidatedDocumentsThisMonth { get; set; }

        /// Revenu mensuel moyen de tous les clients
        public decimal AverageMonthlyRevenue { get; set; }

        /// Revenu mensuel total
        public decimal TotalMonthlyRevenue { get; set; }

        /// Nombre total d'utilisateurs actifs
        public int TotalActiveUsers { get; set; }
    }

    /// <summary>
    /// Évolution des clients dans le temps
    /// </summary>
    public class ClientEvolutionDto
    {
        /// Labels des dates (ex: "Jan 2024", "Fév 2024")
        public List<string> Labels { get; set; } = new();

        /// Nombre de nouveaux clients par période
        public List<int> NewClients { get; set; } = new();

        /// Nombre de clients actifs par période
        public List<int> ActiveClients { get; set; } = new();

        /// Nombre de clients bloqués par période
        public List<int> BlockedClients { get; set; } = new();

        /// Nombre de clients suspendus par période
        public List<int> SuspendedClients { get; set; } = new();
    }

    /// <summary>
    /// Répartition géographique des clients
    /// </summary>
    public class GeographicDistributionDto
    {
        /// Nom de la ville
        public string City { get; set; } = string.Empty;

        /// Nombre de clients dans cette ville
        public int ClientCount { get; set; }

        /// Pourcentage du total
        public double Percentage { get; set; }

        /// Revenu mensuel total de la ville
        public decimal TotalRevenue { get; set; }

        /// Coordonnées GPS (optionnel pour carte)
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    /// <summary>
    /// Répartition des clients par type
    /// </summary>
    public class ClientTypeDistributionDto
    {
        /// Type de client (Particulier, Entreprise, VIP, Professionnel, Association)
        public string Type { get; set; } = string.Empty;

        /// Nombre de clients de ce type
        public int Count { get; set; }

        /// Pourcentage du total
        public double Percentage { get; set; }
    }

    /// <summary>
    /// Top clients par revenu mensuel
    /// </summary>
    public class TopRevenueClientDto
    {
        public int ClientId { get; set; }
        public string CodeClient { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public decimal MonthlyRevenue { get; set; }
        public string Profession { get; set; } = string.Empty;
        public string SectorActivite { get; set; } = string.Empty;
    }

    /// <summary>
    /// Documents en attente de validation
    /// </summary>
    public class DocumentsPendingDto
    {
        /// Nombre total en attente
        public int TotalPending { get; set; }

        /// Répartition par type de document
        public List<DocumentTypeCountDto> ByType { get; set; } = new();

        /// Documents urgents (>7 jours en attente)
        public int UrgentCount { get; set; }

        /// Liste des documents en attente
        public List<PendingDocumentItemDto> RecentPending { get; set; } = new();
    }

    public class DocumentTypeCountDto
    {
        public string DocumentType { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class PendingDocumentItemDto
    {
        public int DocumentId { get; set; }
        public string ClientCode { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime DateAdded { get; set; }
        public int DaysPending { get; set; }
        public bool IsUrgent { get; set; }
    }

    /// <summary>
    /// Performance d'un agent
    /// </summary>
    public class AgentPerformanceDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Matricule { get; set; } = string.Empty;

        /// Nombre de clients créés ce mois
        public int ClientsCreatedThisMonth { get; set; }

        /// Nombre total de clients gérés
        public int TotalClientsManaged { get; set; }

        /// Documents traités ce mois
        public int DocumentsProcessedThisMonth { get; set; }

        /// Nombre total de documents traités
        public int DocumentsProcessedTotal { get; set; }

        /// Nombre de modifications effectuées
        public int ModificationsCount { get; set; }

        /// Dernière activité de l'agent
        public DateTime? LastActivity { get; set; }

        /// Score de performance (0-100)
        public double PerformanceScore { get; set; }
    }

    /// <summary>
    /// Alerte du tableau de bord
    /// </summary>
    public class DashboardAlertDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public AlertType Type { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string ActionUrl { get; set; } = string.Empty;
    }

    public enum AlertType
    {
        InactiveClients,
        PendingDocuments,
        BlockedClients,
        AbnormalRevenue,
        SystemWarning
    }

    public enum AlertSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Configuration personnalisée du tableau de bord par utilisateur
    /// </summary>
    public class DashboardConfigDto
    {
        public int UserId { get; set; }
        public List<WidgetConfig> Widgets { get; set; } = new();
        public string Theme { get; set; } = "light";
        public bool ShowAlerts { get; set; } = true;
        public DateTime LastUpdated { get; set; }
    }

    public class WidgetConfig
    {
        public string WidgetId { get; set; } = string.Empty;
        public string WidgetType { get; set; } = string.Empty;
        public int Position { get; set; }
        public bool IsVisible { get; set; } = true;
        public string Size { get; set; } = "medium"; // small, medium, large
    }
}

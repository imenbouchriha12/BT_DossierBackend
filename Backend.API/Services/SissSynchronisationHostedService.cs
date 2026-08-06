using Backend.BL.IBL;

namespace Backend.API.Services
{
    /// Service planifié : déclenche automatiquement la synchronisation SISS
    /// chaque jour à l'heure configurée (Siss:PlanningHeure, défaut 02:30).
    ///
    /// ARCHITECTURE (Option A) :
    ///   Ce service appelle SynchronisationService.DeclencherAsync, qui lui-même
    ///   lance le package SSIS via dtexec (Siss:DtexecPath + Siss:PackagePath).
    ///   Le package SSIS est l'UNIQUE responsable du chargement des données
    ///   (Client, DetailClient, DocumentClient, Compte, Cours, MVT) dans SQL Server.
    ///   Ce service ne lit aucun CSV et ne manipule aucune entité directement.
    public class SissSynchronisationHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SissSynchronisationHostedService> _logger;

        public SissSynchronisationHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<SissSynchronisationHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service planifié de synchronisation SISS démarré.");

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    var synchronisationService = scope.ServiceProvider.GetRequiredService<ISynchronisationService>();
                    var repository = scope.ServiceProvider.GetRequiredService<Backend.DAL.IDAL.ISynchronisationRepository>();

                    if (!TimeSpan.TryParse(configuration["Siss:PlanningHeure"] ?? "02:30", out var cible))
                        cible = TimeSpan.FromHours(2).Add(TimeSpan.FromMinutes(30));

                    var maintenant = DateTime.Now.TimeOfDay;
                    var ecart = Math.Abs((maintenant - cible).TotalMinutes);
                    if (ecart > 5)
                        continue;

                    var derniere = await repository.GetLastAsync();
                    if (derniere != null && derniere.Statut == "En cours")
                        continue;
                    if (derniere != null && derniere.Declencheur == "Planifie"
                        && (DateTime.UtcNow - derniere.DateDebut).TotalHours < 6)
                        continue;

                    _logger.LogInformation("Déclenchement planifié de la synchronisation SISS (heure cible {Heure:hh\\:mm})", cible);
                    await synchronisationService.DeclencherAsync("Planifie", null);

                    // évite de re-déclencher dans la fenêtre
                    await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la synchronisation SISS planifiée");
                }
            }
        }
    }
}

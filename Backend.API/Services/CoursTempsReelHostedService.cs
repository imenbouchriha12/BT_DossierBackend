using Backend.API.Hubs;
using Backend.BL.IBL;
using Microsoft.AspNetCore.SignalR;

namespace Backend.API.Services
{
    /// <summary>
    /// Service d'arrière-plan : rafraîchit régulièrement les cours de change depuis la
    /// source externe, persiste l'historique en base et diffuse les mises à jour en
    /// temps réel via le hub « cours » à tous les clients connectés.
    /// </summary>
    public class CoursTempsReelHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CoursTempsReelHostedService> _logger;

        public CoursTempsReelHostedService(
            IServiceProvider serviceProvider,
            ILogger<CoursTempsReelHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CoursTempsReelHostedService démarré (actualisation temps réel)");

            // Petit délai initial pour laisser l'API démarrer.
            try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
            catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshAndBroadcastAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de l'actualisation des cours de change");
                }

                try
                {
                    await Task.Delay(GetInterval(), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private TimeSpan GetInterval()
        {
            using var scope = _serviceProvider.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var seconds = int.TryParse(configuration["CoursTempsReel:RefreshSeconds"], out var s) ? s : 60;
            return TimeSpan.FromSeconds(Math.Max(10, seconds));
        }

        private async Task RefreshAndBroadcastAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            var coursService = scope.ServiceProvider.GetRequiredService<ICoursTempsReelService>();
            var cours = await coursService.RefreshAsync();

            if (cours.Count == 0)
                return;

            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<CoursHub>>();
            await hubContext.Clients.All.SendAsync("CoursMaj", cours, cancellationToken);

            _logger.LogDebug("Cours diffusés : {Count} devises", cours.Count);
        }
    }
}

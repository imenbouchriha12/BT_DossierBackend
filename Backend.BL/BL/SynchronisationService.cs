using AutoMapper;
using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Notification;
using Backend.DTO.Synchronisation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Backend.BL.BL
{
    /// <summary>
    /// Service de synchronisation application ↔ SISS.
    ///
    /// ARCHITECTURE (Option A — conforme au cahier des charges) :
    ///   • Le pipeline SSIS (Visual Studio / dtexec) est l'UNIQUE point d'entrée ETL.
    ///     Il charge directement en SQL Server les tables :
    ///       - dbo.Client, dbo.DetailClient, dbo.DocumentClient
    ///       - dbo.Compte, dbo.Cours, dbo.MVT
    ///     depuis les fichiers CSV exportés par le système bancaire.
    ///
    ///   • Ce service C# NE relit PAS les CSV et NE réinsère PAS de données.
    ///     Son rôle est :
    ///       1. Déclencher l'exécution du package SSIS via dtexec (ou SQL Server Agent).
    ///       2. Tracer chaque exécution dans SynchronisationLog.
    ///       3. Notifier les admins du résultat via notifications in-app et SignalR.
    ///
    ///   • SissValidationRules reste présent comme référence documentaire des règles
    ///     métier appliquées dans le package SSIS (aucun traitement côté C#).
    /// </summary>
    public class SynchronisationService : ISynchronisationService
    {
        private readonly ISynchronisationRepository _synchronisationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IHistoriqueService _historiqueService;
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;
        private readonly ILogger<SynchronisationService> _logger;
        private readonly IConfiguration _configuration;

        public SynchronisationService(
            ISynchronisationRepository synchronisationRepository,
            IUserRepository userRepository,
            IHistoriqueService historiqueService,
            INotificationService notificationService,
            ICurrentUserService currentUser,
            IMapper mapper,
            ILogger<SynchronisationService> logger,
            IConfiguration configuration)
        {
            _synchronisationRepository = synchronisationRepository;
            _userRepository            = userRepository;
            _historiqueService         = historiqueService;
            _notificationService       = notificationService;
            _currentUser               = currentUser;
            _mapper                    = mapper;
            _logger                    = logger;
            _configuration             = configuration;
        }

        // ── Déclenchement ────────────────────────────────────────────────────────

        public async Task<SynchronisationResultDto> DeclencherAsync(string declencheur, int? utilisateurId)
        {
            _logger.LogInformation("Déclenchement de la synchronisation SISS ({Declencheur})", declencheur);

            // Empêche deux exécutions simultanées
            var enCours = await _synchronisationRepository.GetEnCoursAsync();
            if (enCours != null)
                throw new InvalidOperationException("Une synchronisation SISS est déjà en cours.");

            var log = new SynchronisationLog
            {
                DateDebut    = DateTime.UtcNow,
                Statut       = "En cours",
                Declencheur  = declencheur,
                UtilisateurId = utilisateurId
            };
            await _synchronisationRepository.AddLogAsync(log);
            await _synchronisationRepository.SaveChangesAsync();

            try
            {
                // ── Exécution du package SSIS via dtexec ─────────────────────
                var (exitCode, output, error) = await ExecuterPackageSissAsync();

                var statut = exitCode == 0 ? "Reussie" : "Echec";
                log.DateFin       = DateTime.UtcNow;
                log.DureeMs       = (long)(log.DateFin.Value - log.DateDebut).TotalMilliseconds;
                log.Statut        = statut;
                log.MessageErreur = exitCode != 0 ? TronquerMessage(error ?? output) : null;

                // NbInsert/NbUpdate/NbRejet sont remplis par SSIS directement en base ;
                // on ne les recalcule pas ici — ils restent à 0 dans le log C# sauf si
                // SSIS écrit lui-même dans SynchronisationLog (voir doc SSIS).
                await _synchronisationRepository.SaveChangesAsync();

                // Trace dans l'historique applicatif
                var nomUtilisateur = declencheur == "Manuel"
                    ? (_currentUser.UserName ?? "Système")
                    : "Planifié";

                await _historiqueService.EnregistrerAsync(
                    "Synchronisation", log.Id, "Synchronisation SISS",
                    null,
                    $"Statut: {statut} — Durée: {log.DureeMs} ms — dtexec exit code: {exitCode}",
                    utilisateurId ?? 0,
                    nomUtilisateur);

                await NotifierAdminsAsync(statut, log.DureeMs, exitCode, error);

                _logger.LogInformation(
                    "Synchronisation SISS terminée — Statut: {Statut}, Durée: {Duree} ms, Exit code: {ExitCode}",
                    statut, log.DureeMs, exitCode);

                return _mapper.Map<SynchronisationResultDto>(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Échec inattendu lors du déclenchement du package SSIS");

                log.DateFin       = DateTime.UtcNow;
                log.DureeMs       = (long)(log.DateFin.Value - log.DateDebut).TotalMilliseconds;
                log.Statut        = "Echec";
                log.MessageErreur = TronquerMessage(ex.Message);

                try { await _synchronisationRepository.SaveChangesAsync(); }
                catch (Exception e2) { _logger.LogError(e2, "Impossible de sauvegarder le log d'échec"); }

                try
                {
                    await _historiqueService.EnregistrerAsync(
                        "Synchronisation", log.Id, "Synchronisation SISS",
                        null, $"Échec : {TronquerMessage(ex.Message)}",
                        utilisateurId ?? 0,
                        declencheur == "Manuel" ? (_currentUser.UserName ?? "Système") : "Planifié");
                }
                catch (Exception e3) { _logger.LogError(e3, "Impossible d'enregistrer l'historique d'échec"); }

                try { await NotifierAdminsAsync("Echec", log.DureeMs, -1, ex.Message); }
                catch (Exception e4) { _logger.LogError(e4, "Impossible d'envoyer la notification d'échec"); }

                throw;
            }
        }

        // ── Consultation ─────────────────────────────────────────────────────────

        public async Task<SynchronisationStateDto> GetEtatAsync()
        {
            var enCours  = await _synchronisationRepository.GetEnCoursAsync();
            var derniere = await _synchronisationRepository.GetLastAsync();
            var rejets   = await _synchronisationRepository.CountRejetsNonResolusAsync();

            // Calcule la prochaine exécution planifiée (heure configurée, prochaine occurrence)
            DateTime? prochaineExecution = null;
            if (TimeSpan.TryParse(_configuration["Siss:PlanningHeure"] ?? "02:30", out var heureCible))
            {
                var maintenant = DateTime.Now;
                var cible = maintenant.Date.Add(heureCible);
                if (cible <= maintenant)
                    cible = cible.AddDays(1);
                prochaineExecution = cible;
            }

            return new SynchronisationStateDto
            {
                EnCours              = enCours != null,
                Derniere             = derniere == null ? null : _mapper.Map<SynchronisationResultDto>(derniere),
                TotalRejetsNonResolus = rejets,
                ProchaineExecution   = prochaineExecution
            };
        }

        public async Task<List<SynchronisationResultDto>> GetHistoriqueAsync(int page, int pageSize)
        {
            var items = await _synchronisationRepository.GetHistoryAsync(
                Math.Max(1, page),
                Math.Clamp(pageSize, 1, 100));

            return _mapper.Map<List<SynchronisationResultDto>>(items);
        }

        public async Task<List<SynchronisationRejetDto>> GetRejetsAsync(int idSynchronisation)
        {
            var rejets = await _synchronisationRepository.GetRejetsAsync(idSynchronisation);
            return _mapper.Map<List<SynchronisationRejetDto>>(rejets);
        }

        // ── Exécution dtexec ─────────────────────────────────────────────────────

        /// <summary>
        /// Lance le package SSIS via dtexec en ligne de commande et attend sa fin.
        /// Configuration (appsettings) :
        ///   Siss:DtexecPath   — chemin complet de dtexec.exe (ou "dtexec" si dans PATH)
        ///   Siss:PackagePath  — chemin du fichier .dtsx ou connexion IS Catalog
        ///   Siss:TimeoutSeconds — timeout global en secondes (défaut : 1800 = 30 min)
        ///
        /// Retourne (exitCode, stdout, stderr).
        /// Exit code 0 = succès SSIS, autre valeur = échec (voir doc dtexec).
        /// </summary>
        private async Task<(int ExitCode, string Output, string Error)> ExecuterPackageSissAsync()
        {
            var dtexecPath     = _configuration["Siss:DtexecPath"]    ?? "dtexec";
            var packagePath    = _configuration["Siss:PackagePath"]    ?? string.Empty;
            var timeoutSeconds = int.TryParse(_configuration["Siss:TimeoutSeconds"], out var t) ? t : 1800;

            if (string.IsNullOrWhiteSpace(packagePath))
                throw new InvalidOperationException(
                    "Le chemin du package SSIS n'est pas configuré (Siss:PackagePath dans appsettings).");

            // Arguments dtexec : /F pour un fichier .dtsx local
            // Si le package est dans le catalogue SSISDB : remplacer par /ISServer + /Server
            var arguments = $"/F \"{packagePath}\" /Rep E";

            _logger.LogInformation("Lancement dtexec : {DtexecPath} {Arguments}", dtexecPath, arguments);

            var psi = new ProcessStartInfo
            {
                FileName               = dtexecPath,
                Arguments              = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };

            using var process = new Process { StartInfo = psi };
            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder  = new System.Text.StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                    _logger.LogDebug("[SSIS stdout] {Line}", e.Data);
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                    _logger.LogWarning("[SSIS stderr] {Line}", e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException(
                    $"Le package SSIS n'a pas terminé dans le délai imparti ({timeoutSeconds}s).");
            }

            return (process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
        }

        // ── Notifications ────────────────────────────────────────────────────────

        private async Task NotifierAdminsAsync(string statut, long dureeMs, int exitCode, string? messageErreur)
        {
            try
            {
                var tous   = await _userRepository.GetAllWithRolesAsync();
                var admins = tous.Where(u =>
                    u.UserRoles.Any(ur => ur.Role.Name == "Admin" || ur.Role.Name == "Superviseur"));

                var type    = statut == "Reussie" ? "Succès" : "Erreur";
                var dureeS  = dureeMs / 1000.0;
                var message = statut == "Reussie"
                    ? $"Synchronisation SISS réussie en {dureeS:F1}s. Les données ont été chargées par SSIS."
                    : $"Synchronisation SSIS échouée (exit code {exitCode}). {TronquerMessage(messageErreur)}";

                foreach (var admin in admins)
                {
                    await _notificationService.CreateAsync(new CreateNotificationDto
                    {
                        UserId  = admin.Id,
                        Titre   = "Synchronisation SISS terminée",
                        Message = message,
                        Type    = type,
                        Lien    = "/historique-modifications"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Échec de la notification de fin de synchronisation");
            }
        }

        private static string TronquerMessage(string? message, int maxLength = 500)
        {
            if (string.IsNullOrWhiteSpace(message)) return string.Empty;
            return message.Length <= maxLength ? message : message[..maxLength] + "…";
        }
    }
}

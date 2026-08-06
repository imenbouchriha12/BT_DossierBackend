using AutoMapper;
using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Client;
using Backend.DTO.Notification;
using Backend.DTO.Workflow;
using Microsoft.Extensions.Logging;

namespace Backend.BL.BL
{
    /// <summary>
    /// Gestion du workflow de validation multi-niveaux des documents.
    /// Chaîne : Agent → Superviseur → Admin.
    /// Un document est « Validé » uniquement lorsque toutes ses étapes sont approuvées.
    /// </summary>
    public class WorkflowService : IWorkflowService
    {
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;
        private readonly IPortalNotifier _portalNotifier;
        private readonly IHistoriqueService _historiqueService;
        private readonly ICurrentUserService _currentUser;
        private readonly ClientStatusManager _statusManager;
        private readonly IMapper _mapper;
        private readonly ILogger<WorkflowService> _logger;
        private readonly IActivityService _activityService;

        public WorkflowService(
            IWorkflowRepository workflowRepository,
            IUserRepository userRepository,
            INotificationService notificationService,
            IPortalNotifier portalNotifier,
            IHistoriqueService historiqueService,
            ICurrentUserService currentUser,
            ClientStatusManager statusManager,
            IMapper mapper,
            ILogger<WorkflowService> logger,
            IActivityService activityService)
        {
            _workflowRepository = workflowRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
            _portalNotifier = portalNotifier;
            _historiqueService = historiqueService;
            _currentUser = currentUser;
            _statusManager = statusManager;
            _mapper = mapper;
            _logger = logger;
            _activityService = activityService;
        }

        public async Task<WorkflowValidationDto> CreateForDocumentAsync(
            DocumentClient document, int? createdByUserId, string? createdByName)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            // Rang maximal du créateur (0 = portail client / aucun rôle)
            var creatorRank = 0;
            if (createdByUserId.HasValue)
            {
                var creator = await _userRepository.GetByIdWithRolesAsync(createdByUserId.Value);
                creatorRank = creator?.UserRoles
                    .Select(ur => WorkflowRoles.Rang(ur.Role.Name))
                    .DefaultIfEmpty(0)
                    .Max() ?? 0;
            }

            var steps = new List<EtapeValidation>();
            foreach (var role in WorkflowRoles.Chaine)
            {
                var rank = WorkflowRoles.Rang(role);
                var step = new EtapeValidation
                {
                    Ordre = rank,
                    RoleRequis = role,
                    Libelle = $"Validation {role}",
                    Statut = "EnAttente"
                };

                // Étapes inférieures au créateur : non applicables (sautées)
                if (rank < creatorRank)
                {
                    step.Statut = "NonApplicable";
                }
                // Étape du niveau du créateur : validée automatiquement
                else if (rank == creatorRank && creatorRank > 0)
                {
                    step.Statut = "Valide";
                    step.UserId = createdByUserId;
                    step.NomUser = createdByName;
                    step.DecisionDate = DateTime.UtcNow;
                    step.Commentaire = "Validation automatique (créateur du document)";
                }

                steps.Add(step);
            }

            var pending = steps.FirstOrDefault(s => s.Statut == "EnAttente");

            var workflow = new WorkflowValidation
            {
                IdDocument = document.Id,
                CreatedByUserId = createdByUserId,
                DateCreation = DateTime.UtcNow,
                DateMaj = DateTime.UtcNow,
                Etapes = steps
            };

            if (pending == null)
            {
                workflow.StatutGlobal = "Valide";
                workflow.EtapeCourante = null;
                document.StatutValidation = "Validé";
            }
            else
            {
                workflow.StatutGlobal = "EnCours";
                workflow.EtapeCourante = pending.RoleRequis;
                document.StatutValidation = "En attente";
            }

            await _workflowRepository.AddAsync(workflow);
            await _workflowRepository.SaveChangesAsync();

            _logger.LogInformation("Workflow créé pour le document {IdDocument} ({Statut})",
                document.Id, workflow.StatutGlobal);

            // Notifie les validateurs du premier rôle en attente
            if (pending != null && workflow.EtapeCourante != null)
            {
                await NotifyRoleAsync(
                    workflow.EtapeCourante,
                    "Document à valider",
                    $"Le document « {document.TypeDocument} » ({document.NomFichier}) attend la validation {workflow.EtapeCourante}.",
                    $"/detail-client/{document.IdClient}");
            }

            return _mapper.Map<WorkflowValidationDto>(workflow);
        }

        public async Task<WorkflowValidationDto?> GetByIdAsync(int id)
        {
            var workflow = await _workflowRepository.GetByIdAsync(id);
            return workflow == null ? null : _mapper.Map<WorkflowValidationDto>(workflow);
        }

        public async Task<WorkflowValidationDto?> GetByDocumentIdAsync(int documentId)
        {
            var workflow = await _workflowRepository.GetLatestByDocumentIdAsync(documentId);
            return workflow == null ? null : _mapper.Map<WorkflowValidationDto>(workflow);
        }

        public async Task<List<WorkflowValidationDto>> GetByDocumentIdsAsync(IEnumerable<int> documentIds)
        {
            var workflows = await _workflowRepository.GetLatestByDocumentIdsAsync(documentIds);
            return _mapper.Map<List<WorkflowValidationDto>>(workflows);
        }

        public async Task<List<WorkflowValidationDto>> GetHistoryByDocumentIdAsync(int documentId)
        {
            var history = await _workflowRepository.GetHistoryByDocumentIdAsync(documentId);
            return _mapper.Map<List<WorkflowValidationDto>>(history);
        }

        public async Task<PagedResult<WorkflowValidationDto>> GetEnAttenteAsync(int page, int pageSize)
        {
            var roles = await GetCurrentUserRolesAsync();
            var (items, total) = await _workflowRepository.GetEnAttenteByRolesAsync(roles, page, pageSize);
            return new PagedResult<WorkflowValidationDto>
            {
                Items = _mapper.Map<List<WorkflowValidationDto>>(items),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<int> GetEnAttenteCountAsync()
        {
            var roles = await GetCurrentUserRolesAsync();
            return await _workflowRepository.CountEnAttenteByRolesAsync(roles);
        }

        public async Task<WorkflowStatsDto> GetStatsAsync()
        {
            var roles = await GetCurrentUserRolesAsync();
            var parRole = await _workflowRepository.CountEnAttenteParRoleAsync();
            return new WorkflowStatsDto
            {
                TotalEnAttente = await _workflowRepository.CountEnAttenteByRolesAsync(roles),
                ParRole = parRole
            };
        }

        public async Task<WorkflowValidationDto> ValiderAsync(int workflowId, string? commentaire)
        {
            var workflow = await GetWorkflowOrThrowAsync(workflowId);
            if (workflow.StatutGlobal != "EnCours")
                throw new InvalidOperationException($"Le workflow est déjà « {workflow.StatutGlobal} ».");

            var document = workflow.Document;
            var stepCourante = workflow.Etapes.FirstOrDefault(s => s.Statut == "EnAttente")
                ?? throw new InvalidOperationException("Aucune étape en attente de validation.");

            await VerifierAutorisationStepAsync(stepCourante.RoleRequis);

            stepCourante.Statut = "Valide";
            stepCourante.UserId = _currentUser.UserId;
            stepCourante.NomUser = _currentUser.UserName;
            stepCourante.DecisionDate = DateTime.UtcNow;
            stepCourante.Commentaire = commentaire;

            var prochaineStep = workflow.Etapes.FirstOrDefault(s => s.Statut == "EnAttente");
            if (prochaineStep == null)
            {
                workflow.StatutGlobal = "Valide";
                workflow.EtapeCourante = null;
                document.StatutValidation = "Validé";

                await _historiqueService.EnregistrerAsync(
                    "WorkflowValidation", workflowId, "Validation",
                    $"Étape {stepCourante.Libelle}",
                    $"Workflow validé — document « {document.TypeDocument} »",
                    _currentUser.UserId, _currentUser.UserName);

                await NotifierUploaderAsync(workflow, document, "Document validé",
                    $"Le document « {document.TypeDocument} » a été entièrement validé.", "Succès");

                await _activityService.EnregistrerEvenementAsync(
                    document.IdClient, EvenementTypes.Workflow,
                    $"Document « {document.TypeDocument} » validé par {_currentUser.UserName}.",
                    donneesJson: System.Text.Json.JsonSerializer.Serialize(new
                    {
                        IdDocument = document.Id,
                        Statut = "Valide"
                    }),
                    idLien: workflow.Id, typeLien: nameof(WorkflowValidation));

                _logger.LogInformation("Workflow {Id} validé — document {DocId}", workflow.Id, document.Id);
            }
            else
            {
                workflow.StatutGlobal = "EnCours";
                workflow.EtapeCourante = prochaineStep.RoleRequis;

                await _historiqueService.EnregistrerAsync(
                    "WorkflowValidation", workflowId, "Validation",
                    $"Étape {stepCourante.Libelle}",
                    $"Validée par {_currentUser.UserName} — prochaine étape : {prochaineStep.RoleRequis}",
                    _currentUser.UserId, _currentUser.UserName);

                await NotifyRoleAsync(
                    prochaineStep.RoleRequis,
                    "Document à valider",
                    $"Le document « {document.TypeDocument} » ({document.NomFichier}) attend la validation {prochaineStep.RoleRequis}.",
                    $"/detail-client/{document.IdClient}");

                _logger.LogInformation("Workflow {Id} : étape {Etape} validée — prochaine : {Next}",
                    workflow.Id, stepCourante.Libelle, prochaineStep.RoleRequis);
            }

            workflow.DateMaj = DateTime.UtcNow;
            await _workflowRepository.SaveChangesAsync();

            await RecalculerStatutClientAsync(document.IdClient);

            return _mapper.Map<WorkflowValidationDto>(workflow);
        }

        public async Task<WorkflowValidationDto> RejeterAsync(int workflowId, string commentaire)
        {
            if (string.IsNullOrWhiteSpace(commentaire))
                throw new ArgumentException("Le motif de rejet est obligatoire.");

            var workflow = await GetWorkflowOrThrowAsync(workflowId);
            if (workflow.StatutGlobal != "EnCours")
                throw new InvalidOperationException($"Le workflow est déjà « {workflow.StatutGlobal} ».");

            var document = workflow.Document;
            var stepCourante = workflow.Etapes.FirstOrDefault(s => s.Statut == "EnAttente")
                ?? throw new InvalidOperationException("Aucune étape en attente de validation.");

            await VerifierAutorisationStepAsync(stepCourante.RoleRequis);

            stepCourante.Statut = "Rejete";
            stepCourante.UserId = _currentUser.UserId;
            stepCourante.NomUser = _currentUser.UserName;
            stepCourante.DecisionDate = DateTime.UtcNow;
            stepCourante.Commentaire = commentaire;

            // Les étapes restantes sont bloquées
            foreach (var step in workflow.Etapes.Where(s => s.Statut == "EnAttente"))
                step.Statut = "NonApplicable";

            workflow.StatutGlobal = "Rejete";
            workflow.EtapeCourante = null;
            workflow.MotifRejet = commentaire;
            workflow.DateMaj = DateTime.UtcNow;

            document.StatutValidation = "Rejeté";

            await _workflowRepository.SaveChangesAsync();

            await _historiqueService.EnregistrerAsync(
                "WorkflowValidation", workflowId, "Rejet",
                $"Étape {stepCourante.Libelle}",
                $"Rejeté par {_currentUser.UserName} — motif : {commentaire}",
                _currentUser.UserId, _currentUser.UserName);

            await NotifierUploaderAsync(workflow, document, "Document rejeté",
                $"Le document « {document.TypeDocument} » a été rejeté. Motif : {commentaire}", "Erreur");

            await _activityService.EnregistrerEvenementAsync(
                document.IdClient, EvenementTypes.Workflow,
                $"Document « {document.TypeDocument} » rejeté par {_currentUser.UserName}. Motif : {commentaire}",
                donneesJson: System.Text.Json.JsonSerializer.Serialize(new
                {
                    IdDocument = document.Id,
                    Statut = "Rejete",
                    Motif = commentaire
                }),
                idLien: workflow.Id, typeLien: nameof(WorkflowValidation));

            _logger.LogWarning("Workflow {Id} rejeté — document {DocId} ({Motif})",
                workflow.Id, document.Id, commentaire);

            await RecalculerStatutClientAsync(document.IdClient);

            return _mapper.Map<WorkflowValidationDto>(workflow);
        }

        public async Task<WorkflowValidationDto> RelancerAsync(int workflowId, string? commentaire)
        {
            var oldWorkflow = await GetWorkflowOrThrowAsync(workflowId);
            if (oldWorkflow.StatutGlobal != "Rejete")
                throw new InvalidOperationException("Seul un workflow rejeté peut être relancé.");

            var document = oldWorkflow.Document;
            _logger.LogInformation("Relance du workflow du document {DocId} par {User}",
                document.Id, _currentUser.UserName);

            var nouveau = await CreateForDocumentAsync(
                document, _currentUser.UserId, _currentUser.UserName);

            await _historiqueService.EnregistrerAsync(
                "WorkflowValidation", nouveau.Id, "Relance",
                $"Ancien workflow {oldWorkflow.Id} (rejeté)",
                commentaire ?? "Document relancé pour validation",
                _currentUser.UserId, _currentUser.UserName);

            await _activityService.EnregistrerEvenementAsync(
                document.IdClient, EvenementTypes.Workflow,
                $"Document « {document.TypeDocument} » relancé pour validation par {_currentUser.UserName}.",
                idLien: nouveau.Id, typeLien: nameof(WorkflowValidation));

            return nouveau;
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private async Task<WorkflowValidation> GetWorkflowOrThrowAsync(int id)
        {
            return await _workflowRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Workflow introuvable.");
        }

        private async Task<List<string>> GetCurrentUserRolesAsync()
        {
            var user = await _userRepository.GetByIdWithRolesAsync(_currentUser.UserId);
            return user?.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList() ?? new List<string>();
        }

        private async Task<int> GetCurrentUserMaxRankAsync()
        {
            var roles = await GetCurrentUserRolesAsync();
            return roles.Select(WorkflowRoles.Rang).DefaultIfEmpty(0).Max();
        }

        /// Un utilisateur peut valider une étape si son rang maximal est >= rang requis
        private async Task VerifierAutorisationStepAsync(string roleRequis)
        {
            var userRank = await GetCurrentUserMaxRankAsync();
            var stepRank = WorkflowRoles.Rang(roleRequis);
            if (userRank < stepRank)
                throw new UnauthorizedAccessException(
                    $"Cette étape requiert le rôle « {roleRequis} » — vos droits sont insuffisants.");
        }

        private async Task NotifyRoleAsync(string role, string titre, string message, string lien)
        {
            try
            {
                var allUsers = await _userRepository.GetAllWithRolesAsync();
                var targetUsers = allUsers.Where(u =>
                    u.IsActive && u.UserRoles.Any(ur => ur.Role.Name == role));

                foreach (var user in targetUsers)
                {
                    await _notificationService.CreateAsync(new CreateNotificationDto
                    {
                        UserId = user.Id,
                        Titre = titre,
                        Message = message,
                        Type = "Info",
                        Lien = lien
                    });
                    await _portalNotifier.NotifyUserAsync(user.Id, titre, message, "workflow", lien);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erreur lors de la notification du rôle {Role}", role);
            }
        }

        private async Task NotifierUploaderAsync(
            WorkflowValidation workflow, DocumentClient document, string titre, string message, string type)
        {
            var userId = document.UploadedByUserId ?? workflow.CreatedByUserId;
            if (userId == null)
                return;

            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                UserId = userId.Value,
                Titre = titre,
                Message = message,
                Type = type,
                Lien = $"/detail-client/{document.IdClient}"
            });
            await _portalNotifier.NotifyUserAsync(userId.Value, titre, message, "workflow", $"/detail-client/{document.IdClient}");
        }

        private async Task RecalculerStatutClientAsync(int idClient)
        {
            try
            {
                await _statusManager.RecalculerStatutClientAsync(idClient);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erreur lors du recalcul du statut client {ClientId}", idClient);
            }
        }
    }
}

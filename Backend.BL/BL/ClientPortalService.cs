using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Notification;
using Backend.DTO.Portal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backend.BL.BL
{
    public class ClientPortalService : IClientPortalService
    {
        private readonly IClientPortalRepository     _portalRepo;
        private readonly IDocumentTemplateRepository _templateRepo;
        private readonly IDocumentRepository         _documentRepo;
        private readonly IClientRepository           _clientRepo;
        private readonly IUserRepository             _userRepo;
        private readonly INotificationService        _notificationService;
        private readonly IEmailService               _emailService;
        private readonly IPortalNotifier             _portalNotifier;
        private readonly IConfiguration              _configuration;
        private readonly ILogger<ClientPortalService> _logger;

        public ClientPortalService(
            IClientPortalRepository     portalRepo,
            IDocumentTemplateRepository templateRepo,
            IDocumentRepository         documentRepo,
            IClientRepository           clientRepo,
            IUserRepository             userRepo,
            INotificationService        notificationService,
            IEmailService               emailService,
            IPortalNotifier             portalNotifier,
            IConfiguration              configuration,
            ILogger<ClientPortalService> logger)
        {
            _portalRepo          = portalRepo;
            _templateRepo        = templateRepo;
            _documentRepo        = documentRepo;
            _clientRepo          = clientRepo;
            _userRepo            = userRepo;
            _notificationService = notificationService;
            _emailService        = emailService;
            _portalNotifier      = portalNotifier;
            _configuration       = configuration;
            _logger              = logger;
        }

        // ── Admin operations ──────────────────────────────────────────────────

        public async Task<GeneratePortalLinkResponseDto> GenerateAndSendPortalLinkAsync(int idClient, int dureeJours = 7)
        {
            var client = await _clientRepo.GetByIdAsync(idClient)
                ?? throw new KeyNotFoundException("Client introuvable.");

            // Révoque tous les tokens précédents
            await _portalRepo.RevokeAllByClientIdAsync(idClient);

            var tokenValue = GenerateSecureToken();
            var expiration = DateTime.UtcNow.AddDays(dureeJours);

            var portalToken = new ClientPortalToken
            {
                IdClient       = idClient,
                Token          = tokenValue,
                DateCreation   = DateTime.UtcNow,
                DateExpiration = expiration,
                EstRevoque     = false,
                NbAcces        = 0
            };

            await _portalRepo.AddAsync(portalToken);
            await _portalRepo.SaveChangesAsync();

            var portalBaseUrl = _configuration["PortalBaseUrl"] ?? "http://localhost:4200";
            var portalLink    = $"{portalBaseUrl}/portal/{tokenValue}";

            // Email au client
            var emailBody = BuildPortalInviteEmailBody(
                $"{client.Prenom} {client.Nom}", portalLink, expiration, client.TypeClient);

            var emailEnvoye = false;
            try
            {
                await _emailService.SendDirectEmailAsync(
                    client.Email,
                    $"{client.Prenom} {client.Nom}",
                    "Lumina Finance – Déposez vos documents en ligne",
                    emailBody);
                emailEnvoye = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossible d'envoyer l'email portail au client {IdClient}", idClient);
            }

            _logger.LogInformation("Lien portail généré pour {CodeClient}, expire le {Expiration}",
                client.CodeClient, expiration);

            return new GeneratePortalLinkResponseDto
            {
                PortalLink     = portalLink,
                DateExpiration = expiration,
                EmailEnvoye    = emailEnvoye,
                Message        = emailEnvoye
                    ? $"Lien envoyé par email à {client.Email}."
                    : "Lien généré. L'envoi d'email a échoué — copiez le lien manuellement."
            };
        }

        public async Task RevokePortalAccessAsync(int idClient)
        {
            _ = await _clientRepo.GetByIdAsync(idClient)
                ?? throw new KeyNotFoundException("Client introuvable.");

            await _portalRepo.RevokeAllByClientIdAsync(idClient);
            await _portalRepo.SaveChangesAsync();
            _logger.LogInformation("Accès portail révoqué pour le client {IdClient}", idClient);
        }

        public async Task<ClientPortalTokenDto?> GetActiveTokenInfoAsync(int idClient)
        {
            var client = await _clientRepo.GetByIdAsync(idClient);
            var token  = await _portalRepo.GetActiveTokenByClientIdAsync(idClient);
            if (token == null) return null;

            return new ClientPortalTokenDto
            {
                Id             = token.Id,
                IdClient       = token.IdClient,
                CodeClient     = client?.CodeClient ?? string.Empty,
                NomComplet     = client != null ? $"{client.Prenom} {client.Nom}" : string.Empty,
                DateCreation   = token.DateCreation,
                DateExpiration = token.DateExpiration,
                EstRevoque     = token.EstRevoque,
                NbAcces        = token.NbAcces,
                DernierAcces   = token.DernierAcces
            };
        }

        // ── Portal (unauthenticated) operations ───────────────────────────────

        public async Task<ValidatePortalTokenResultDto> ValidateTokenAsync(string token, string ip)
        {
            var portalToken = await _portalRepo.GetByTokenValueAsync(token);

            if (portalToken == null)
                return new ValidatePortalTokenResultDto { IsValid = false, RaisonInvalidite = "Token introuvable." };
            if (portalToken.EstRevoque)
                return new ValidatePortalTokenResultDto { IsValid = false, RaisonInvalidite = "Ce lien a été révoqué." };
            if (portalToken.DateExpiration < DateTime.UtcNow)
                return new ValidatePortalTokenResultDto { IsValid = false, RaisonInvalidite = "Ce lien a expiré." };

            // Enregistre l'accès
            portalToken.NbAcces++;
            portalToken.DernierAcces = DateTime.UtcNow;
            portalToken.DerniereIp   = ip;
            _portalRepo.Update(portalToken);
            await _portalRepo.SaveChangesAsync();

            return new ValidatePortalTokenResultDto { IsValid = true, IdClient = portalToken.IdClient };
        }

        public async Task<ClientPortalViewDto> GetPortalViewAsync(string token, string ip)
        {
            var validation = await ValidateTokenAsync(token, ip);
            if (!validation.IsValid)
                throw new UnauthorizedAccessException(validation.RaisonInvalidite);

            var idClient = validation.IdClient!.Value;
            var client   = await _clientRepo.GetByIdAsync(idClient)
                ?? throw new KeyNotFoundException("Client introuvable.");

            var templates    = await _templateRepo.GetByTypeClientAsync(client.TypeClient);
            var uploadedDocs = await _documentRepo.GetByClientIdAsync(idClient);

            var documentStatuses = templates.Select(t =>
            {
                var matched = uploadedDocs
                    .Where(d => string.Equals(d.TypeDocument, t.NomDocument, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(d => d.DateAjout)
                    .FirstOrDefault();

                return new RequiredDocumentStatusDto
                {
                    TemplateId      = t.Id,
                    NomDocument     = t.NomDocument,
                    Description     = t.Description,
                    EstObligatoire  = t.EstObligatoire,
                    Ordre           = t.Ordre,
                    FormatsAcceptes = t.FormatsAcceptes,
                    TailleMaxMo     = t.TailleMaxMo,
                    Statut          = matched?.StatutValidation,
                    DocumentId      = matched?.Id,
                    DateUpload      = matched?.DateAjout
                };
            }).OrderBy(d => d.Ordre).ToList();

            var obligatoires        = documentStatuses.Where(d => d.EstObligatoire).ToList();
            var obligatoiresFournis = obligatoires.Count(d => d.Statut != null);
            var totalDocs           = documentStatuses.Count;
            var docsFournis         = documentStatuses.Count(d => d.Statut != null);
            var taux = totalDocs == 0 ? 0 : (int)Math.Round(docsFournis * 100.0 / totalDocs);

            return new ClientPortalViewDto
            {
                CodeClient          = client.CodeClient,
                NomComplet          = $"{client.Prenom} {client.Nom}",
                TypeClient          = client.TypeClient,
                Email               = client.Email,
                TauxCompletion      = taux,
                ObligatoiresFournis = obligatoiresFournis,
                ObligatoiresTotal   = obligatoires.Count,
                Documents           = documentStatuses
            };
        }

        public async Task<PortalUploadResultDto> UploadDocumentAsync(
            string token, string typeDocument, string fileName, byte[] fileBytes, string ip)
        {
            var validation = await ValidateTokenAsync(token, ip);
            if (!validation.IsValid)
                throw new UnauthorizedAccessException(validation.RaisonInvalidite);

            var idClient = validation.IdClient!.Value;
            var client   = await _clientRepo.GetByIdAsync(idClient)
                ?? throw new KeyNotFoundException("Client introuvable.");

            // Vérifie que ce typeDocument est bien dans les templates du client
            var templates = await _templateRepo.GetByTypeClientAsync(client.TypeClient);
            var template  = templates.FirstOrDefault(t =>
                string.Equals(t.NomDocument, typeDocument, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException(
                    $"Le document '{typeDocument}' n'est pas requis pour le profil '{client.TypeClient}'.");

            // Validation extension
            var allowedExts = (template.FormatsAcceptes ?? "pdf,jpg,jpeg,png")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => "." + e.Trim())
                .ToArray();

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (!allowedExts.Contains(ext))
                throw new InvalidOperationException(
                    $"Format '{ext}' non autorisé. Formats acceptés : {template.FormatsAcceptes}");

            // Validation taille
            if (template.TailleMaxMo.HasValue)
            {
                var tailleMo = fileBytes.Length / (1024.0 * 1024.0);
                if (tailleMo > template.TailleMaxMo.Value)
                    throw new InvalidOperationException(
                        $"Fichier trop volumineux. Maximum autorisé : {template.TailleMaxMo} Mo.");
            }

            // Sauvegarde physique
            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Documents", client.CodeClient);
            Directory.CreateDirectory(uploadDir);

            var savedName = $"{typeDocument}_{client.Nom}_{client.Prenom}_Portal{ext}"
                .Replace(" ", "_").Replace("/", "_");
            var filePath = Path.Combine(uploadDir, savedName);
            await File.WriteAllBytesAsync(filePath, fileBytes);

            var relativePath = $"/documents/{client.CodeClient}/{savedName}";

            var document = new DocumentClient
            {
                IdClient         = idClient,
                TypeDocument     = typeDocument,
                NomFichier       = savedName,
                CheminFichier    = relativePath,
                DateAjout        = DateTime.UtcNow,
                StatutValidation = "En attente",
                UploadedByUserId = null   // uploadé par le client lui-même
            };

            await _documentRepo.AddAsync(document);
            await _documentRepo.SaveChangesAsync();

            _logger.LogInformation("Document '{TypeDocument}' uploadé via portail pour {CodeClient}",
                typeDocument, client.CodeClient);

            // Notifications in-app + SignalR (temps réel)
            await NotifyAdminsAsync(client, typeDocument);

            return new PortalUploadResultDto
            {
                DocumentId       = document.Id,
                NomDocument      = typeDocument,
                TypeDocument     = typeDocument,
                StatutValidation = "En attente",
                DateUpload       = document.DateAjout,
                Message          = $"Document '{typeDocument}' soumis avec succès. Il sera validé par notre équipe sous 24-48h."
            };
        }

        // ── Dashboard ─────────────────────────────────────────────────────────

        public async Task<DocumentCompletionSummaryDto> GetCompletionSummaryAsync(
            string? typeClient = null, string? ville = null)
        {
            var allClients = await _clientRepo.GetAllAsync();
            var clients    = allClients
                .Where(c => !c.EstArchive)
                .Where(c => typeClient == null || c.TypeClient == typeClient)
                .Where(c => ville      == null || c.Ville      == ville)
                .ToList();

            var allTemplates = await _templateRepo.GetAllAsync();
            var allDocuments = await _documentRepo.GetAllAsync();
            var allTokens    = await _portalRepo.GetByAllClientsAsync();

            var completions = new List<ClientDocumentCompletionDto>();

            foreach (var c in clients)
            {
                var tpls = allTemplates
                    .Where(t => t.TypeClient == c.TypeClient && t.IsActive)
                    .ToList();

                var docs = allDocuments.Where(d => d.IdClient == c.Id).ToList();

                var totalTpls           = tpls.Count;
                var obligatoiresTotal   = tpls.Count(t => t.EstObligatoire);

                var fournisTemplates    = tpls.Where(t =>
                    docs.Any(d => string.Equals(d.TypeDocument, t.NomDocument,
                        StringComparison.OrdinalIgnoreCase))).ToList();

                var obligatoiresFournis = fournisTemplates.Count(t => t.EstObligatoire);
                var docsFournis         = fournisTemplates.Count;
                var taux = totalTpls == 0 ? 0
                    : (int)Math.Round(docsFournis * 100.0 / totalTpls);

                var latestToken = allTokens
                    .Where(t => t.IdClient == c.Id)
                    .OrderByDescending(t => t.DateCreation)
                    .FirstOrDefault();

                completions.Add(new ClientDocumentCompletionDto
                {
                    IdClient                     = c.Id,
                    CodeClient                   = c.CodeClient,
                    NomComplet                   = $"{c.Prenom} {c.Nom}",
                    TypeClient                   = c.TypeClient,
                    Email                        = c.Email,
                    Ville                        = c.Ville,
                    TauxCompletion               = taux,
                    DocumentsFournis             = docsFournis,
                    DocumentsTotal               = totalTpls,
                    DocumentsObligatoiresFournis = obligatoiresFournis,
                    DocumentsObligatoiresTotal   = obligatoiresTotal,
                    DocumentsEnAttente           = docs.Count(d => d.StatutValidation == "En attente"),
                    DocumentsValides             = docs.Count(d => d.StatutValidation == "Validé"),
                    DocumentsRejetes             = docs.Count(d => d.StatutValidation == "Rejeté"),
                    PortalInviteEnvoye           = latestToken != null,
                    DernierAccesPortal           = latestToken?.DernierAcces
                });
            }

            return new DocumentCompletionSummaryDto
            {
                TotalClients        = completions.Count,
                ClientsComplets     = completions.Count(c => c.TauxCompletion == 100),
                ClientsEnCours      = completions.Count(c => c.TauxCompletion > 0 && c.TauxCompletion < 100),
                ClientsNonCommences = completions.Count(c => c.TauxCompletion == 0),
                TauxMoyenCompletion = completions.Count == 0 ? 0
                    : Math.Round(completions.Average(c => c.TauxCompletion), 1),
                Clients             = completions.OrderBy(c => c.TauxCompletion).ToList()
            };
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static string GenerateSecureToken()
        {
            var bytes = new byte[48];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private async Task NotifyAdminsAsync(Client client, string typeDocument)
        {
            try
            {
                var allUsers = await _userRepo.GetAllWithRolesAsync();
                var admins   = allUsers.Where(u =>
                    u.UserRoles.Any(ur => ur.Role.Name is "Admin" or "Superviseur"));

                foreach (var admin in admins)
                {
                    // In-app notification (persiste en base)
                    await _notificationService.CreateAsync(new CreateNotificationDto
                    {
                        UserId  = admin.Id,
                        Titre   = "Nouveau document via portail client",
                        Message = $"Le client {client.CodeClient} ({client.Prenom} {client.Nom}) " +
                                  $"a soumis le document « {typeDocument} » via le portail self-service.",
                        Type    = "Info",
                        Lien    = $"/detail-client/{client.Id}"
                    });

                    // SignalR push temps réel
                    await _portalNotifier.NotifyUserAsync(
                        admin.Id,
                        "Nouveau document via portail client",
                        $"{client.CodeClient} – {typeDocument}",
                        "portal_upload",
                        $"/detail-client/{client.Id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erreur lors de l'envoi des notifications admins (upload portail)");
            }
        }

        private static string BuildPortalInviteEmailBody(
            string nomComplet, string portalLink, DateTime expiration, string typeClient)
        {
            var expirationStr = expiration.ToString("dd/MM/yyyy à HH:mm");
            return $@"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family:Segoe UI,Arial,sans-serif;margin:0;padding:0;background:#f4f7fb;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f4f7fb;padding:40px 0;'>
    <tr><td align='center'>
      <table width='580' cellpadding='0' cellspacing='0'
             style='background:#fff;border-radius:16px;box-shadow:0 4px 20px rgba(0,0,0,.06);overflow:hidden;'>
        <tr>
          <td style='background:linear-gradient(135deg,#1a237e,#4a148c);padding:40px 32px;text-align:center;'>
            <h1 style='color:#fff;font-size:22px;font-weight:600;margin:0;'>Déposez vos documents en ligne</h1>
            <p style='color:rgba(255,255,255,.8);font-size:14px;margin:8px 0 0;'>Lumina Finance – Portail Client</p>
          </td>
        </tr>
        <tr>
          <td style='padding:40px 32px;'>
            <p style='color:#1e293b;font-size:16px;line-height:1.6;margin:0 0 20px;'>
              Bonjour <strong>{nomComplet}</strong>,
            </p>
            <p style='color:#475569;font-size:15px;line-height:1.6;margin:0 0 20px;'>
              Pour finaliser votre dossier <strong>{typeClient}</strong>, nous avons besoin de vos documents justificatifs.
              Cliquez ci-dessous pour accéder à votre espace sécurisé et déposer vos fichiers en ligne.
            </p>
            <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom:24px;'>
              <tr><td align='center'>
                <a href='{portalLink}'
                   style='display:inline-block;background:linear-gradient(135deg,#1a237e,#4a148c);
                          color:#fff;text-decoration:none;font-size:15px;font-weight:600;
                          padding:14px 40px;border-radius:10px;'>
                  Accéder à mon espace documents
                </a>
              </td></tr>
            </table>
            <table width='100%' cellpadding='16'
                   style='background:#f8fafc;border-radius:10px;margin-bottom:24px;'>
              <tr><td>
                <p style='margin:0;color:#64748b;font-size:13px;'>
                  ⏳ Lien valide jusqu'au <strong style='color:#1e293b;'>{expirationStr} UTC</strong>
                </p>
                <p style='margin:8px 0 0;color:#64748b;font-size:13px;'>
                  🔒 Lien sécurisé et personnel — ne le partagez pas
                </p>
                <p style='margin:8px 0 0;color:#64748b;font-size:13px;'>
                  📎 Formats PDF, JPG, PNG acceptés selon le document
                </p>
              </td></tr>
            </table>
            <p style='color:#94a3b8;font-size:12px;text-align:center;margin:0;'>
              Si vous n'êtes pas à l'origine de cette demande, ignorez cet email.<br/>
              &copy; 2024 Lumina Finance Solutions. Tous droits réservés.
            </p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";
        }
    }
}

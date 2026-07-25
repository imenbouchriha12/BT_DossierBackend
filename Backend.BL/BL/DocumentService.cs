using AutoMapper;
using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Client;
using Backend.DTO.Notification;
using Microsoft.Extensions.Logging;

namespace Backend.BL.BL
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<DocumentService> _logger;
        private readonly IHistoriqueService _historiqueService;
        private readonly ICurrentUserService _currentUser;
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        private readonly ClientStatusManager _statusManager;

        public DocumentService(
            IDocumentRepository documentRepository,
            IClientRepository clientRepository,
            IMapper mapper,
            ILogger<DocumentService> logger,
            IHistoriqueService historiqueService,
            ICurrentUserService currentUser,
            INotificationService notificationService,
            IUserRepository userRepository,
            ClientStatusManager statusManager)
        {
            _documentRepository = documentRepository;
            _clientRepository = clientRepository;
            _mapper = mapper;
            _logger = logger;
            _historiqueService = historiqueService;
            _currentUser = currentUser;
            _notificationService = notificationService;
            _userRepository = userRepository;
            _statusManager = statusManager;
        }

        public async Task<List<DocumentDto>> GetByClientIdAsync(int clientId)
        {
            var docs = await _documentRepository.GetByClientIdAsync(clientId);
            return _mapper.Map<List<DocumentDto>>(docs);
        }

        public async Task<PagedResult<DocumentDto>> GetByClientIdPagedAsync(int clientId, int page, int pageSize)
        {
            var (items, total) = await _documentRepository.GetByClientIdPagedAsync(clientId, page, pageSize);
            return new PagedResult<DocumentDto>
            {
                Items = _mapper.Map<List<DocumentDto>>(items),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<DocumentDto?> GetByIdAsync(int id)
        {
            var doc = await _documentRepository.GetByIdAsync(id);
            return doc == null ? null : _mapper.Map<DocumentDto>(doc);
        }

        public async Task<DocumentDto> CreateAsync(int idClient, string typeDocument, string fileName, byte[] fileBytes, string statutValidation = "En attente")
        {
            _logger.LogInformation("Ajout document pour le client {IdClient}", idClient);

            var client = await _clientRepository.GetByIdAsync(idClient);
            if (client == null)
                throw new KeyNotFoundException("Client introuvable.");

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Documents", client.CodeClient);
            Directory.CreateDirectory(uploadDir);

            var savedName = $"{typeDocument}_{client.Nom}_{client.Prenom}{Path.GetExtension(fileName)}"
                .Replace(" ", "_")
                .Replace("/", "_");
            var filePath = Path.Combine(uploadDir, savedName);

            await File.WriteAllBytesAsync(filePath, fileBytes);

            var relativePath = $"/documents/{client.CodeClient}/{savedName}";

            var document = new DocumentClient
            {
                IdClient = idClient,
                TypeDocument = typeDocument,
                NomFichier = savedName,
                CheminFichier = relativePath,
                DateAjout = DateTime.UtcNow,
                StatutValidation = statutValidation,
                UploadedByUserId = _currentUser.UserId
            };

            await _documentRepository.AddAsync(document);
            await _documentRepository.SaveChangesAsync();

            _logger.LogInformation("Document ajouté : {NomFichier}", savedName);

            await _historiqueService.EnregistrerAsync("Documents", document.Id, "Création", null, $"Document {typeDocument} - {savedName}", _currentUser.UserId, _currentUser.UserName);

            if (statutValidation == "En attente")
            {
                var admins = await _userRepository.GetAllWithRolesAsync();
                var adminUsers = admins.Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Admin" || ur.Role.Name == "Superviseur"));
                foreach (var admin in adminUsers)
                {
                    await _notificationService.CreateAsync(new CreateNotificationDto
                    {
                        UserId = admin.Id,
                        Titre = "Nouveau document en attente",
                        Message = $"Un document {typeDocument} pour le client {client.CodeClient} est en attente de validation.",
                        Type = "Info",
                        Lien = $"/detail-client/{idClient}"
                    });
                }
            }

            var created = await _documentRepository.GetByIdAsync(document.Id);
            
            // ✅ MISE À JOUR AUTOMATIQUE DU STATUT CLIENT
            try
            {
                await _statusManager.RecalculerStatutClientAsync(idClient);
                _logger.LogInformation("Statut client {ClientId} recalculé après ajout de document", idClient);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erreur lors du recalcul du statut client {ClientId}", idClient);
            }
            
            return _mapper.Map<DocumentDto>(created);
        }

        private static string GetSafePhysicalPath(string cheminFichier)
        {
            var rootDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
            var relativePath = cheminFichier.TrimStart('/').Replace('\\', '/');
            var physicalPath = Path.GetFullPath(Path.Combine(rootDir, relativePath));
            if (!physicalPath.StartsWith(Path.GetFullPath(rootDir), StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chemin de fichier non autorisé.");
            return physicalPath;
        }

        public async Task DeleteAsync(int id)
        {
            _logger.LogInformation("Suppression document {Id}", id);

            var doc = await _documentRepository.GetByIdAsync(id);
            if (doc == null)
                throw new KeyNotFoundException("Document introuvable.");

            var physicalPath = GetSafePhysicalPath(doc.CheminFichier);
            if (File.Exists(physicalPath))
                File.Delete(physicalPath);

            _documentRepository.Delete(doc);
            await _documentRepository.SaveChangesAsync();

            await _historiqueService.EnregistrerAsync("Documents", id, "Suppression", $"Document {doc.TypeDocument} - {doc.NomFichier}", "Supprimé", _currentUser.UserId, _currentUser.UserName);

            _logger.LogInformation("Document supprimé : {Id}", id);
        }

        public async Task<(byte[] FileBytes, string FileName, string ContentType)> DownloadAsync(int id)
        {
            var doc = await _documentRepository.GetByIdAsync(id);
            if (doc == null)
                throw new KeyNotFoundException("Document introuvable.");

            var physicalPath = GetSafePhysicalPath(doc.CheminFichier);
            if (!File.Exists(physicalPath))
                throw new FileNotFoundException("Fichier introuvable sur le serveur.");

            var fileBytes = await File.ReadAllBytesAsync(physicalPath);
            var contentType = GetContentType(doc.NomFichier);

            return (fileBytes, doc.NomFichier, contentType);
        }

        public async Task<DocumentDto> UpdateValidationAsync(int id, string statut)
        {
            _logger.LogInformation("Validation document {Id} -> {Statut}", id, statut);

            var doc = await _documentRepository.GetByIdAsync(id);
            if (doc == null)
                throw new KeyNotFoundException("Document introuvable.");

            var ancienStatut = doc.StatutValidation;
            doc.StatutValidation = statut;
            await _documentRepository.SaveChangesAsync();

            await _historiqueService.EnregistrerAsync("Documents", id, "Modification", $"Statut: {ancienStatut}", $"Statut: {statut}", _currentUser.UserId, _currentUser.UserName);

            if (doc.UploadedByUserId.HasValue && (statut == "Validé" || statut == "Rejeté"))
            {
                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    UserId = doc.UploadedByUserId.Value,
                    Titre = $"Document {statut}",
                    Message = $"Le document {doc.TypeDocument} a été {statut}.",
                    Type = statut == "Validé" ? "Succès" : "Erreur",
                    Lien = $"/detail-client/{doc.IdClient}"
                });
            }

            var updated = await _documentRepository.GetByIdAsync(id);
            
            // ✅ MISE À JOUR AUTOMATIQUE DU STATUT CLIENT après validation/rejet
            try
            {
                await _statusManager.RecalculerStatutClientAsync(doc.IdClient);
                _logger.LogInformation("Statut client {ClientId} recalculé après validation de document", doc.IdClient);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erreur lors du recalcul du statut client {ClientId}", doc.IdClient);
            }
            
            return _mapper.Map<DocumentDto>(updated);
        }

        public async Task<List<DocumentDto>> GetAllAsync()
        {
            var docs = await _documentRepository.GetAllAsync();
            return _mapper.Map<List<DocumentDto>>(docs);
        }

        private static string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }
    }
}

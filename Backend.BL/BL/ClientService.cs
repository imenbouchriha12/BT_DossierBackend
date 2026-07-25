using System.Text.Json;
using AutoMapper;
using Backend.BL.IBL;
using Backend.DAL.Entities;
using Backend.DAL.IDAL;
using Backend.DTO.Client;
using Backend.DTO.Notification;
using Microsoft.Extensions.Logging;

namespace Backend.BL.BL
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository _clientRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ClientService> _logger;
        private readonly IHistoriqueService _historiqueService;
        private readonly ICurrentUserService _currentUser;
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;

        public ClientService(IClientRepository clientRepository, IDocumentRepository documentRepository, IMapper mapper, ILogger<ClientService> logger, IHistoriqueService historiqueService, ICurrentUserService currentUser, INotificationService notificationService, IUserRepository userRepository)
        {
            _clientRepository = clientRepository;
            _documentRepository = documentRepository;
            _mapper = mapper;
            _logger = logger;
            _historiqueService = historiqueService;
            _currentUser = currentUser;
            _notificationService = notificationService;
            _userRepository = userRepository;
        }

        public async Task<PagedResult<ClientDto>> SearchAsync(ClientSearchDto search)
        {
            _logger.LogInformation("Recherche de clients avec les critères fournis");

            var statutEnum = !string.IsNullOrWhiteSpace(search.Statut)
                ? ParseStatut(search.Statut)
                : (StatutClient?)null;

            var (items, total) = await _clientRepository.SearchAsync(
                search.CodeClient, search.Nom, search.Prenom,
                search.Ville, statutEnum, search.DateDebut, search.DateFin,
                search.Page, search.PageSize);

            return new PagedResult<ClientDto>
            {
                Items = _mapper.Map<List<ClientDto>>(items),
                Total = total,
                Page = search.Page,
                PageSize = search.PageSize
            };
        }

        public async Task<PagedResult<ClientDto>> GetArchivedAsync(int page, int pageSize)
        {
            _logger.LogInformation("Récupération des clients archivés");

            var (items, total) = await _clientRepository.GetArchivedAsync(page, pageSize);

            return new PagedResult<ClientDto>
            {
                Items = _mapper.Map<List<ClientDto>>(items),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ClientDto?> GetByIdAsync(int id)
        {
            var client = await _clientRepository.GetByIdWithDetailAsync(id);
            if (client == null)
            {
                _logger.LogWarning("Client introuvable : {Id}", id);
                return null;
            }
            return _mapper.Map<ClientDto>(client);
        }

        public async Task<ClientDto> CreateAsync(CreateClientDto dto)
        {
            _logger.LogInformation("Création d'un nouveau client : {Nom} {Prenom}", dto.Nom, dto.Prenom);

            await using var transaction = await _clientRepository.BeginTransactionAsync();

            var codeClient = await GenerateUniqueCodeClientAsync();

            var client = new Client
            {
                CodeClient    = codeClient,
                Nom           = dto.Nom,
                Prenom        = dto.Prenom,
                DateNaissance = dto.DateNaissance,
                Sexe          = dto.Sexe,
                Nationalite   = dto.Nationalite,
                Telephone     = dto.Telephone,
                Email         = dto.Email,
                Adresse       = dto.Adresse,
                Ville         = dto.Ville,
                DateCreation  = DateTime.UtcNow,
                Statut        = ParseStatut(dto.Statut),
                TypeClient    = string.IsNullOrWhiteSpace(dto.TypeClient) ? "Particulier" : dto.TypeClient
            };

            if (dto.Detail != null)
            {
                client.Detail = new DetailClient
                {
                    Profession = dto.Detail.Profession,
                    Employeur = dto.Detail.Employeur,
                    SecteurActivite = dto.Detail.SecteurActivite,
                    RevenuMensuel = dto.Detail.RevenuMensuel,
                    SituationFamiliale = dto.Detail.SituationFamiliale,
                    TypeResidence = dto.Detail.TypeResidence
                };
            }

            await _clientRepository.AddAsync(client);
            await _clientRepository.SaveChangesAsync();

            _logger.LogInformation("Client créé avec succès : {CodeClient}", codeClient);

            await _historiqueService.EnregistrerAsync("Clients", client.Id, "Création", null, JsonSerializer.Serialize(new { client.CodeClient, client.Nom, client.Prenom, client.Statut }), _currentUser.UserId, _currentUser.UserName);

            await transaction.CommitAsync();

            var created = await _clientRepository.GetByIdWithDetailAsync(client.Id);
            return _mapper.Map<ClientDto>(created);
        }

        public async Task<ClientDto> UpdateAsync(int id, UpdateClientDto dto)
        {
            _logger.LogInformation("Mise à jour du client {Id}", id);

            await using var transaction = await _clientRepository.BeginTransactionAsync();

            var client = await _clientRepository.GetByIdWithDetailAsync(id);
            if (client == null)
                throw new KeyNotFoundException("Client introuvable.");

            var changes = new List<string>();
            void Track(string field, string? old, string? @new)
            {
                if (old != @new) changes.Add($"{field}: \"{old}\" → \"{@new}\"");
            }

            var oldNom = client.Nom; var oldPrenom = client.Prenom;
            var oldDateNaissance = client.DateNaissance.ToString("yyyy-MM-dd");
            var oldSexe = client.Sexe; var oldNationalite = client.Nationalite;
            var oldTelephone = client.Telephone; var oldEmail = client.Email;
            var oldAdresse = client.Adresse; var oldVille = client.Ville;
            var oldStatut = client.Statut;

            client.Nom = dto.Nom;
            client.Prenom = dto.Prenom;
            client.DateNaissance = dto.DateNaissance;
            client.Sexe = dto.Sexe;
            client.Nationalite = dto.Nationalite;
            client.Telephone = dto.Telephone;
            client.Email = dto.Email;
            client.Adresse = dto.Adresse;
            client.Ville = dto.Ville;
            client.Statut = ParseStatut(dto.Statut);

            Track("Nom", oldNom, dto.Nom);
            Track("Prénom", oldPrenom, dto.Prenom);
            Track("Date naissance", oldDateNaissance, dto.DateNaissance.ToString("yyyy-MM-dd"));
            Track("Sexe", oldSexe, dto.Sexe);
            Track("Nationalité", oldNationalite, dto.Nationalite);
            Track("Téléphone", oldTelephone, dto.Telephone);
            Track("Email", oldEmail, dto.Email);
            Track("Adresse", oldAdresse, dto.Adresse);
            Track("Ville", oldVille, dto.Ville);
            Track("Statut", oldStatut.ToString(), ParseStatut(dto.Statut).ToString());

            if (dto.Detail != null)
            {
                if (client.Detail == null)
                {
                    client.Detail = new DetailClient { IdClient = client.Id };
                }

                var oldProfession = client.Detail.Profession;
                var oldEmployeur = client.Detail.Employeur;
                var oldSecteur = client.Detail.SecteurActivite;
                var oldRevenu = client.Detail.RevenuMensuel.ToString();
                var oldSitFam = client.Detail.SituationFamiliale;
                var oldTypeRes = client.Detail.TypeResidence;

                client.Detail.Profession = dto.Detail.Profession;
                client.Detail.Employeur = dto.Detail.Employeur;
                client.Detail.SecteurActivite = dto.Detail.SecteurActivite;
                client.Detail.RevenuMensuel = dto.Detail.RevenuMensuel;
                client.Detail.SituationFamiliale = dto.Detail.SituationFamiliale;
                client.Detail.TypeResidence = dto.Detail.TypeResidence;

                Track("Profession", oldProfession, dto.Detail.Profession);
                Track("Employeur", oldEmployeur, dto.Detail.Employeur);
                Track("Secteur activité", oldSecteur, dto.Detail.SecteurActivite);
                Track("Revenu mensuel", oldRevenu, dto.Detail.RevenuMensuel.ToString());
                Track("Situation familiale", oldSitFam, dto.Detail.SituationFamiliale);
                Track("Type résidence", oldTypeRes, dto.Detail.TypeResidence);
            }

            await _clientRepository.SaveChangesAsync();

            _logger.LogInformation("Client mis à jour : {Id}", id);

            var avant = changes.Count > 0 ? string.Join("; ", changes) : null;
            var apres = changes.Count > 0 ? $"Modification de {changes.Count} champ(s)" : "Aucune modification";
            await _historiqueService.EnregistrerAsync("Clients", id, "Modification", avant, apres, _currentUser.UserId, _currentUser.UserName);

            if (oldStatut != client.Statut)
            {
                var notificationType = client.Statut switch
                {
                    StatutClient.Actif => "Succès",
                    StatutClient.Bloqué => "Erreur",
                    _ => "Info"
                };
                var allUsers = await _userRepository.GetAllWithRolesAsync();
                var admins = allUsers.Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Admin" || ur.Role.Name == "Superviseur"));
                foreach (var admin in admins)
                {
                    await _notificationService.CreateAsync(new CreateNotificationDto
                    {
                        UserId = admin.Id,
                        Titre = "Statut client modifié",
                        Message = $"Le statut du client {client.CodeClient} ({client.Nom} {client.Prenom}) est passé à {client.Statut}.",
                        Type = notificationType,
                        Lien = $"/detail-client/{id}"
                    });
                }
            }

            await transaction.CommitAsync();

            var updated = await _clientRepository.GetByIdWithDetailAsync(id);
            return _mapper.Map<ClientDto>(updated);
        }

        public async Task ArchiveAsync(int id)
        {
            _logger.LogInformation("Archivage du client {Id}", id);

            await using var transaction = await _clientRepository.BeginTransactionAsync();

            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
                throw new KeyNotFoundException("Client introuvable.");

            client.EstArchive = true;
            await _clientRepository.SaveChangesAsync();

            await _historiqueService.EnregistrerAsync("Clients", id, "Archivage", $"Client {client.CodeClient} actif", $"Client {client.CodeClient} archivé", _currentUser.UserId, _currentUser.UserName);

            await transaction.CommitAsync();

            _logger.LogInformation("Client archivé : {Id}", id);
        }

        public async Task RestoreAsync(int id)
        {
            _logger.LogInformation("Restauration du client {Id}", id);

            await using var transaction = await _clientRepository.BeginTransactionAsync();

            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
                throw new KeyNotFoundException("Client introuvable.");

            client.EstArchive = false;
            await _clientRepository.SaveChangesAsync();

            await _historiqueService.EnregistrerAsync("Clients", id, "Restauration", $"Client {client.CodeClient} archivé", $"Client {client.CodeClient} restauré", _currentUser.UserId, _currentUser.UserName);

            await transaction.CommitAsync();

            _logger.LogInformation("Client restauré : {Id}", id);
        }

        public async Task<int> ImportExcelAsync(Stream fileStream, string fileName)
        {
            _logger.LogInformation("Import de clients depuis le fichier : {FileName}", fileName);

            var imported = 0;
            var errors = new List<string>();

            using var package = new OfficeOpenXml.ExcelPackage(fileStream);
            var worksheet = package.Workbook.Worksheets[0];
            var rowCount = worksheet.Dimension?.Rows ?? 0;

            if (rowCount < 2)
                throw new InvalidOperationException("Le fichier est vide.");

            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var codeClient = worksheet.Cells[row, 1]?.Text?.Trim();
                    var nom = worksheet.Cells[row, 2]?.Text?.Trim();
                    var prenom = worksheet.Cells[row, 3]?.Text?.Trim();
                    var telephone = worksheet.Cells[row, 4]?.Text?.Trim();
                    var email = worksheet.Cells[row, 5]?.Text?.Trim();
                    var ville = worksheet.Cells[row, 6]?.Text?.Trim();
                    var statutText = worksheet.Cells[row, 7]?.Text?.Trim();

                    if (string.IsNullOrWhiteSpace(nom) || string.IsNullOrWhiteSpace(prenom))
                        continue;

                    var statut = StatutClient.Actif;
                    if (!string.IsNullOrWhiteSpace(statutText))
                    {
                        statutText = statutText switch
                        {
                            "En attente" or "EnAttente" => "EnAttente",
                            "Bloqué" or "Bloque" => "Bloqué",
                            _ => statutText
                        };
                        statut = (StatutClient)Enum.Parse(typeof(StatutClient), statutText);
                    }

                    var client = new Client
                    {
                        CodeClient = string.IsNullOrWhiteSpace(codeClient)
                            ? await GenerateUniqueCodeClientAsync()
                            : codeClient,
                        Nom = nom,
                        Prenom = prenom,
                        Telephone = telephone,
                        Email = email,
                        Ville = ville,
                        Statut = statut,
                        DateCreation = DateTime.UtcNow
                    };

                    if (!string.IsNullOrWhiteSpace(codeClient) && await _clientRepository.CodeClientExistsAsync(codeClient))
                        client.CodeClient = await GenerateUniqueCodeClientAsync();

                    await _clientRepository.AddAsync(client);
                    imported++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Erreur à la ligne {Row} : {Message}", row, ex.Message);
                    errors.Add($"Ligne {row}: {ex.Message}");
                }
            }

            if (imported > 0)
                await _clientRepository.SaveChangesAsync();

            _logger.LogInformation("Import terminé : {Count} client(s) importé(s), {Errors} erreur(s)", imported, errors.Count);

            if (errors.Count > 0 && imported == 0)
                throw new InvalidOperationException($"Aucun client importé. {errors.Count} erreur(s).");

            return imported;
        }

        public async Task<byte[]> ExportExcelAsync(ClientSearchDto search)
        {
            _logger.LogInformation("Export Excel des clients");

            search.PageSize = int.MaxValue;
            search.Page = 1;
            var result = await SearchAsync(search);

            var stream = new System.IO.MemoryStream();
            using (var package = new OfficeOpenXml.ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.Add("Clients");

                worksheet.Cells[1, 1].Value = "Code Client";
                worksheet.Cells[1, 2].Value = "Nom";
                worksheet.Cells[1, 3].Value = "Prénom";
                worksheet.Cells[1, 4].Value = "Date Naissance";
                worksheet.Cells[1, 5].Value = "Sexe";
                worksheet.Cells[1, 6].Value = "Nationalité";
                worksheet.Cells[1, 7].Value = "Téléphone";
                worksheet.Cells[1, 8].Value = "Email";
                worksheet.Cells[1, 9].Value = "Adresse";
                worksheet.Cells[1, 10].Value = "Ville";
                worksheet.Cells[1, 11].Value = "Date Création";
                worksheet.Cells[1, 12].Value = "Statut";
                worksheet.Cells[1, 13].Value = "Profession";
                worksheet.Cells[1, 14].Value = "Employeur";
                worksheet.Cells[1, 15].Value = "Secteur Activité";
                worksheet.Cells[1, 16].Value = "Revenu Mensuel";
                worksheet.Cells[1, 17].Value = "Situation Familiale";
                worksheet.Cells[1, 18].Value = "Type Résidence";

                using (var range = worksheet.Cells[1, 1, 1, 18])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                int row = 2;
                foreach (var client in result.Items)
                {
                    worksheet.Cells[row, 1].Value = client.CodeClient;
                    worksheet.Cells[row, 2].Value = client.Nom;
                    worksheet.Cells[row, 3].Value = client.Prenom;
                    worksheet.Cells[row, 4].Value = client.DateNaissance.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 5].Value = client.Sexe;
                    worksheet.Cells[row, 6].Value = client.Nationalite;
                    worksheet.Cells[row, 7].Value = client.Telephone;
                    worksheet.Cells[row, 8].Value = client.Email;
                    worksheet.Cells[row, 9].Value = client.Adresse;
                    worksheet.Cells[row, 10].Value = client.Ville;
                    worksheet.Cells[row, 11].Value = client.DateCreation.ToString("yyyy-MM-dd HH:mm");
                    worksheet.Cells[row, 12].Value = client.Statut.ToString();
                    worksheet.Cells[row, 13].Value = client.Detail?.Profession;
                    worksheet.Cells[row, 14].Value = client.Detail?.Employeur;
                    worksheet.Cells[row, 15].Value = client.Detail?.SecteurActivite;
                    worksheet.Cells[row, 16].Value = client.Detail?.RevenuMensuel;
                    worksheet.Cells[row, 17].Value = client.Detail?.SituationFamiliale;
                    worksheet.Cells[row, 18].Value = client.Detail?.TypeResidence;
                    row++;
                }

                worksheet.Cells.AutoFitColumns();
                package.Save();
            }

            stream.Position = 0;
            _logger.LogInformation("Export Excel terminé : {Count} clients", result.Items.Count);
            return stream.ToArray();
        }

        public async Task<ClientStatsDto> GetStatsAsync()
        {
            _logger.LogInformation("Récupération des statistiques clients");

            var debutMois = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var total = await _clientRepository.CountAsync();
            var archived = await _clientRepository.CountArchivedAsync();
            var nouveauxMois = await _clientRepository.CountByDateRangeAsync(debutMois, DateTime.UtcNow);
            var hommes = await _clientRepository.CountBySexeAsync("M");
            var femmes = await _clientRepository.CountBySexeAsync("F");
            var villes = await _clientRepository.GroupByVilleAsync();
            var statuts = await _clientRepository.GroupByStatutAsync();
            var mois = await _clientRepository.GroupByMonthAsync();
            var recents = await _clientRepository.GetRecentAsync(5);

            var actifs = statuts.FirstOrDefault(g => g.Key == StatutClient.Actif)?.Count() ?? 0;
            var enAttente = statuts.FirstOrDefault(g => g.Key == StatutClient.EnAttente)?.Count() ?? 0;
            var bloques = statuts.FirstOrDefault(g => g.Key == StatutClient.Bloqué)?.Count() ?? 0;

            var docsRecentsData = await _documentRepository.GetRecentAsync(5);
            var docsRecents = docsRecentsData
                .Select(d => new DocumentRecentsItem
                {
                    Id = d.Id,
                    TypeDocument = d.TypeDocument,
                    NomFichier = d.NomFichier,
                    StatutValidation = d.StatutValidation,
                    CodeClient = d.Client.CodeClient,
                    NomClient = d.Client.Nom + " " + d.Client.Prenom,
                    DateAjout = d.DateAjout
                }).ToList();

            var totalDocs = await _documentRepository.CountAsync();
            var docsValides = await _documentRepository.CountByStatutAsync("Validé");
            var docsEnAttente = await _documentRepository.CountByStatutAsync("En attente");
            var docsRejetes = await _documentRepository.CountByStatutAsync("Rejeté");

            return new ClientStatsDto
            {
                TotalClients = total,
                ClientsActifs = actifs,
                ClientsEnAttente = enAttente,
                ClientsBloques = bloques,
                NouveauxClientsMois = nouveauxMois,
                Hommes = hommes,
                Femmes = femmes,
                TotalDocuments = totalDocs,
                DocumentsValides = docsValides,
                DocumentsEnAttente = docsEnAttente,
                DocumentsRejetes = docsRejetes,
                RepartitionVille = villes.Select(g => new VilleStat { Ville = g.Key, Count = g.Count() }).ToList(),
                RepartitionStatut = statuts.Select(g => new StatutStat { Statut = g.Key.ToString(), Count = g.Count() }).ToList(),
                CreationsParMois = mois.Select(g => new CreationMois { Mois = g.Key, Count = g.Count() }).OrderBy(x => x.Mois).ToList(),
                ClientsRecents = recents.Select(c => new ClientRecentsItem
                {
                    Id = c.Id,
                    CodeClient = c.CodeClient,
                    Nom = c.Nom,
                    Prenom = c.Prenom,
                    Ville = c.Ville,
                    DateCreation = c.DateCreation
                }).ToList(),
                DocumentsRecents = docsRecents
            };
        }

        public async Task<List<string>> GetAllVillesAsync()
        {
            return await _clientRepository.GetDistinctVillesAsync();
        }

        private async Task<string> GenerateUniqueCodeClientAsync()
        {
            string code;
            do
            {
                code = Random.Shared.Next(1000000000, 2147483647).ToString();
            }
            while (await _clientRepository.CodeClientExistsAsync(code));
            return code;
        }

        private static StatutClient ParseStatut(string? statut)
        {
            if (string.IsNullOrWhiteSpace(statut))
                return StatutClient.Actif;

            return statut switch
            {
                "Actif" => StatutClient.Actif,
                "EnAttente" or "En attente" => StatutClient.EnAttente,
                "Bloqué" or "Bloque" => StatutClient.Bloqué,
                _ when Enum.TryParse<StatutClient>(statut, ignoreCase: true, out var result) => result,
                _ => StatutClient.Actif
            };
        }
    }
}

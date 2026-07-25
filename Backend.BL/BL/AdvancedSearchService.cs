using System.Diagnostics;
using System.Text.Json;
using AutoMapper;
using Backend.BL.IBL;
using Backend.DAL;
using Backend.DAL.Entities;
using Backend.DTO.Search;
using Microsoft.EntityFrameworkCore;

namespace Backend.BL.BL
{
    /// <summary>
    /// Service de recherche avancée avec fonctionnalités intelligentes
    /// </summary>
    public class AdvancedSearchService : IAdvancedSearchService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;

        public AdvancedSearchService(
            AppDbContext context,
            ICurrentUserService currentUser,
            IMapper mapper)
        {
            _context = context;
            _currentUser = currentUser;
            _mapper = mapper;
        }

        public async Task<AdvancedSearchResultDto> SearchAsync(AdvancedSearchRequestDto request)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new AdvancedSearchResultDto
            {
                Metadata = new SearchMetadata
                {
                    SearchQuery = request.Query ?? string.Empty,
                    SearchMode = request.Mode,
                    CurrentPage = request.Page,
                    PageSize = request.PageSize
                }
            };

            // Recherche dans les catégories demandées
            if (request.Categories.Contains(SearchCategory.All) || request.Categories.Contains(SearchCategory.Clients))
            {
                result.Clients = await SearchClientsAsync(request);
                result.Metadata.TotalClients = result.Clients.Count;
            }

            if (request.Categories.Contains(SearchCategory.All) || request.Categories.Contains(SearchCategory.Documents))
            {
                result.Documents = await SearchDocumentsAsync(request);
                result.Metadata.TotalDocuments = result.Documents.Count;
            }

            if (request.Categories.Contains(SearchCategory.All) || request.Categories.Contains(SearchCategory.Users))
            {
                result.Users = await SearchUsersAsync(request);
                result.Metadata.TotalUsers = result.Users.Count;
            }

            // Calcul des totaux
            result.Metadata.TotalResults = result.Metadata.TotalClients + 
                                           result.Metadata.TotalDocuments + 
                                           result.Metadata.TotalUsers;
            result.Metadata.TotalPages = (int)Math.Ceiling(result.Metadata.TotalResults / (double)request.PageSize);

            stopwatch.Stop();
            result.Metadata.ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds;

            // Suggestions et tags populaires
            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                result.Suggestions = await GenerateSuggestionsAsync(request.Query);
            }
            result.PopularTags = await GetPopularTagsAsync(10);

            // Enregistrer dans l'historique
            await SaveSearchHistoryAsync(request, result.Metadata.TotalResults, result.Metadata.ExecutionTimeMs);

            return result;
        }

        private async Task<List<ClientSearchResultDto>> SearchClientsAsync(AdvancedSearchRequestDto request)
        {
            var query = _context.Clients
                .Include(c => c.Detail)
                .Where(c => !c.EstArchive)
                .AsQueryable();

            // Recherche textuelle
            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                query = ApplyTextSearch(query, request.Query, request.Mode);
            }

            // Filtres spécifiques clients
            if (request.ClientFilters != null)
            {
                query = ApplyClientFilters(query, request.ClientFilters);
            }

            // Filtrage par tags
            if (request.Tags != null && request.Tags.Any())
            {
                var clientIdsWithTags = await _context.ClientTags
                    .Where(t => request.Tags.Contains(t.Tag))
                    .Select(t => t.IdClient)
                    .Distinct()
                    .ToListAsync();

                query = query.Where(c => clientIdsWithTags.Contains(c.Id));
            }

            // Récupération des données
            var clients = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => new ClientSearchResultDto
                {
                    Id = c.Id,
                    CodeClient = c.CodeClient,
                    Nom = c.Nom,
                    Prenom = c.Prenom,
                    NomComplet = c.Prenom + " " + c.Nom,
                    Email = c.Email,
                    Telephone = c.Telephone,
                    Ville = c.Ville,
                    Statut = c.Statut.ToString(),
                    DateCreation = c.DateCreation,
                    RevenuMensuel = c.Detail != null ? c.Detail.RevenuMensuel : null,
                    Profession = c.Detail != null ? c.Detail.Profession : null,
                    Tags = new List<string>()
                })
                .ToListAsync();

            // Charger les tags en une seule requête (N+1 fix)
            var clientIds = clients.Select(c => c.Id).ToList();
            var clientTags = await _context.ClientTags
                .Where(t => clientIds.Contains(t.IdClient))
                .GroupBy(t => t.IdClient)
                .ToDictionaryAsync(g => g.Key, g => g.Select(t => t.Tag).ToList());

            foreach (var client in clients)
            {
                client.Tags = clientTags.TryGetValue(client.Id, out var tags) ? tags : new List<string>();
                client.RelevanceScore = CalculateRelevanceScore(request.Query, client);
                client.MatchedFields = GetMatchedFields(request.Query, client);
            }

            // Tri
            clients = ApplySorting(clients, request.SortBy, request.SortDirection);

            return clients;
        }

        private IQueryable<Client> ApplyTextSearch(IQueryable<Client> query, string searchText, SearchMode mode)
        {
            searchText = searchText.Trim().ToLower();

            return mode switch
            {
                SearchMode.Exact => query.Where(c =>
                    c.CodeClient.ToLower() == searchText ||
                    c.Nom.ToLower() == searchText ||
                    c.Prenom.ToLower() == searchText ||
                    c.Email.ToLower() == searchText),

                SearchMode.Phonetic => query.Where(c =>
                    EF.Functions.Like(c.Nom, $"%{GetPhoneticPattern(searchText)}%") ||
                    EF.Functions.Like(c.Prenom, $"%{GetPhoneticPattern(searchText)}%") ||
                    c.CodeClient.Contains(searchText) ||
                    c.Email.Contains(searchText) ||
                    c.Telephone.Contains(searchText) ||
                    c.Ville.Contains(searchText) ||
                    c.Adresse.Contains(searchText)),

                SearchMode.Fuzzy or _ => query.Where(c =>
                    c.CodeClient.Contains(searchText) ||
                    c.Nom.Contains(searchText) ||
                    c.Prenom.Contains(searchText) ||
                    (c.Prenom + " " + c.Nom).Contains(searchText) ||
                    (c.Nom + " " + c.Prenom).Contains(searchText) ||
                    c.Email.Contains(searchText) ||
                    c.Telephone.Contains(searchText) ||
                    c.Ville.Contains(searchText) ||
                    c.Adresse.Contains(searchText) ||
                    c.Nationalite.Contains(searchText))
            };
        }

        private IQueryable<Client> ApplyClientFilters(IQueryable<Client> query, ClientFilters filters)
        {
            if (filters.Villes != null && filters.Villes.Any())
                query = query.Where(c => filters.Villes.Contains(c.Ville));

            if (filters.Statuts != null && filters.Statuts.Any())
            {
                var statutsEnum = filters.Statuts.Select(s => Enum.Parse<StatutClient>(s.Replace(" ", ""))).ToList();
                query = query.Where(c => statutsEnum.Contains(c.Statut));
            }

            if (filters.Sexe != null)
                query = query.Where(c => c.Sexe == filters.Sexe);

            if (filters.Nationalite != null)
                query = query.Where(c => c.Nationalite.Contains(filters.Nationalite));

            if (filters.DateCreationDebut.HasValue)
                query = query.Where(c => c.DateCreation >= filters.DateCreationDebut.Value);

            if (filters.DateCreationFin.HasValue)
                query = query.Where(c => c.DateCreation <= filters.DateCreationFin.Value);

            if (filters.AgeMin.HasValue || filters.AgeMax.HasValue)
            {
                var today = DateTime.Today;
                if (filters.AgeMin.HasValue)
                {
                    var maxBirthDate = today.AddYears(-filters.AgeMin.Value);
                    query = query.Where(c => c.DateNaissance <= maxBirthDate);
                }
                if (filters.AgeMax.HasValue)
                {
                    var minBirthDate = today.AddYears(-filters.AgeMax.Value - 1);
                    query = query.Where(c => c.DateNaissance > minBirthDate);
                }
            }

            // Filtres sur les détails clients
            if (filters.RevenuMin.HasValue || filters.RevenuMax.HasValue ||
                filters.SituationsFamiliales != null || filters.SecteursActivite != null)
            {
                query = query.Where(c => c.Detail != null);

                if (filters.RevenuMin.HasValue)
                    query = query.Where(c => c.Detail!.RevenuMensuel >= filters.RevenuMin.Value);

                if (filters.RevenuMax.HasValue)
                    query = query.Where(c => c.Detail!.RevenuMensuel <= filters.RevenuMax.Value);

                if (filters.SituationsFamiliales != null && filters.SituationsFamiliales.Any())
                    query = query.Where(c => filters.SituationsFamiliales.Contains(c.Detail!.SituationFamiliale));

                if (filters.SecteursActivite != null && filters.SecteursActivite.Any())
                    query = query.Where(c => filters.SecteursActivite.Contains(c.Detail!.SecteurActivite));
            }

            return query;
        }

        private async Task<List<DocumentSearchResultDto>> SearchDocumentsAsync(AdvancedSearchRequestDto request)
        {
            var query = _context.DocumentClients
                .Include(d => d.Client)
                .AsQueryable();

            // Recherche textuelle
            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                var searchText = request.Query.Trim().ToLower();
                query = query.Where(d =>
                    d.NomFichier.Contains(searchText) ||
                    d.TypeDocument.Contains(searchText) ||
                    d.Client.CodeClient.Contains(searchText) ||
                    d.Client.Nom.Contains(searchText) ||
                    d.Client.Prenom.Contains(searchText));
            }

            // Filtres spécifiques documents
            if (request.DocumentFilters != null)
            {
                query = ApplyDocumentFilters(query, request.DocumentFilters);
            }

            // Filtrage par tags
            if (request.Tags != null && request.Tags.Any())
            {
                var docIdsWithTags = await _context.DocumentTags
                    .Where(t => request.Tags.Contains(t.Tag))
                    .Select(t => t.IdDocument)
                    .Distinct()
                    .ToListAsync();

                query = query.Where(d => docIdsWithTags.Contains(d.Id));
            }

            var documents = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(d => new DocumentSearchResultDto
                {
                    Id = d.Id,
                    IdClient = d.IdClient,
                    CodeClient = d.Client.CodeClient,
                    NomClient = d.Client.Prenom + " " + d.Client.Nom,
                    TypeDocument = d.TypeDocument,
                    NomFichier = d.NomFichier,
                    StatutValidation = d.StatutValidation,
                    DateAjout = d.DateAjout,
                    Tags = new List<string>()
                })
                .ToListAsync();

            // Charger les tags en une seule requête (N+1 fix)
            var docIds = documents.Select(d => d.Id).ToList();
            var docTags = await _context.DocumentTags
                .Where(t => docIds.Contains(t.IdDocument))
                .GroupBy(t => t.IdDocument)
                .ToDictionaryAsync(g => g.Key, g => g.Select(t => t.Tag).ToList());

            foreach (var doc in documents)
            {
                doc.Tags = docTags.TryGetValue(doc.Id, out var tags) ? tags : new List<string>();
                doc.RelevanceScore = CalculateDocumentRelevanceScore(request.Query, doc);
                doc.MatchedFields = GetDocumentMatchedFields(request.Query, doc);
            }

            return documents;
        }

        private IQueryable<DocumentClient> ApplyDocumentFilters(IQueryable<DocumentClient> query, DocumentFilters filters)
        {
            if (filters.TypesDocument != null && filters.TypesDocument.Any())
                query = query.Where(d => filters.TypesDocument.Contains(d.TypeDocument));

            if (filters.StatutsValidation != null && filters.StatutsValidation.Any())
                query = query.Where(d => filters.StatutsValidation.Contains(d.StatutValidation));

            if (filters.DateAjoutDebut.HasValue)
                query = query.Where(d => d.DateAjout >= filters.DateAjoutDebut.Value);

            if (filters.DateAjoutFin.HasValue)
                query = query.Where(d => d.DateAjout <= filters.DateAjoutFin.Value);

            if (filters.Extensions != null && filters.Extensions.Any())
            {
                query = query.Where(d => filters.Extensions.Any(ext => d.NomFichier.EndsWith(ext)));
            }

            return query;
        }

        private async Task<List<UserSearchResultDto>> SearchUsersAsync(AdvancedSearchRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return new List<UserSearchResultDto>();

            var searchText = request.Query.Trim().ToLower();

            var users = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.Username.Contains(searchText) ||
                           u.Email.Contains(searchText) ||
                           u.Matricule.Contains(searchText))
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(u => new UserSearchResultDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Matricule = u.Matricule,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                })
                .ToListAsync();

            foreach (var user in users)
            {
                user.RelevanceScore = CalculateUserRelevanceScore(request.Query, user);
            }

            return users;
        }

        // Calcul du score de pertinence pour les clients
        private double CalculateRelevanceScore(string? query, ClientSearchResultDto client)
        {
            if (string.IsNullOrWhiteSpace(query))
                return 1.0;

            double score = 0;
            query = query.ToLower();

            // Correspondance exacte du code client (score élevé)
            if (client.CodeClient.ToLower() == query) score += 10;
            else if (client.CodeClient.ToLower().Contains(query)) score += 5;

            // Correspondance nom/prénom
            if (client.Nom.ToLower() == query || client.Prenom.ToLower() == query) score += 8;
            else if (client.NomComplet.ToLower().Contains(query)) score += 6;
            else if (client.Nom.ToLower().Contains(query) || client.Prenom.ToLower().Contains(query)) score += 3;

            // Email et téléphone
            if (client.Email.ToLower().Contains(query)) score += 4;
            if (client.Telephone.Contains(query)) score += 4;

            // Ville et autres
            if (client.Ville.ToLower().Contains(query)) score += 2;
            if (client.Profession?.ToLower().Contains(query) == true) score += 2;

            // Tags
            if (client.Tags.Any(t => t.ToLower().Contains(query))) score += 3;

            return score;
        }

        private double CalculateDocumentRelevanceScore(string? query, DocumentSearchResultDto doc)
        {
            if (string.IsNullOrWhiteSpace(query))
                return 1.0;

            double score = 0;
            query = query.ToLower();

            if (doc.TypeDocument.ToLower().Contains(query)) score += 5;
            if (doc.NomFichier.ToLower().Contains(query)) score += 4;
            if (doc.CodeClient.ToLower().Contains(query)) score += 3;
            if (doc.NomClient.ToLower().Contains(query)) score += 2;
            if (doc.Tags.Any(t => t.ToLower().Contains(query))) score += 3;

            return score;
        }

        private double CalculateUserRelevanceScore(string? query, UserSearchResultDto user)
        {
            if (string.IsNullOrWhiteSpace(query))
                return 1.0;

            double score = 0;
            query = query.ToLower();

            if (user.Username.ToLower() == query) score += 10;
            else if (user.Username.ToLower().Contains(query)) score += 5;
            if (user.Email.ToLower().Contains(query)) score += 4;
            if (user.Matricule.ToLower().Contains(query)) score += 6;

            return score;
        }

        private List<string> GetMatchedFields(string? query, ClientSearchResultDto client)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<string>();

            var matched = new List<string>();
            query = query.ToLower();

            if (client.CodeClient.ToLower().Contains(query)) matched.Add("Code Client");
            if (client.Nom.ToLower().Contains(query)) matched.Add("Nom");
            if (client.Prenom.ToLower().Contains(query)) matched.Add("Prénom");
            if (client.Email.ToLower().Contains(query)) matched.Add("Email");
            if (client.Telephone.Contains(query)) matched.Add("Téléphone");
            if (client.Ville.ToLower().Contains(query)) matched.Add("Ville");
            if (client.Profession?.ToLower().Contains(query) == true) matched.Add("Profession");
            if (client.Tags.Any(t => t.ToLower().Contains(query))) matched.Add("Tags");

            return matched;
        }

        private List<string> GetDocumentMatchedFields(string? query, DocumentSearchResultDto doc)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<string>();

            var matched = new List<string>();
            query = query.ToLower();

            if (doc.TypeDocument.ToLower().Contains(query)) matched.Add("Type Document");
            if (doc.NomFichier.ToLower().Contains(query)) matched.Add("Nom Fichier");
            if (doc.CodeClient.ToLower().Contains(query)) matched.Add("Code Client");
            if (doc.NomClient.ToLower().Contains(query)) matched.Add("Nom Client");
            if (doc.Tags.Any(t => t.ToLower().Contains(query))) matched.Add("Tags");

            return matched;
        }

        private List<ClientSearchResultDto> ApplySorting(List<ClientSearchResultDto> clients, SortOption sortBy, SortDirection direction)
        {
            var ordered = sortBy switch
            {
                SortOption.Relevance => clients.OrderByDescending(c => c.RelevanceScore),
                SortOption.DateCreated => clients.OrderBy(c => c.DateCreation),
                SortOption.Name => clients.OrderBy(c => c.NomComplet),
                SortOption.Code => clients.OrderBy(c => c.CodeClient),
                _ => clients.OrderByDescending(c => c.RelevanceScore)
            };

            return direction == SortDirection.Asc 
                ? ordered.ToList() 
                : ordered.Reverse().ToList();
        }

        // Recherche phonétique simplifiée (Soundex-like)
        private string GetPhoneticPattern(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Simplification phonétique française
            text = text.ToLower()
                .Replace("ph", "f")
                .Replace("tion", "sion")
                .Replace("eau", "o")
                .Replace("au", "o")
                .Replace("ai", "é")
                .Replace("ei", "é")
                .Replace("ch", "sh")
                .Replace("qu", "k")
                .Replace("c", "k")
                .Replace("ç", "s");

            return text;
        }

        public async Task<GlobalSearchResultDto> GlobalSearchAsync(string query, int limit = 5)
        {
            var results = new GlobalSearchResultDto();

            results.Clients = await _context.Clients
                .Where(c => !c.EstArchive && (
                    c.CodeClient.Contains(query) ||
                    c.Nom.Contains(query) ||
                    c.Prenom.Contains(query) ||
                    (c.Nom + " " + c.Prenom).Contains(query) ||
                    (c.Prenom + " " + c.Nom).Contains(query)))
                .Take(limit)
                .Select(c => new ClientSearchItemDto
                {
                    Id = c.Id,
                    CodeClient = c.CodeClient,
                    Nom = c.Nom,
                    Prenom = c.Prenom,
                    Ville = c.Ville,
                    Statut = c.Statut.ToString()
                })
                .ToListAsync();

            results.Documents = await _context.DocumentClients
                .Include(d => d.Client)
                .Where(d => d.NomFichier.Contains(query) || d.TypeDocument.Contains(query))
                .Take(limit)
                .Select(d => new DocumentSearchItemDto
                {
                    Id = d.Id,
                    IdClient = d.IdClient,
                    CodeClient = d.Client != null ? d.Client.CodeClient : "",
                    NomClient = d.Client != null ? d.Client.Prenom + " " + d.Client.Nom : "",
                    TypeDocument = d.TypeDocument,
                    NomFichier = d.NomFichier,
                    StatutValidation = d.StatutValidation
                })
                .ToListAsync();

            return results;
        }

        // Génération de suggestions
        private async Task<List<string>> GenerateSuggestionsAsync(string query)
        {
            var suggestions = new List<string>();
            query = query.ToLower().Trim();

            // Suggestions basées sur les noms de clients
            var clientSuggestions = await _context.Clients
                .Where(c => !c.EstArchive && (
                    c.Nom.ToLower().StartsWith(query) ||
                    c.Prenom.ToLower().StartsWith(query) ||
                    c.CodeClient.ToLower().StartsWith(query)))
                .Select(c => new[] { c.Nom, c.Prenom, c.CodeClient, c.Nom + " " + c.Prenom })
                .Take(5)
                .ToListAsync();

            suggestions.AddRange(clientSuggestions.SelectMany(x => x).Where(s => s.ToLower().Contains(query)).Distinct());

            // Suggestions basées sur les villes
            var villeSuggestions = await _context.Clients
                .Where(c => !c.EstArchive && c.Ville.ToLower().StartsWith(query))
                .Select(c => c.Ville)
                .Distinct()
                .Take(3)
                .ToListAsync();

            suggestions.AddRange(villeSuggestions);

            // Suggestions basées sur les tags
            var tagSuggestions = await _context.ClientTags
                .Where(t => t.Tag.ToLower().StartsWith(query))
                .Select(t => t.Tag)
                .Distinct()
                .Take(3)
                .ToListAsync();

            suggestions.AddRange(tagSuggestions);

            return suggestions.Distinct().Take(10).ToList();
        }

        public async Task<SearchSuggestionsDto> GetSuggestionsAsync(string query)
        {
            var result = new SearchSuggestionsDto
            {
                Suggestions = await GenerateSuggestionsAsync(query),
                RecentSearches = await GetSearchHistoryAsync(_currentUser.UserId, 5),
                PopularSearches = await GetPopularSearchesAsync(10),
                TrendingTags = await GetPopularTagsAsync(10)
            };

            return result;
        }

        public async Task<List<SearchHistoryDto>> GetSearchHistoryAsync(int userId, int limit = 20)
        {
            return await _context.SearchHistories
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.SearchDate)
                .Take(limit)
                .Select(h => new SearchHistoryDto
                {
                    Id = h.Id,
                    SearchQuery = h.SearchQuery,
                    SearchType = h.SearchType,
                    ResultsCount = h.ResultsCount,
                    SearchDate = h.SearchDate,
                    ExecutionTimeMs = h.ExecutionTimeMs
                })
                .ToListAsync();
        }

        public async Task ClearSearchHistoryAsync(int userId)
        {
            var history = await _context.SearchHistories
                .Where(h => h.UserId == userId)
                .ToListAsync();

            _context.SearchHistories.RemoveRange(history);
            await _context.SaveChangesAsync();
        }

        private async Task SaveSearchHistoryAsync(AdvancedSearchRequestDto request, int resultsCount, int executionTimeMs)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return;

            var history = new SearchHistory
            {
                UserId = _currentUser.UserId,
                SearchQuery = request.Query,
                SearchType = "Advanced",
                ResultsCount = resultsCount,
                ExecutionTimeMs = executionTimeMs,
                Filters = JsonSerializer.Serialize(new
                {
                    request.Mode,
                    request.Categories,
                    request.ClientFilters,
                    request.DocumentFilters,
                    request.Tags
                })
            };

            _context.SearchHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        private async Task<List<string>> GetPopularSearchesAsync(int limit)
        {
            return await _context.SearchHistories
                .Where(h => h.SearchDate >= DateTime.UtcNow.AddDays(-30))
                .GroupBy(h => h.SearchQuery.ToLower())
                .OrderByDescending(g => g.Count())
                .Take(limit)
                .Select(g => g.Key)
                .ToListAsync();
        }

        // Gestion des tags pour les clients
        public async Task<TagDto> AddClientTagAsync(int clientId, string tag)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null)
                throw new KeyNotFoundException("Client introuvable.");

            // Vérifier si le tag existe déjà
            var existingTag = await _context.ClientTags
                .FirstOrDefaultAsync(t => t.IdClient == clientId && t.Tag.ToLower() == tag.ToLower());

            if (existingTag != null)
                throw new InvalidOperationException("Ce tag existe déjà pour ce client.");

            var clientTag = new ClientTag
            {
                IdClient = clientId,
                Tag = tag.Trim(),
                CreatedBy = _currentUser.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ClientTags.Add(clientTag);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(_currentUser.UserId);

            return new TagDto
            {
                Id = clientTag.Id,
                Tag = clientTag.Tag,
                CreatedAt = clientTag.CreatedAt,
                CreatedByUsername = user?.Username ?? "Unknown"
            };
        }

        public async Task<TagDto> AddDocumentTagAsync(int documentId, string tag)
        {
            var document = await _context.DocumentClients.FindAsync(documentId);
            if (document == null)
                throw new KeyNotFoundException("Document introuvable.");

            var existingTag = await _context.DocumentTags
                .FirstOrDefaultAsync(t => t.IdDocument == documentId && t.Tag.ToLower() == tag.ToLower());

            if (existingTag != null)
                throw new InvalidOperationException("Ce tag existe déjà pour ce document.");

            var docTag = new DocumentTag
            {
                IdDocument = documentId,
                Tag = tag.Trim(),
                CreatedBy = _currentUser.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.DocumentTags.Add(docTag);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(_currentUser.UserId);

            return new TagDto
            {
                Id = docTag.Id,
                Tag = docTag.Tag,
                CreatedAt = docTag.CreatedAt,
                CreatedByUsername = user?.Username ?? "Unknown"
            };
        }

        public async Task RemoveClientTagAsync(int tagId)
        {
            var tag = await _context.ClientTags.FindAsync(tagId);
            if (tag == null)
                throw new KeyNotFoundException("Tag introuvable.");

            _context.ClientTags.Remove(tag);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveDocumentTagAsync(int tagId)
        {
            var tag = await _context.DocumentTags.FindAsync(tagId);
            if (tag == null)
                throw new KeyNotFoundException("Tag introuvable.");

            _context.DocumentTags.Remove(tag);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TagDto>> GetClientTagsAsync(int clientId)
        {
            return await _context.ClientTags
                .Include(t => t.User)
                .Where(t => t.IdClient == clientId)
                .Select(t => new TagDto
                {
                    Id = t.Id,
                    Tag = t.Tag,
                    CreatedAt = t.CreatedAt,
                    CreatedByUsername = t.User.Username
                })
                .ToListAsync();
        }

        public async Task<List<TagDto>> GetDocumentTagsAsync(int documentId)
        {
            return await _context.DocumentTags
                .Include(t => t.User)
                .Where(t => t.IdDocument == documentId)
                .Select(t => new TagDto
                {
                    Id = t.Id,
                    Tag = t.Tag,
                    CreatedAt = t.CreatedAt,
                    CreatedByUsername = t.User.Username
                })
                .ToListAsync();
        }

        public async Task<List<string>> GetPopularTagsAsync(int limit = 20)
        {
            var clientTags = await _context.ClientTags
                .GroupBy(t => t.Tag.ToLower())
                .Select(g => new { Tag = g.Key, Count = g.Count() })
                .ToListAsync();

            var documentTags = await _context.DocumentTags
                .GroupBy(t => t.Tag.ToLower())
                .Select(g => new { Tag = g.Key, Count = g.Count() })
                .ToListAsync();

            var allTags = clientTags.Concat(documentTags)
                .GroupBy(t => t.Tag)
                .Select(g => new { Tag = g.Key, Count = g.Sum(x => x.Count) })
                .OrderByDescending(t => t.Count)
                .Take(limit)
                .Select(t => t.Tag)
                .ToList();

            return allTags;
        }
    }
}

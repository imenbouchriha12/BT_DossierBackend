namespace Backend.DTO.Search
{
    /// <summary>
    /// DTO pour la recherche avancée
    /// </summary>
    public class AdvancedSearchRequestDto
    {
        /// <summary>
        /// Texte de recherche principal
        /// </summary>
        public string? Query { get; set; }

        /// <summary>
        /// Type de recherche : Exact, Fuzzy, Phonetic
        /// </summary>
        public SearchMode Mode { get; set; } = SearchMode.Fuzzy;

        /// <summary>
        /// Catégories à rechercher
        /// </summary>
        public List<SearchCategory> Categories { get; set; } = new() { SearchCategory.All };

        /// <summary>
        /// Filtres spécifiques clients
        /// </summary>
        public ClientFilters? ClientFilters { get; set; }

        /// <summary>
        /// Filtres spécifiques documents
        /// </summary>
        public DocumentFilters? DocumentFilters { get; set; }

        /// <summary>
        /// Tags à rechercher
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// Pagination
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Taille de page
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Tri
        /// </summary>
        public SortOption SortBy { get; set; } = SortOption.Relevance;

        /// <summary>
        /// Ordre du tri
        /// </summary>
        public SortDirection SortDirection { get; set; } = SortDirection.Desc;
    }

    public class ClientFilters
    {
        public List<string>? Villes { get; set; }
        public List<string>? Statuts { get; set; }
        public List<string>? SituationsFamiliales { get; set; }
        public List<string>? SecteursActivite { get; set; }
        public decimal? RevenuMin { get; set; }
        public decimal? RevenuMax { get; set; }
        public DateTime? DateCreationDebut { get; set; }
        public DateTime? DateCreationFin { get; set; }
        public int? AgeMin { get; set; }
        public int? AgeMax { get; set; }
        public string? Sexe { get; set; }
        public string? Nationalite { get; set; }
    }

    public class DocumentFilters
    {
        public List<string>? TypesDocument { get; set; }
        public List<string>? StatutsValidation { get; set; }
        public DateTime? DateAjoutDebut { get; set; }
        public DateTime? DateAjoutFin { get; set; }
        public List<string>? Extensions { get; set; }
    }

    public enum SearchMode
    {
        Exact,      // Recherche exacte
        Fuzzy,      // Recherche floue (tolérance aux fautes)
        Phonetic    // Recherche phonétique (sons similaires)
    }

    public enum SearchCategory
    {
        All,
        Clients,
        Documents,
        Users
    }

    public enum SortOption
    {
        Relevance,      // Pertinence (score)
        DateCreated,    // Date de création
        DateModified,   // Date de modification
        Name,           // Nom alphabétique
        Code            // Code client
    }

    public enum SortDirection
    {
        Asc,
        Desc
    }

    /// <summary>
    /// Résultat de recherche avancée
    /// </summary>
    public class AdvancedSearchResultDto
    {
        public List<ClientSearchResultDto> Clients { get; set; } = new();
        public List<DocumentSearchResultDto> Documents { get; set; } = new();
        public List<UserSearchResultDto> Users { get; set; } = new();

        public SearchMetadata Metadata { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
        public List<string> PopularTags { get; set; } = new();
    }

    public class ClientSearchResultDto
    {
        public int Id { get; set; }
        public string CodeClient { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string NomComplet { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public string Ville { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public decimal? RevenuMensuel { get; set; }
        public string? Profession { get; set; }
        public List<string> Tags { get; set; } = new();
        public double RelevanceScore { get; set; }
        public List<string> MatchedFields { get; set; } = new();
    }

    public class DocumentSearchResultDto
    {
        public int Id { get; set; }
        public int IdClient { get; set; }
        public string CodeClient { get; set; } = string.Empty;
        public string NomClient { get; set; } = string.Empty;
        public string TypeDocument { get; set; } = string.Empty;
        public string NomFichier { get; set; } = string.Empty;
        public string StatutValidation { get; set; } = string.Empty;
        public DateTime DateAjout { get; set; }
        public List<string> Tags { get; set; } = new();
        public double RelevanceScore { get; set; }
        public List<string> MatchedFields { get; set; } = new();
    }

    public class UserSearchResultDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Matricule { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public bool IsActive { get; set; }
        public double RelevanceScore { get; set; }
    }

    public class SearchMetadata
    {
        public int TotalResults { get; set; }
        public int TotalClients { get; set; }
        public int TotalDocuments { get; set; }
        public int TotalUsers { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int ExecutionTimeMs { get; set; }
        public string SearchQuery { get; set; } = string.Empty;
        public SearchMode SearchMode { get; set; }
    }

    /// <summary>
    /// DTO pour l'historique des recherches
    /// </summary>
    public class SearchHistoryDto
    {
        public int Id { get; set; }
        public string SearchQuery { get; set; } = string.Empty;
        public string SearchType { get; set; } = string.Empty;
        public int ResultsCount { get; set; }
        public DateTime SearchDate { get; set; }
        public int ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// DTO pour les suggestions de recherche
    /// </summary>
    public class SearchSuggestionsDto
    {
        public List<string> Suggestions { get; set; } = new();
        public List<SearchHistoryDto> RecentSearches { get; set; } = new();
        public List<string> PopularSearches { get; set; } = new();
        public List<string> TrendingTags { get; set; } = new();
    }

    /// <summary>
    /// DTO pour ajouter des tags
    /// </summary>
    public class AddTagDto
    {
        public string Tag { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO pour les tags
    /// </summary>
    public class TagDto
    {
        public int Id { get; set; }
        public string Tag { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedByUsername { get; set; } = string.Empty;
    }
}

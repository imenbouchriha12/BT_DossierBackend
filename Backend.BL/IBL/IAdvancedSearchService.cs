using Backend.DTO.Search;

namespace Backend.BL.IBL
{
    /// <summary>
    /// Interface pour le service de recherche avancée
    /// </summary>
    public interface IAdvancedSearchService
    {
        /// <summary>
        /// Effectue une recherche avancée avec tous les critères
        /// </summary>
        Task<AdvancedSearchResultDto> SearchAsync(AdvancedSearchRequestDto request);

        /// <summary>
        /// Obtient des suggestions de recherche basées sur la saisie
        /// </summary>
        Task<SearchSuggestionsDto> GetSuggestionsAsync(string query);

        /// <summary>
        /// Obtient l'historique des recherches de l'utilisateur
        /// </summary>
        Task<List<SearchHistoryDto>> GetSearchHistoryAsync(int userId, int limit = 20);

        /// <summary>
        /// Efface l'historique des recherches d'un utilisateur
        /// </summary>
        Task ClearSearchHistoryAsync(int userId);

        /// <summary>
        /// Ajoute un tag à un client
        /// </summary>
        Task<TagDto> AddClientTagAsync(int clientId, string tag);

        /// <summary>
        /// Ajoute un tag à un document
        /// </summary>
        Task<TagDto> AddDocumentTagAsync(int documentId, string tag);

        /// <summary>
        /// Supprime un tag d'un client
        /// </summary>
        Task RemoveClientTagAsync(int tagId);

        /// <summary>
        /// Supprime un tag d'un document
        /// </summary>
        Task RemoveDocumentTagAsync(int tagId);

        /// <summary>
        /// Obtient tous les tags d'un client
        /// </summary>
        Task<List<TagDto>> GetClientTagsAsync(int clientId);

        /// <summary>
        /// Obtient tous les tags d'un document
        /// </summary>
        Task<List<TagDto>> GetDocumentTagsAsync(int documentId);

        /// <summary>
        /// Obtient les tags les plus populaires
        /// </summary>
        Task<List<string>> GetPopularTagsAsync(int limit = 20);

        /// <summary>
        /// Recherche globale rapide (utilisée par SearchController)
        /// </summary>
        Task<GlobalSearchResultDto> GlobalSearchAsync(string query, int limit = 5);
    }
}

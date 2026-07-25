using Backend.BL.IBL;
using Backend.DTO.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    /// <summary>
    /// Contrôleur pour la recherche intelligente avancée
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdvancedSearchController : ControllerBase
    {
        private readonly IAdvancedSearchService _searchService;
        private readonly ICurrentUserService _currentUser;

        public AdvancedSearchController(
            IAdvancedSearchService searchService,
            ICurrentUserService currentUser)
        {
            _searchService = searchService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Recherche avancée multi-critères avec filtres intelligents
        /// </summary>
        [HttpPost("search")]
        public async Task<IActionResult> AdvancedSearch([FromBody] AdvancedSearchRequestDto request)
        {
            var result = await _searchService.SearchAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Obtenir des suggestions de recherche en temps réel
        /// </summary>
        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Ok(new SearchSuggestionsDto());

            var suggestions = await _searchService.GetSuggestionsAsync(query);
            return Ok(suggestions);
        }

        /// <summary>
        /// Obtenir l'historique des recherches de l'utilisateur connecté
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetSearchHistory([FromQuery] int limit = 20)
        {
            var history = await _searchService.GetSearchHistoryAsync(_currentUser.UserId, limit);
            return Ok(history);
        }

        /// <summary>
        /// Effacer l'historique des recherches
        /// </summary>
        [HttpDelete("history")]
        public async Task<IActionResult> ClearSearchHistory()
        {
            await _searchService.ClearSearchHistoryAsync(_currentUser.UserId);
            return Ok(new { message = "Historique de recherche effacé avec succès." });
        }

        /// <summary>
        /// Ajouter un tag à un client
        /// </summary>
        [HttpPost("clients/{clientId}/tags")]
        [Authorize(Roles = "Admin,Agent,Superviseur")]
        public async Task<IActionResult> AddClientTag(int clientId, [FromBody] AddTagDto dto)
        {
            try
            {
                var tag = await _searchService.AddClientTagAsync(clientId, dto.Tag);
                return Ok(tag);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Supprimer un tag d'un client
        /// </summary>
        [HttpDelete("clients/tags/{tagId}")]
        [Authorize(Roles = "Admin,Agent,Superviseur")]
        public async Task<IActionResult> RemoveClientTag(int tagId)
        {
            try
            {
                await _searchService.RemoveClientTagAsync(tagId);
                return Ok(new { message = "Tag supprimé avec succès." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtenir tous les tags d'un client
        /// </summary>
        [HttpGet("clients/{clientId}/tags")]
        public async Task<IActionResult> GetClientTags(int clientId)
        {
            var tags = await _searchService.GetClientTagsAsync(clientId);
            return Ok(tags);
        }

        /// <summary>
        /// Ajouter un tag à un document
        /// </summary>
        [HttpPost("documents/{documentId}/tags")]
        [Authorize(Roles = "Admin,Agent,Superviseur")]
        public async Task<IActionResult> AddDocumentTag(int documentId, [FromBody] AddTagDto dto)
        {
            try
            {
                var tag = await _searchService.AddDocumentTagAsync(documentId, dto.Tag);
                return Ok(tag);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Supprimer un tag d'un document
        /// </summary>
        [HttpDelete("documents/tags/{tagId}")]
        [Authorize(Roles = "Admin,Agent,Superviseur")]
        public async Task<IActionResult> RemoveDocumentTag(int tagId)
        {
            try
            {
                await _searchService.RemoveDocumentTagAsync(tagId);
                return Ok(new { message = "Tag supprimé avec succès." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtenir tous les tags d'un document
        /// </summary>
        [HttpGet("documents/{documentId}/tags")]
        public async Task<IActionResult> GetDocumentTags(int documentId)
        {
            var tags = await _searchService.GetDocumentTagsAsync(documentId);
            return Ok(tags);
        }

        /// <summary>
        /// Obtenir les tags les plus populaires
        /// </summary>
        [HttpGet("tags/popular")]
        public async Task<IActionResult> GetPopularTags([FromQuery] int limit = 20)
        {
            var tags = await _searchService.GetPopularTagsAsync(limit);
            return Ok(tags);
        }
    }
}

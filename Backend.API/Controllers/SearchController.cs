using Backend.BL.IBL;
using Backend.DTO.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SearchController : ControllerBase
    {
        private readonly IAdvancedSearchService _searchService;

        public SearchController(IAdvancedSearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpGet]
        public async Task<IActionResult> GlobalSearch([FromQuery] string q, [FromQuery] int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Ok(new GlobalSearchResultDto());

            var results = await _searchService.GlobalSearchAsync(q, limit);
            return Ok(results);
        }
    }
}

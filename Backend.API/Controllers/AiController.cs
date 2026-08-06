using Backend.BL.IBL;
using Backend.DTO.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AiController : ControllerBase
    {
        private readonly IAiAssistantService _aiService;

        public AiController(IAiAssistantService aiService)
        {
            _aiService = aiService;
        }

        /// Conversation en langage naturel avec l'assistant IA (type RAG)
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequestDto request)
        {
            var reponse = await _aiService.ChatAsync(request);
            return Ok(reponse);
        }

        /// Résumé automatique d'un dossier client (5 lignes)
        [HttpGet("clients/{id}/resume")]
        public async Task<IActionResult> ResumeClient(int id)
        {
            var resume = await _aiService.ResumerClientAsync(id);
            return Ok(resume);
        }
    }
}

using Backend.BL.IBL;
using Backend.DTO.Portal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    /// <summary>
    /// Portail self-service client (endpoints non-authentifiés via token sécurisé)
    /// + gestion admin des liens portail.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ClientPortalController : ControllerBase
    {
        private readonly IClientPortalService _portalService;
        private readonly ILogger<ClientPortalController> _logger;

        public ClientPortalController(
            IClientPortalService portalService,
            ILogger<ClientPortalController> logger)
        {
            _portalService = portalService;
            _logger        = logger;
        }

        // ── Admin endpoints ────────────────────────────────────────────────────

        /// POST api/clientportal/{clientId}/invite
        /// Génère un lien portail sécurisé et l'envoie par email au client.
        [HttpPost("{clientId:int}/invite")]
        [Authorize(Roles = "Admin,Agent,Superviseur")]
        public async Task<IActionResult> SendInvite(int clientId, [FromBody] SendPortalInviteDto dto)
        {
            try
            {
                var result = await _portalService.GenerateAndSendPortalLinkAsync(clientId, dto.DureeJours);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// GET api/clientportal/{clientId}/token-info
        /// Retourne les informations sur le token actif du client.
        [HttpGet("{clientId:int}/token-info")]
        [Authorize(Roles = "Admin,Agent,Superviseur")]
        public async Task<IActionResult> GetTokenInfo(int clientId)
        {
            var info = await _portalService.GetActiveTokenInfoAsync(clientId);
            if (info == null)
                return Ok(new { message = "Aucun lien portail actif pour ce client.", hasActiveToken = false });
            return Ok(new { hasActiveToken = true, token = info });
        }

        /// DELETE api/clientportal/{clientId}/revoke
        /// Révoque tous les tokens actifs du client.
        [HttpDelete("{clientId:int}/revoke")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> RevokeAccess(int clientId)
        {
            try
            {
                await _portalService.RevokePortalAccessAsync(clientId);
                return Ok(new { message = "Accès portail révoqué avec succès." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// GET api/clientportal/completion
        /// Taux de complétion documentaire de tous les clients (dashboard admin).
        [HttpGet("completion")]
        [Authorize(Roles = "Admin,Superviseur")]
        public async Task<IActionResult> GetCompletionSummary(
            [FromQuery] string? typeClient = null,
            [FromQuery] string? ville = null)
        {
            var summary = await _portalService.GetCompletionSummaryAsync(typeClient, ville);
            return Ok(summary);
        }

        // ── Public portal endpoints (token-based, no JWT) ─────────────────────

        /// GET api/clientportal/portal/{token}
        /// Vue complète du portail : liste des documents requis avec statut de chaque upload.
        [HttpGet("portal/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPortalView(string token)
        {
            try
            {
                var ip   = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var view = await _portalService.GetPortalViewAsync(token, ip);
                return Ok(view);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// POST api/clientportal/portal/{token}/upload
        /// Upload d'un document depuis le portail client (sans compte utilisateur).
        [HttpPost("portal/{token}/upload")]
        [AllowAnonymous]
        [RequestSizeLimit(50 * 1024 * 1024)] // 50 Mo max global
        public async Task<IActionResult> UploadDocument(string token, [FromForm] string typeDocument, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Fichier requis." });

            var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return BadRequest(new { message = $"Format '{ext}' non autorisé." });

            if (string.IsNullOrWhiteSpace(typeDocument))
                return BadRequest(new { message = "Le type de document est requis." });

            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);

                var result = await _portalService.UploadDocumentAsync(
                    token, typeDocument, file.FileName, ms.ToArray(), ip);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// GET api/clientportal/portal/{token}/validate
        /// Vérifie si un token est valide (utilisé par le front pour la page d'accueil du portail).
        [HttpGet("portal/{token}/validate")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateToken(string token)
        {
            var ip     = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            // Validation sans incrémenter le compteur d'accès — on lit juste le token
            var pToken = await _portalService.GetActiveTokenInfoAsync(-1); // ne pas utiliser cette surcharge
            // Appel direct validate
            var result = await _portalService.ValidateTokenAsync(token, ip);
            if (!result.IsValid)
                return Unauthorized(new { isValid = false, raison = result.RaisonInvalidite });
            return Ok(new { isValid = true });
        }
    }
}

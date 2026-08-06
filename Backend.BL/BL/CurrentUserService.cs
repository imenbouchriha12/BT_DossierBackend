using System.Security.Claims;
using Backend.BL.IBL;
using Microsoft.AspNetCore.Http;

namespace Backend.BL.BL
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int UserId
        {
            get
            {
                var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
                if (claim == null || !int.TryParse(claim.Value, out var id))
                    throw new UnauthorizedAccessException("Utilisateur non authentifié.");
                return id;
            }
        }

        public string UserName
        {
            get
            {
                var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name);
                return claim?.Value ?? "Système";
            }
        }

        public bool IsAdmin
        {
            get
            {
                var roles = _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role) ?? Enumerable.Empty<Claim>();
                return roles.Any(r => r.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}

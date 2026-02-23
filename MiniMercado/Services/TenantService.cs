using System.Security.Claims;

namespace MiniMercado.Services
{
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? GetTenantId()
        {
            var claim = _httpContextAccessor.HttpContext?
                .User
                .FindFirst("TenantId");

            if (claim == null)
                return null;

            return int.Parse(claim.Value);
        }
    }
}
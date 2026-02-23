using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MiniMercado.Data;
using MiniMercado.Models;
using System.Security.Claims;

namespace MiniMercado.Services
{
    public class CustomClaimsPrincipalFactory
        : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        private readonly MiniMercadoContext _context;

        public CustomClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor,
            MiniMercadoContext context)
            : base(userManager, roleManager, optionsAccessor)
        {
            _context = context;
        }


        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            if (user.TenantId.HasValue)
            {
                identity.AddClaim(new Claim("TenantId", user.TenantId.Value.ToString()));
            }

            return identity;
        }


    }
}

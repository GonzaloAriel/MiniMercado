using Microsoft.AspNetCore.Identity;
using MiniMercado.Data;
using MiniMercado.Models;

namespace MiniMercado.Middleware
{
    public class TenantValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context,
     UserManager<ApplicationUser> userManager,
     MiniMercadoContext dbContext,
     SignInManager<ApplicationUser> signInManager)
        {
            var path = context.Request.Path.Value;

            // 🔹 Ignorar rutas públicas
            if (path != null &&
                (path.StartsWith("/Identity") ||
                 path.StartsWith("/Tenant/Desactivado") ||
                 path.StartsWith("/lib") ||
                 path.StartsWith("/css") ||
                 path.StartsWith("/js")))
            {
                await _next(context);
                return;
            }

            if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                var user = await userManager.GetUserAsync(context.User);

                if (user != null && user.TenantId.HasValue)
                {
                    var tenant = await dbContext.Tenants.FindAsync(user.TenantId.Value);

                    if (tenant == null || !tenant.Activo)
                    {
                        await signInManager.SignOutAsync();
                        context.Response.Redirect("/Tenant/Desactivado");
                        return;
                    }
                }
            }

            await _next(context);
        }


    }
}

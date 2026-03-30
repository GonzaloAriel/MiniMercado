using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Services;

namespace MiniMercado.Middleware
{
    public class TenantValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;

        public TenantValidationMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context,
                                     UserManager<ApplicationUser> userManager,
                                     MiniMercadoContext dbContext,
                                     SignInManager<ApplicationUser> signInManager,
                                     INotificacionService notificacionService,
                                     IVencimientoService vencimientoService)
        {
            var path = context.Request.Path.Value;

            // 🔹 Ignorar rutas públicas y recursos estáticos
            if (path != null &&
                (path.StartsWith("/Identity") ||
                 path.StartsWith("/Account/Login") ||
                 path.StartsWith("/Tenant/Desactivado") ||
                 path.StartsWith("/Cuenta/Bloqueado") ||
                 path.StartsWith("/lib") ||
                 path.StartsWith("/css") ||
                 path.StartsWith("/js") ||
                 path.StartsWith("/images")))
            {
                await _next(context);
                return;
            }

            if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                var user = await userManager.GetUserAsync(context.User);

                if (user != null && user.TenantId.HasValue)
                {
                    int tenantId = user.TenantId.Value;
                    string cacheKey = $"tenant_{tenantId}";

                    // 🎯 Intentar obtener el tenant desde cache
                    if (!_cache.TryGetValue(cacheKey, out Tenant tenant))
                    {
                        // 🔹 Si NO existe, consultar DB
                        tenant = await dbContext.Tenants.FindAsync(tenantId);

                        if (tenant != null)
                        {
                            // 🔹 Guardar en cache por 10 minutos
                            var cacheOptions = new MemoryCacheEntryOptions()
                                .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30)); // Máximo 30 min sin refrescar

                            _cache.Set(cacheKey, tenant, cacheOptions);
                        }
                    }

                    if (tenant != null)
                    {
                        // 1. Validar si el tenant existe y está activo
                        if (!tenant.Activo)
                        {
                            _cache.Remove(cacheKey); // Limpiar cache por las dudas
                            await signInManager.SignOutAsync();
                            context.Response.Redirect("/Tenant/Desactivado");
                            return;
                        }

                        // 2. Validar bloqueo o vencimiento
                        bool estaVencido = tenant.FechaVencimiento.HasValue && tenant.FechaVencimiento.Value < DateTime.Now;

                        if (tenant.Bloqueado || estaVencido)
                        {
                            context.Response.Redirect("/Cuenta/Bloqueado");
                            return;
                        }

                        // 🔔 3. Notificación de vencimiento próximo (Ej: 7 días antes)
                        if (tenant.FechaVencimiento.HasValue)
                        {
                            var diasRestantes = (tenant.FechaVencimiento.Value - DateTime.Now).TotalDays;
                            if (diasRestantes > 0 && diasRestantes <= 7)
                            {
                                string notifCacheKey = $"notif_pago_{tenantId}";
                                if (!_cache.TryGetValue(notifCacheKey, out _))
                                {
                                    await notificacionService.CrearNotificacion(
                                        "Suscripción por vencer",
                                        $"Tu suscripción vence en {Math.Ceiling(diasRestantes)} días. Evita cortes en el servicio.",
                                        "Pago"
                                    );

                                    // Evitar spamear: recordar solo una vez cada 24hs
                                    _cache.Set(notifCacheKey, true, TimeSpan.FromHours(24));
                                }
                            }
                        }
                    }
                }

                // 🔔 Verificar vencimientos de productos del tenant actual
                await vencimientoService.VerificarProductosVencidos();
            }

            await _next(context);
        }
    }
}
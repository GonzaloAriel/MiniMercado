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
                                     AlertasService alertasService)
        {
            var path = context.Request.Path.Value;

            // 🔹 Ignorar rutas públicas y recursos estáticos
            if (path != null &&
                (path.StartsWith("/Identity", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("/Tenant/Desactivado", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("/Cuenta/Bloqueado", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("/Home/Error", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
                 path.StartsWith("/images", StringComparison.OrdinalIgnoreCase)))
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

                        // 2. Validar bloqueo (Manual o por Vencimiento tras 5 días de gracia)
                        bool bloqueadoManualmente = tenant.Bloqueado;
                        bool bloqueadoPorVencimiento = false;

                        if (tenant.FechaVencimiento.HasValue)
                        {
                            var diasVencido = (DateTime.Now.Date - tenant.FechaVencimiento.Value.Date).Days;
                            if (diasVencido > 5)
                            {
                                bloqueadoPorVencimiento = true;
                            }
                        }

                        if (bloqueadoManualmente || bloqueadoPorVencimiento)
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

                    // 🔔 Verificar alertas del tenant actual
                    await alertasService.VerificarYCrearNotificaciones(tenantId);
                }
            }

            await _next(context);
        }
    }
}
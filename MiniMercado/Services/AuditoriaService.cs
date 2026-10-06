using MiniMercado.Data;
using MiniMercado.Models;
using Microsoft.AspNetCore.Identity;

namespace MiniMercado.Services
{
    public class AuditoriaService
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;
        private readonly IHttpContextAccessor _http;

        public AuditoriaService(
            MiniMercadoContext context,
            ITenantService tenantService,
            IHttpContextAccessor http)
        {
            _context = context;
            _tenantService = tenantService;
            _http = http;
        }

        public void Registrar(string accion, string tabla, int? registroId, string descripcion, int? tenantIdManual = null)
        {
            var usuarioId = _http.HttpContext.User?.Identity?.Name;

            int tenantId;
            if (tenantIdManual.HasValue)
            {
                tenantId = tenantIdManual.Value;
            }
            else if (!_tenantService.TryGetTenantId(out tenantId))
            {
                throw new InvalidOperationException(
                    "AuditoriaService.Registrar: sin TenantId y sin tenantIdManual. " +
                    "La auditoria no puede registrarse sin tenant (P-007).");
            }

            var log = new Auditoria
            {
                TenantId = tenantId,
                UsuarioId = usuarioId ?? "Sistema",
                Accion = accion,
                Tabla = tabla,
                RegistroId = registroId,
                Descripcion = descripcion,
                Fecha = DateTime.Now
            };

            _context.Auditorias.Add(log);
            _context.SaveChanges();
        }
    }
}
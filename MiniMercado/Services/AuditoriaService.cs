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
            var tenantId = tenantIdManual ?? _tenantService.GetTenantId().Value;

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
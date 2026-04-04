using Microsoft.AspNetCore.Mvc;
using MiniMercado.Data;
using MiniMercado.Services;
using System.Security.Claims;

namespace MiniMercado.ViewComponents
{
    public class CajaStatusViewComponent : ViewComponent
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;

        public CajaStatusViewComponent(MiniMercadoContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!User.Identity.IsAuthenticated) return Content(string.Empty);

            // Obtención segura del TenantId
            var tenantIdResult = _tenantService.GetTenantId();
            
            // Si es AdminGlobal (null), no mostramos el estado de caja
            if (!tenantIdResult.HasValue) return Content(string.Empty);

            var tenantId = tenantIdResult.Value;
            var userId = ((ClaimsPrincipal)User).FindFirstValue(ClaimTypes.NameIdentifier);

            var config = _context.Configuraciones.FirstOrDefault(c => c.TenantId == tenantId);
            if (config == null || !config.UsarCaja) return Content(string.Empty);

            var caja = _context.Cajas.FirstOrDefault(c => c.TenantId == tenantId && c.UsuarioId == userId && c.Estado == "ABIERTA");

            return View(caja);
        }
    }
}

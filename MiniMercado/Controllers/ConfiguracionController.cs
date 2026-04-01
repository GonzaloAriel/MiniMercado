using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Services;

namespace MiniMercado.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ConfiguracionController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;

        public ConfiguracionController(MiniMercadoContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public IActionResult Index()
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var tenant = _context.Tenants
                .FirstOrDefault(t => t.Id == tenantId);

            if (tenant == null)
                return NotFound();

            var config = _context.Configuraciones.FirstOrDefault(c => c.TenantId == tenantId);
            if (config == null)
            {
                config = new Configuracion { TenantId = tenantId, UsarCaja = false };
                _context.Configuraciones.Add(config);
                _context.SaveChanges();
            }

            ViewBag.UsarCaja = config.UsarCaja;

            return View(tenant);
        }

        [HttpPost]
        public IActionResult Index(Tenant tenant, bool UsarCaja)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            ModelState.Remove(nameof(tenant.Activo));

            if (!ModelState.IsValid)
                return View(tenant);

            var tenantDb = _context.Tenants
                .FirstOrDefault(t => t.Id == tenantId);

            if (tenantDb == null)
                return NotFound();

            tenantDb.NombreComercio = tenant.NombreComercio;
            tenantDb.Direccion = tenant.Direccion;
            tenantDb.Telefono = tenant.Telefono;
            tenantDb.Email = tenant.Email;
            tenantDb.CUIT = tenant.CUIT;
            tenantDb.MensajeTicket = tenant.MensajeTicket;

            var config = _context.Configuraciones.FirstOrDefault(c => c.TenantId == tenantId);
            if (config == null)
            {
                config = new Configuracion { TenantId = tenantId, UsarCaja = UsarCaja };
                _context.Configuraciones.Add(config);
            }
            else
            {
                config.UsarCaja = UsarCaja;
            }

            _context.SaveChanges();

            TempData["Mensaje"] = "✅ Configuración guardada correctamente.";

            return RedirectToAction(nameof(Index));
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MiniMercado.Data;
using MiniMercado.Services;

namespace MiniMercado.Controllers
{
    public class HomeController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;

        public HomeController(MiniMercadoContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public IActionResult Index()
        {
            // Si es AdminGlobal, no cargamos estadísticas de tenant específico
            if (User.IsInRole("AdminGlobal"))
            {
                return View();
            }

            var tenantId = _tenantService.GetTenantId();
            if (tenantId == null) return View();

            // 📅 Ventas de HOY
            DateTime hoy = DateTime.Today;
            DateTime mañana = hoy.AddDays(1);

            var ventasHoy = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                     NombreVenta, UsuarioId, Usuario, EsFiado, 
                                     ClienteId, TenantId, CajaId 
                              FROM Ventas 
                              WHERE Activa = 0 AND TenantId = {0}", tenantId.Value)
                .Where(v => v.Fecha >= hoy && v.Fecha < mañana)
                .ToList();

            ViewBag.TotalHoy = ventasHoy.Sum(v => v.Total);
            ViewBag.CantidadVentasHoy = ventasHoy.Count;

            // ⚠️ Stock bajo (menos de 5, ajustable)
            ViewBag.StockBajo = _context.Productos
                .Where(p => p.Activo && p.TenantId == tenantId.Value && p.Stock <= 5)
                .OrderBy(p => p.Stock)
                .Select(p => new { p.Nombre, p.Stock })
                .ToList();

            return View();
        }

        public IActionResult Novedades()
        {
            return View();
        }
    }
}

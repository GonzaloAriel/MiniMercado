using Microsoft.AspNetCore.Mvc;
using MiniMercado.Data;

namespace MiniMercado.Controllers
{
    public class HomeController : Controller
    {
        private readonly MiniMercadoContext _context;

        public HomeController(MiniMercadoContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // 📅 Ventas de HOY
            DateTime hoy = DateTime.Today;
            DateTime mañana = hoy.AddDays(1);

            var ventasHoy = _context.Ventas
                .Where(v => v.Fecha >= hoy && v.Fecha < mañana);

            ViewBag.TotalHoy = ventasHoy.Sum(v => v.Total);
            ViewBag.CantidadVentasHoy = ventasHoy.Count();

            // ⚠️ Stock bajo (menos de 5, ajustable)
            ViewBag.StockBajo = _context.Productos
                .Where(p => p.Activo && p.Stock <= 5)
                .OrderBy(p => p.Stock)
                .ToList();

            return View();
        }
    }
}

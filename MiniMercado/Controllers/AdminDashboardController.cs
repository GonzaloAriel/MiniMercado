using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;

namespace MiniMercado.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly MiniMercadoContext _context;

        public AdminDashboardController(MiniMercadoContext context)
        {
            _context = context;
        }

        public IActionResult Index(int? anio)
        {
            int year = anio ?? DateTime.Today.Year;

            var ventasDelAnio = _context.Ventas
                .Where(v => !v.Activa && v.Fecha.Year == year)
                .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
                .ToList();

            // Totales mensuales
            var ventasPorMes = ventasDelAnio
                .GroupBy(v => v.Fecha.Month)
                .Select(g => new
                {
                    Mes = g.Key,
                    Total = g.Sum(x => x.Total),
                    CantidadVentas = g.Count()
                })
                .OrderBy(x => x.Mes)
                .ToList();

            // Productos más vendidos
            var productosTop = ventasDelAnio
                .SelectMany(v => v.Detalles)
                .GroupBy(d => d.Producto.Nombre)
                .Select(g => new
                {
                    Producto = g.Key,
                    CantidadVendida = g.Sum(x => x.Cantidad)
                })
                .OrderByDescending(x => x.CantidadVendida)
                .Take(5)
                .ToList();

            // Ventas anuladas
            var anuladas = _context.Ventas
                .Count(v => v.Activa && v.Fecha.Year == year);

            // Ticket promedio
            var ticketPromedio = ventasDelAnio.Any()
                ? ventasDelAnio.Average(v => v.Total)
                : 0;

            // Bajo stock
            var bajoStock = _context.Productos
                .Where(p => p.Activo && p.Stock <= 5)
                .OrderBy(p => p.Stock)
                .Take(5)
                .ToList();

            ViewBag.Anio = year;
            ViewBag.VentasPorMes = ventasPorMes;
            ViewBag.ProductosTop = productosTop;
            ViewBag.Anuladas = anuladas;
            ViewBag.TicketPromedio = ticketPromedio;
            ViewBag.BajoStock = bajoStock;
            ViewBag.TotalAnual = ventasDelAnio.Sum(v => v.Total);

            return View();
        }
    }
}

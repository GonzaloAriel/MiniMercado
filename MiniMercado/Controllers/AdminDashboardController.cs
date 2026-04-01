using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Services;

namespace MiniMercado.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;

        public AdminDashboardController(MiniMercadoContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public IActionResult Index(int? anio)
        {
            var tenantId = _tenantService.GetTenantId().Value;
            int year = anio ?? DateTime.Today.Year;

            var ventasRaw = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                     NombreVenta, UsuarioId, Usuario, EsFiado, 
                                     ClienteId, TenantId, CajaId 
                              FROM Ventas 
                              WHERE TenantId = {0}", tenantId);

            var ventasDelAnio = ventasRaw
                .Where(v => !v.Activa && v.Fecha.Year == year)
                .ToList();

            // Cargar detalles manualmente
            foreach (var v in ventasDelAnio)
            {
                v.Detalles = _context.VentaDetalles
                    .Where(d => d.VentaId == v.Id && d.TenantId == tenantId)
                    .Include(d => d.Producto)
                    .ToList();
            }

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
            var anuladas = ventasRaw
                .Count(v => v.Activa && v.Fecha.Year == year);

            // Ticket promedio
            var ticketPromedio = ventasDelAnio.Any()
                ? ventasDelAnio.Average(v => v.Total)
                : 0;

            // Bajo stock
            var bajoStock = _context.Productos
                .Where(p => p.Activo && p.TenantId == tenantId && p.Stock <= 5)
                .OrderBy(p => p.Stock)
                .Take(5)
                .ToList();

            // --- STOCK INTELIGENTE (LOTES) ---
            var hoy = DateTime.Now;
            var limiteProximo = hoy.AddDays(3);

            var lotesVencidos = _context.LotesProducto
                .Include(l => l.Producto)
                .Where(l => l.TenantId == tenantId && l.Cantidad > 0 && l.FechaVencimiento != null && l.FechaVencimiento < hoy)
                .GroupBy(l => new { l.ProductoId, l.Producto.Nombre })
                .Select(g => new
                {
                    Nombre = g.Key.Nombre,
                    Cantidad = g.Sum(x => x.Cantidad),
                    Fecha = g.Min(x => x.FechaVencimiento).Value
                })
                .OrderBy(x => x.Fecha)
                .Take(5)
                .ToList();

            var lotesPorVencer = _context.LotesProducto
                .Include(l => l.Producto)
                .Where(l => l.TenantId == tenantId && l.Cantidad > 0 && l.FechaVencimiento != null && l.FechaVencimiento >= hoy && l.FechaVencimiento <= limiteProximo)
                .GroupBy(l => new { l.ProductoId, l.Producto.Nombre })
                .Select(g => new
                {
                    Nombre = g.Key.Nombre,
                    Cantidad = g.Sum(x => x.Cantidad),
                    Fecha = g.Min(x => x.FechaVencimiento).Value
                })
                .OrderBy(x => x.Fecha)
                .Take(5)
                .ToList();

            ViewBag.Anio = year;
            ViewBag.VentasPorMes = ventasPorMes;
            ViewBag.ProductosTop = productosTop;
            ViewBag.Anuladas = anuladas;
            ViewBag.TicketPromedio = ticketPromedio;
            ViewBag.BajoStock = bajoStock;
            ViewBag.TotalAnual = ventasDelAnio.Sum(v => v.Total);

            ViewBag.ProductosVencidos = lotesVencidos;
            ViewBag.ProductosPorVencer = lotesPorVencer;

            return View();
        }
    }
}

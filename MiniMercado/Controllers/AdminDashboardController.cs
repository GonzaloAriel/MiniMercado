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
        private readonly AlertasService _alertasService;

        public AdminDashboardController(MiniMercadoContext context, ITenantService tenantService, AlertasService alertasService)
        {
            _context = context;
            _tenantService = tenantService;
            _alertasService = alertasService;
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
            var bajoStock = _alertasService.GetStockBajo(tenantId);
            var negativos = _alertasService.GetStockNegativo(tenantId);
            var vencimientos = _alertasService.GetVencimientos(tenantId);

            ViewBag.StockBajoCount = bajoStock.Count;
            ViewBag.NegativosCount = negativos.Count;
            ViewBag.VencimientosCount = vencimientos.Count;

            // --- STOCK INTELIGENTE (LOTES) ---
            var lotesVencidos = _alertasService.GetProductosVencidos(tenantId);
            var lotesPorVencer = _alertasService.GetProductosPorVencer(tenantId);

            ViewBag.Anio = year;
            ViewBag.VentasPorMes = ventasPorMes;
            ViewBag.ProductosTop = productosTop;
            ViewBag.Anuladas = anuladas;
            ViewBag.TicketPromedio = ticketPromedio;
            ViewBag.BajoStock = bajoStock;
            ViewBag.TotalAnual = ventasDelAnio.Sum(v => v.Total);

            ViewBag.ProductosVencidos = lotesVencidos;
            ViewBag.ProductosPorVencer = lotesPorVencer;

            // --- PRODUCTOS SIN ROTACIÓN (LOTES + VENTAS RECIENTES) ---
            var fechaLimite = DateTime.Today.AddMonths(-6);

            // 1. Obtener IDs de productos vendidos en los últimos 6 meses
            var productosVendidosRecientesIds = _context.Ventas
                .Where(v => v.TenantId == tenantId && !v.Activa && v.Fecha >= fechaLimite)
                .SelectMany(v => v.Detalles)
                .Select(d => d.ProductoId)
                .Distinct()
                .ToList();

            // 2. Obtener stock real desde lotes (agrupado por producto)
            var productosConStockReal = _context.LotesProducto
                .Where(l => l.TenantId == tenantId && l.Cantidad > 0)
                .GroupBy(l => l.ProductoId)
                .Select(g => new
                {
                    ProductoId = g.Key,
                    StockTotal = g.Sum(x => x.Cantidad)
                })
                .ToList();

            // 3. Filtrar productos que tienen stock pero NO han tenido ventas recientes
            var productosSinRotacionIds = productosConStockReal
                .Where(p => !productosVendidosRecientesIds.Contains(p.ProductoId))
                .OrderByDescending(p => p.StockTotal)
                .Take(5)
                .Select(p => p.ProductoId)
                .ToList();

            // 4. Obtener los datos completos de los productos para la vista
            var productosSinRotacion = _context.Productos
                .Where(p => productosSinRotacionIds.Contains(p.Id))
                .Include(p => p.ProductoBase) // Cargar nombres si usa catálogo global
                .ToList();

            ViewBag.ProductosSinRotacion = productosSinRotacion;

            // --- DATOS DE PAGOS (RESUMEN ANUAL) ---
            var pagosAnuales = _context.Pagos
                .Where(p => p.TenantId == tenantId && p.Fecha.Year == year)
                .ToList();

            ViewBag.TotalEfectivo = pagosAnuales.Where(p => p.Metodo == "EFECTIVO").Sum(p => p.Monto);
            ViewBag.TotalDigital = pagosAnuales.Where(p => p.Metodo == "MP" || p.Metodo == "TARJETA").Sum(p => p.Monto);
            ViewBag.TotalVuelto = pagosAnuales.Where(p => p.Metodo == "EFECTIVO").Sum(p => p.Vuelto ?? 0);

            return View();
        }

        public IActionResult Pagos(int? anio)
        {
            var tenantId = _tenantService.GetTenantId().Value;
            int year = anio ?? DateTime.Today.Year;

            var pagosQuery = _context.Pagos
                .Where(p => p.TenantId == tenantId && p.Fecha.Year == year);

            var resumenPagos = pagosQuery
                .GroupBy(p => p.Metodo)
                .Select(g => new
                {
                    Metodo = g.Key,
                    Total = g.Sum(p => p.Monto),
                    Cantidad = g.Count(),
                    TicketPromedio = g.Average(p => p.Monto)
                })
                .ToList();

            ViewBag.Anio = year;
            ViewBag.ResumenPagos = resumenPagos;
            ViewBag.TotalVuelto = pagosQuery.Where(p => p.Metodo == "EFECTIVO").Sum(p => p.Vuelto ?? 0);
            ViewBag.TotalGeneral = resumenPagos.Sum(r => r.Total);

            // --- PAGOS POR DÍA (PARA GRÁFICO DE LÍNEAS) ---
            var pagosPorDia = pagosQuery
                .GroupBy(p => p.Fecha.Date)
                .Select(g => new
                {
                    Fecha = g.Key,
                    Total = g.Sum(x => x.Monto)
                })
                .OrderBy(x => x.Fecha)
                .ToList();

            ViewBag.PagosPorDia = pagosPorDia;

            return View();
        }
    }
}

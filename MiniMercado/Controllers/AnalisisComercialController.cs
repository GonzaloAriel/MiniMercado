using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Services;
using System;
using System.Linq;

namespace MiniMercado.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AnalisisComercialController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;

        public AnalisisComercialController(MiniMercadoContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        private (DateTime desde, DateTime hasta) GetDateRange(string filtro, DateTime? desde, DateTime? hasta)
        {
            var hoy = DateTime.Today;
            switch (filtro)
            {
                case "Hoy": return (hoy, hoy.AddDays(1));
                case "Año": return (new DateTime(hoy.Year, 1, 1), new DateTime(hoy.Year, 12, 31).AddDays(1));
                case "Personalizado": return (desde ?? hoy, hasta?.AddDays(1) ?? hoy.AddDays(1));
                default: // Mes
                    var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
                    return (inicioMes, inicioMes.AddMonths(1));
            }
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult VentasPorRubro(string filtro = "Mes", DateTime? desde = null, DateTime? hasta = null, string busqueda = "", int pagina = 1)
        {
            var tenantId = _tenantService.GetTenantId();
            if (!tenantId.HasValue) return Unauthorized();

            var (fechaDesde, fechaHasta) = GetDateRange(filtro, desde, hasta);

            var ventas = _context.VentaDetalles
                .Include(d => d.Producto)
                .ThenInclude(p => p.Rubro)
                .Include(d => d.Venta)
                .Where(d => d.TenantId == tenantId.Value && 
                            !d.Venta.Activa && 
                            d.Venta.Fecha >= fechaDesde && 
                            d.Venta.Fecha < fechaHasta)
                .ToList();

            var totalFacturacion = ventas.Sum(v => v.Cantidad * v.PrecioUnitario);

            var query = ventas.GroupBy(v => v.Producto.Rubro != null ? v.Producto.Rubro.Nombre : "Sin Rubro")
                .Select(g => new VentasPorRubroViewModel
                {
                    RubroNombre = g.Key,
                    FacturacionTotal = g.Sum(v => v.Cantidad * v.PrecioUnitario),
                    UnidadesVendidas = g.Sum(v => v.Cantidad),
                    CantidadVentas = g.Select(v => v.VentaId).Distinct().Count(),
                    ParticipacionPorcentual = totalFacturacion > 0 ? (g.Sum(v => v.Cantidad * v.PrecioUnitario) / totalFacturacion) * 100 : 0
                })
                .OrderByDescending(r => r.FacturacionTotal)
                .AsQueryable();

            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(r => r.RubroNombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase));
            }

            var totalRegistros = query.Count();
            int pageSize = 15;
            var report = query.Skip((pagina - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Filtro = filtro;
            ViewBag.Desde = fechaDesde.ToString("yyyy-MM-dd");
            ViewBag.Hasta = fechaHasta.AddDays(-1).ToString("yyyy-MM-dd");
            ViewBag.Busqueda = busqueda;
            ViewBag.Pagina = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            return View(report);
        }

        public IActionResult ProductosMasVendidos(string filtro = "Mes", DateTime? desde = null, DateTime? hasta = null, string busqueda = "", int pagina = 1)
        {
            var tenantId = _tenantService.GetTenantId();
            if (!tenantId.HasValue) return Unauthorized();

            var (fechaDesde, fechaHasta) = GetDateRange(filtro, desde, hasta);

            var ventas = _context.VentaDetalles
                .Include(d => d.Producto)
                .ThenInclude(p => p.Rubro)
                .Include(d => d.Venta)
                .Where(d => d.TenantId == tenantId.Value && 
                            !d.Venta.Activa && 
                            d.Venta.Fecha >= fechaDesde && 
                            d.Venta.Fecha < fechaHasta)
                .ToList();

            var totalFacturacion = ventas.Sum(v => v.Cantidad * v.PrecioUnitario);

            var query = ventas.GroupBy(v => new { v.ProductoId, v.Producto.Nombre, RubroNombre = v.Producto.Rubro != null ? v.Producto.Rubro.Nombre : "Sin Rubro" })
                .Select(g => new ProductosMasVendidosViewModel
                {
                    Nombre = g.Key.Nombre,
                    Rubro = g.Key.RubroNombre,
                    CantidadTotalVendida = g.Sum(d => d.Cantidad),
                    FacturacionGenerada = g.Sum(d => d.Cantidad * d.PrecioUnitario),
                    PrecioPromedio = g.Sum(d => d.Cantidad) > 0 ? g.Sum(d => d.Cantidad * d.PrecioUnitario) / g.Sum(d => d.Cantidad) : 0,
                    CantidadDeVentas = g.Select(d => d.VentaId).Distinct().Count(),
                    ParticipacionPorcentual = totalFacturacion > 0 ? (g.Sum(d => d.Cantidad * d.PrecioUnitario) / totalFacturacion) * 100 : 0
                })
                .OrderByDescending(r => r.FacturacionGenerada)
                .AsQueryable();

            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(r => r.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase));
            }

            var totalRegistros = query.Count();
            int pageSize = 15;
            var report = query.Skip((pagina - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Filtro = filtro;
            ViewBag.Desde = fechaDesde.ToString("yyyy-MM-dd");
            ViewBag.Hasta = fechaHasta.AddDays(-1).ToString("yyyy-MM-dd");
            ViewBag.Busqueda = busqueda;
            ViewBag.Pagina = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            return View(report);
        }

        public IActionResult ProductosSinRotacion(string filtro = "Mes", DateTime? desde = null, DateTime? hasta = null, string busqueda = "", int pagina = 1)
        {
            var tenantId = _tenantService.GetTenantId();
            if (!tenantId.HasValue) return Unauthorized();

            var (fechaDesde, fechaHasta) = GetDateRange(filtro, desde, hasta);

            // Productos con stock > 0
            var productos = _context.Productos
                .Include(p => p.Rubro)
                .Where(p => p.TenantId == tenantId.Value && p.Stock > 0)
                .ToList();

            // Ventas en el período (para exclusión)
            var idsVendidosEnPeriodo = _context.VentaDetalles
                .Where(vd => vd.TenantId == tenantId.Value && !vd.Venta.Activa && vd.Venta.Fecha >= fechaDesde && vd.Venta.Fecha < fechaHasta)
                .Select(vd => vd.ProductoId)
                .Distinct()
                .ToList();

            // Última venta de cada producto
            var ultimasVentas = _context.VentaDetalles
                .Where(vd => vd.TenantId == tenantId.Value && !vd.Venta.Activa && vd.ProductoId.HasValue)
                .GroupBy(vd => vd.ProductoId.Value)
                .Select(g => new { ProductoId = g.Key, LastSale = g.Max(x => x.Venta.Fecha) })
                .ToDictionary(x => x.ProductoId, x => x.LastSale);

            var query = productos
                .Where(p => !idsVendidosEnPeriodo.Contains(p.Id))
                .Select(p => {
                    var ultimaVenta = ultimasVentas.ContainsKey(p.Id) ? (DateTime?)ultimasVentas[p.Id] : null;
                    var diasSinVender = ultimaVenta.HasValue ? (DateTime.Today - ultimaVenta.Value).Days : 999999;
                    return new ProductosSinRotacionViewModel
                    {
                        Nombre = p.Nombre,
                        Rubro = p.Rubro?.Nombre ?? "Sin Rubro",
                        Stock = p.Stock,
                        UltimaVenta = ultimaVenta,
                        DiasSinVender = diasSinVender,
                        Costo = p.Precio, // Proxy de costo
                        ValorStock = p.Stock * p.Precio
                    };
                })
                .OrderByDescending(r => r.DiasSinVender)
                .AsQueryable();

            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(r => r.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase));
            }

            var totalRegistros = query.Count();
            int pageSize = 15;
            var report = query.Skip((pagina - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Filtro = filtro;
            ViewBag.Desde = fechaDesde.ToString("yyyy-MM-dd");
            ViewBag.Hasta = fechaHasta.AddDays(-1).ToString("yyyy-MM-dd");
            ViewBag.Busqueda = busqueda;
            ViewBag.Pagina = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            return View(report);
        }

        public IActionResult Pagos(string filtro = "Mes", DateTime? desde = null, DateTime? hasta = null, string busqueda = "", int pagina = 1)
        {
            var tenantId = _tenantService.GetTenantId();
            if (!tenantId.HasValue) return Unauthorized();

            var (fechaDesde, fechaHasta) = GetDateRange(filtro, desde, hasta);

            // Filtrar pagos de ventas válidas
            var pagos = _context.Pagos
                .Include(p => p.Venta)
                .Where(p => p.TenantId == tenantId.Value && 
                            !p.Venta.Activa && 
                            p.Venta.Fecha >= fechaDesde && 
                            p.Venta.Fecha < fechaHasta)
                .ToList();

            var totalRecaudado = pagos.Sum(p => p.Monto);

            var query = pagos.GroupBy(p => p.Metodo)
                .Select(g => new PagosViewModel
                {
                    Metodo = g.Key,
                    CantidadOperaciones = g.Count(),
                    MontoTotal = g.Sum(p => p.Monto),
                    TicketPromedio = g.Any() ? g.Sum(p => p.Monto) / g.Count() : 0,
                    ParticipacionPorcentual = totalRecaudado > 0 ? (g.Sum(p => p.Monto) / totalRecaudado) * 100 : 0
                })
                .OrderByDescending(r => r.MontoTotal)
                .AsQueryable();

            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(r => r.Metodo.Contains(busqueda, StringComparison.OrdinalIgnoreCase));
            }

            var totalRegistros = query.Count();
            int pageSize = 15;
            var report = query.Skip((pagina - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Filtro = filtro;
            ViewBag.Desde = fechaDesde.ToString("yyyy-MM-dd");
            ViewBag.Hasta = fechaHasta.AddDays(-1).ToString("yyyy-MM-dd");
            ViewBag.Busqueda = busqueda;
            ViewBag.Pagina = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            return View(report);
        }

        public IActionResult EvolucionVentas(string filtro = "Mes", DateTime? desde = null, DateTime? hasta = null)
        {
            var tenantId = _tenantService.GetTenantId();
            if (!tenantId.HasValue) return Unauthorized();

            var (fechaDesde, fechaHasta) = GetDateRange(filtro, desde, hasta);

            var ventas = _context.Ventas
                .Where(v => v.TenantId == tenantId.Value && 
                            !v.Activa && 
                            v.Fecha >= fechaDesde && 
                            v.Fecha < fechaHasta)
                .ToList();

            var report = ventas.GroupBy(v =>
            {
                switch (filtro)
                {
                    case "Hoy": return new { Val = v.Fecha.Hour, Date = v.Fecha.Date.AddHours(v.Fecha.Hour) };
                    case "Año": return new { Val = v.Fecha.Month, Date = new DateTime(v.Fecha.Year, v.Fecha.Month, 1) };
                    default: return new { Val = v.Fecha.DayOfYear, Date = v.Fecha.Date }; // Mes o Personalizado
                }
            })
            .Select(g => new EvolucionVentasViewModel
            {
                Periodo = filtro == "Hoy" ? $"{g.Key.Val}:00" : 
                          filtro == "Año" ? System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Val) : 
                          g.Key.Date.ToString("dd/MM"),
                Facturacion = g.Sum(v => v.Total),
                CantidadVentas = g.Count(),
                TicketPromedio = g.Average(v => v.Total),
                FechaOrden = g.Key.Date
            })
            .OrderBy(r => r.FechaOrden)
            .ToList();

            ViewBag.Filtro = filtro;
            ViewBag.Desde = fechaDesde.ToString("yyyy-MM-dd");
            ViewBag.Hasta = fechaHasta.AddDays(-1).ToString("yyyy-MM-dd");

            return View(report);
        }
    }
}

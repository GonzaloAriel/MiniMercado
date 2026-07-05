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

        public IActionResult ProductosSinRotacion()
        {
            return View();
        }

        public IActionResult Pagos()
        {
            return View();
        }

        public IActionResult EvolucionVentas()
        {
            return View();
        }
    }
}

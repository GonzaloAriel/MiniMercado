using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.ViewModels;
using MiniMercado.Models;

namespace MiniMercado.Controllers
{
    public class DashboardController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;

        public DashboardController(MiniMercadoContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public IActionResult AuditoriaCajas(DateTime? desde, DateTime? hasta)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var fechaDesde = desde ?? DateTime.Today;
            var fechaHasta = hasta ?? DateTime.Today.AddDays(1);

            var model = new DashboardCajasViewModel
            {
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta
            };

            model.ResumenCajas = _context.Ventas
                .Where(v => v.Fecha >= fechaDesde && v.Fecha <= fechaHasta && v.TenantId == tenantId) 
                .GroupBy(v => new { v.Usuario, Fecha = v.Fecha.Date })
                .Select(g => new ResumenCajaDia
                {
                    Usuario = g.Key.Usuario,
                    Fecha = g.Key.Fecha,
                    CantidadVentas = g.Count(x => !x.Activa), 
                    CantidadAnuladas = g.Count(x => x.Activa), 
                    TotalVendido = g.Where(x => !x.Activa).Sum(x => x.Total) 
                })
                .OrderByDescending(x => x.Fecha)
                .ToList();

            model.HistorialVentas = _context.Ventas
                .Where(v => v.Fecha >= fechaDesde && v.Fecha <= fechaHasta && v.TenantId == tenantId) 
                .OrderByDescending(v => v.Fecha)
                .Select(v => new VentaHistorialItem
                {
                    VentaId = v.Id,
                    Fecha = v.Fecha,
                    Usuario = v.Usuario,
                    Total = v.Total,
                    Activa = v.Activa
                })
                .ToList();

            
            model.Auditorias = _context.Auditorias
                .Where(a => a.Fecha >= fechaDesde && a.Fecha <= fechaHasta && a.TenantId == tenantId) 
                .OrderByDescending(a => a.Fecha)
                .Take(100)
                .ToList();

            return View(model);
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.ViewModels;
using MiniMercado.Models;

namespace MiniMercado.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;

        public DashboardController(MiniMercadoContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public async Task<IActionResult> AuditoriaCajas(DateTime? desde, DateTime? hasta, int pageResumen = 1, int pageVentas = 1, int pageAuditoria = 1)
        {
            if (pageResumen < 1) pageResumen = 1;
            if (pageVentas < 1) pageVentas = 1;
            if (pageAuditoria < 1) pageAuditoria = 1;

            var tenantIdNullable = _tenantService.GetTenantId();
            if (!tenantIdNullable.HasValue) return Forbid();
            var tenantId = tenantIdNullable.Value;
            int pageSize = 10;

            var fechaDesde = desde ?? DateTime.Today;
            var fechaHasta = hasta ?? DateTime.Today.AddDays(1);

            var model = new DashboardCajasViewModel
            {
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta
            };

            var ventasRaw = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                     NombreVenta, UsuarioId, Usuario, EsFiado, 
                                     ClienteId, TenantId, CajaId 
                              FROM Ventas 
                              WHERE TenantId = {0}", tenantId);

            var queryResumen = ventasRaw
                .Where(v => v.Fecha >= fechaDesde && v.Fecha <= fechaHasta) 
                .GroupBy(v => new { v.Usuario, Fecha = v.Fecha.Date })
                .Select(g => new ResumenCajaDia
                {
                    Usuario = g.Key.Usuario,
                    Fecha = g.Key.Fecha,
                    CantidadVentas = g.Count(x => !x.Activa), 
                    CantidadAnuladas = g.Count(x => x.Activa), 
                    TotalVendido = g.Where(x => !x.Activa).Sum(x => x.Total) 
                });

            var totalResumen = await queryResumen.CountAsync();
            model.ResumenCajas = await queryResumen
                .OrderByDescending(x => x.Fecha)
                .Skip((pageResumen - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var queryVentas = ventasRaw
                .Where(v => v.Fecha >= fechaDesde && v.Fecha <= fechaHasta) 
                .OrderByDescending(v => v.Fecha)
                .Select(v => new VentaHistorialItem
                {
                    VentaId = v.Id,
                    Fecha = v.Fecha,
                    Usuario = v.Usuario,
                    Total = v.Total,
                    Activa = v.Activa
                });

            var totalVentas = await queryVentas.CountAsync();
            model.HistorialVentas = await queryVentas
                .Skip((pageVentas - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var queryAuditoria = _context.Auditorias
                .Where(a => a.Fecha >= fechaDesde && a.Fecha <= fechaHasta && a.TenantId == tenantId);

            var totalAuditoria = await queryAuditoria.CountAsync();
            model.Auditorias = await queryAuditoria
                .OrderByDescending(a => a.Fecha)
                .Skip((pageAuditoria - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PageResumen = pageResumen;
            ViewBag.TotalResumen = (int)Math.Ceiling(totalResumen / (double)pageSize);
            ViewBag.PageVentas = pageVentas;
            ViewBag.TotalVentas = (int)Math.Ceiling(totalVentas / (double)pageSize);
            ViewBag.PageAuditoria = pageAuditoria;
            ViewBag.TotalAuditoria = (int)Math.Ceiling(totalAuditoria / (double)pageSize);
            ViewBag.Desde = fechaDesde.ToString("yyyy-MM-dd");
            ViewBag.Hasta = fechaHasta.ToString("yyyy-MM-dd");

            return View(model);
        }
    }
}
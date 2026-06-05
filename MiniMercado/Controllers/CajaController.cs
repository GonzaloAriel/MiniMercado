using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Services;
using System.Security.Claims;

namespace MiniMercado.Controllers
{
    [Authorize]
    public class CajaController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;

        public CajaController(MiniMercadoContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public IActionResult AbrirCaja()
        {
            var tenantId = _tenantService.GetTenantId().Value;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cajaAbierta = _context.Cajas.FirstOrDefault(c => c.TenantId == tenantId && c.UsuarioId == userId && c.Estado == "ABIERTA");
            if (cajaAbierta != null)
            {
                return RedirectToAction("Create", "Venta");
            }

            ViewBag.ErrorCaja = TempData["ErrorCaja"];
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AbrirCaja(decimal montoInicial)
        {
            var tenantId = _tenantService.GetTenantId().Value;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var caja = new Caja
            {
                TenantId = tenantId,
                UsuarioId = userId,
                FechaApertura = DateTime.Now,
                MontoInicial = montoInicial,
                TotalVentas = 0,
                Estado = "ABIERTA"
            };

            _context.Cajas.Add(caja);
            await _context.SaveChangesAsync();

            return RedirectToAction("Create", "Venta");
        }

        public IActionResult CerrarCaja()
        {
            var tenantId = _tenantService.GetTenantId().Value;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var caja = _context.Cajas.FirstOrDefault(c => c.TenantId == tenantId && c.UsuarioId == userId && c.Estado == "ABIERTA");
            if (caja == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.TotalEgresos = _context.EgresosCaja
                .Where(e => e.CajaId == caja.Id && e.TenantId == tenantId)
                .Sum(e => (decimal?)e.Monto) ?? 0;

            return View(caja);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarCaja(int id, decimal montoReal)
        {
            var tenantId = _tenantService.GetTenantId().Value;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var caja = _context.Cajas.FirstOrDefault(c => c.Id == id && c.TenantId == tenantId && c.UsuarioId == userId);
            if (caja == null || caja.Estado != "ABIERTA")
            {
                return BadRequest();
            }

            var totalEgresos = await _context.EgresosCaja
                .Where(e => e.CajaId == id && e.TenantId == tenantId)
                .SumAsync(e => (decimal?)e.Monto) ?? 0;

            decimal saldoEsperado = caja.MontoInicial + caja.TotalVentas - totalEgresos;

            caja.MontoReal = montoReal;
            caja.Diferencia = montoReal - saldoEsperado;
            caja.Estado = "CERRADA";
            caja.FechaCierre = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction("ResumenCierre", new { id = caja.Id });
        }

        public IActionResult ResumenCierre(int id)
        {
            var tenantId = _tenantService.GetTenantId().Value;
            var caja = _context.Cajas.FirstOrDefault(c => c.Id == id && c.TenantId == tenantId);
            if (caja == null) return NotFound();

            ViewBag.TotalEgresos = _context.EgresosCaja
                .Where(e => e.CajaId == id && e.TenantId == tenantId)
                .Sum(e => (decimal?)e.Monto) ?? 0;

            // Calcular ventas fuera de caja realizadas por este usuario durante esta sesión de caja
            var fechaHasta = caja.FechaCierre ?? DateTime.Now;
            
            var ventasFuera = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                     NombreVenta, UsuarioId, Usuario, EsFiado, 
                                     ClienteId, TenantId, CajaId 
                              FROM Ventas 
                              WHERE TenantId = {0} AND UsuarioId = {1} 
                                AND CajaId IS NULL AND Activa = 0 
                                AND Fecha >= {2} AND Fecha <= {3}", 
                              tenantId, caja.UsuarioId, caja.FechaApertura, fechaHasta)
                .ToList();

            ViewBag.VentasFueraCantidad = ventasFuera.Count;
            ViewBag.VentasFueraTotal = ventasFuera.Sum(v => v.Total);

            return View(caja);
        }

        public async Task<IActionResult> Historial(DateTime? desde, DateTime? hasta, int page = 1)
        {
            if (page < 1) page = 1;
            var tenantId = _tenantService.GetTenantId().Value;
            int pageSize = 10;
            
            var fechaDesde = desde ?? DateTime.Today.AddDays(-7);
            var fechaHasta = hasta ?? DateTime.Today.AddDays(1);

            var query = _context.Cajas
                .Where(c => c.TenantId == tenantId && c.FechaApertura >= fechaDesde && c.FechaApertura <= fechaHasta)
                .AsQueryable();

            var totalRegistros = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            var cajas = await query
                .OrderByDescending(c => c.FechaApertura)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Desde = fechaDesde;
            ViewBag.Hasta = fechaHasta;
            ViewBag.PaginaActual = page;
            ViewBag.TotalPaginas = totalPaginas;

            return View(cajas);
        }
    }
}

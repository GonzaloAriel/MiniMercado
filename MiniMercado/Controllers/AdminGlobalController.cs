using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Models.ViewModels;
using MiniMercado.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MiniMercado.Controllers
{
    [Authorize(Roles = "AdminGlobal")]
    public class AdminGlobalController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly IMemoryCache _cache;
        private readonly ISuscripcionService _suscripcionService;

        public AdminGlobalController(MiniMercadoContext context, IMemoryCache cache, ISuscripcionService suscripcionService)
        {
            _context = context;
            _cache = cache;
            _suscripcionService = suscripcionService;
        }

        // Dashboard global de ingresos y estados
        public async Task<IActionResult> Dashboard()
        {
            var hoy = DateTime.Now;

            var model = new AdminDashboardViewModel
            {
                TotalRecaudado = await _context.CobrosTenants.AsNoTracking().SumAsync(c => c.Monto),
                
                // Un tenant es "Activo" si Activo == true y no está bloqueado ni vencido
                TenantsActivos = await _context.Tenants.AsNoTracking()
                    .CountAsync(t => t.Activo && !t.Bloqueado && (!t.FechaProximoPago.HasValue || t.FechaProximoPago > hoy)),

                // Un tenant es "Vencido" si tiene fecha de vencimiento y es anterior a hoy
                TenantsVencidos = await _context.Tenants.AsNoTracking()
                    .CountAsync(t => t.FechaProximoPago.HasValue && t.FechaProximoPago < hoy),

                TenantsBloqueados = await _context.Tenants.AsNoTracking().CountAsync(t => t.Bloqueado),
                TotalTenants = await _context.Tenants.AsNoTracking().CountAsync()
            };

            return View(model);
        }

        // Listado de todos los tenants para el administrador global
        public async Task<IActionResult> Tenants(string search, bool ocultarDesactivados, int page = 1)
        {
            if (page < 1) page = 1;
            int pageSize = 10;

            ViewBag.CurrentSearch = search;
            ViewBag.OcultarDesactivados = ocultarDesactivados;

            var query = _context.Tenants.AsNoTracking().AsQueryable();

            // Filtro por nombre
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => t.NombreComercio.Contains(search));
            }

            // Filtro: Ocultar bloqueados
            if (ocultarDesactivados)
            {
                query = query.Where(t => !t.Bloqueado);
            }

            var totalRegistros = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            var tenants = await query
                .OrderBy(t => t.NombreComercio)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TenantAdminViewModel
                {
                    Id = t.Id,
                    NombreComercio = t.NombreComercio,
                    Plan = t.Plan,
                    FechaVencimiento = t.FechaProximoPago ?? t.FechaVencimiento,
                    Bloqueado = t.Bloqueado,
                    Activo = t.Activo,
                    UltimoPago = t.UltimoPagoRegistrado,
                    TotalPagado = t.Cobros.Sum(c => c.Monto),
                    EstadoSuscripcion = t.EstadoSuscripcion.ToString()
                })
                .ToListAsync();

            ViewBag.PaginaActual = page;
            ViewBag.TotalPaginas = totalPaginas;

            return View(tenants);
        }

        // GET: AdminGlobal/RegistrarPago/id
        [HttpGet]
        public async Task<IActionResult> RegistrarPago(int id)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
            {
                return NotFound();
            }

            var model = new RegistrarPagoViewModel
            {
                TenantId = tenant.Id,
                NombreComercio = tenant.NombreComercio,
                PrecioSugerido = tenant.PrecioMensual,
                Monto = tenant.PrecioMensual, // Valor sugerido inicial
                Plan = tenant.Plan
            };

            return View(model);
        }

        // POST: AdminGlobal/RegistrarPago
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarPago(RegistrarPagoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var tenant = await _context.Tenants.FindAsync(model.TenantId);
            if (tenant == null)
            {
                return NotFound();
            }

            // Usar el servicio para toda la lógica de negocio
            await _suscripcionService.RegistrarPagoAsync(
                model.TenantId, 
                model.Meses ?? 1, 
                model.Plan ?? "Mensual", 
                model.Monto,
                model.Observacion);

            // 🎯 Invalidar cache para que el middleware refleje el cambio
            _cache.Remove($"tenant_{tenant.Id}");

            TempData["Success"] = $"Pago registrado con éxito para {tenant.NombreComercio}.";

            return RedirectToAction(nameof(Tenants));
        }

        // POST: AdminGlobal/ToggleBloqueo/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBloqueo(int id)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null) return NotFound();

            tenant.Bloqueado = !tenant.Bloqueado;
            _context.Update(tenant);
            await _context.SaveChangesAsync();

            // 🎯 Invalidar cache
            _cache.Remove($"tenant_{id}");

            TempData["Success"] = $"Estado de bloqueo actualizado para {tenant.NombreComercio}.";
            return RedirectToAction(nameof(Tenants));
        }

        // POST: AdminGlobal/ToggleActivo/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null) return NotFound();

            tenant.Activo = !tenant.Activo;
            _context.Update(tenant);
            await _context.SaveChangesAsync();

            // 🎯 Invalidar cache
            _cache.Remove($"tenant_{id}");

            string mensaje = tenant.Activo ? "activado (visible)" : "desactivado (oculto)";
            TempData["Success"] = $"El comercio {tenant.NombreComercio} ha sido {mensaje}.";
            return RedirectToAction(nameof(Tenants));
        }
    }
}

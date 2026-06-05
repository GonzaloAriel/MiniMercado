using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Services;
using System.Security.Claims;

namespace MiniMercado.Controllers
{
    [Authorize]
    public class EgresosController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;
        private readonly AuditoriaService _auditoria;

        public EgresosController(MiniMercadoContext context, 
                                 ITenantService tenantService,
                                 AuditoriaService auditoria)
        {
            _context = context;
            _tenantService = tenantService;
            _auditoria = auditoria;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            if (page < 1) page = 1;
            var tenantId = _tenantService.GetTenantId().Value;
            int pageSize = 15;

            var query = _context.EgresosCaja
                .Include(e => e.Usuario)
                .Include(e => e.Proveedor)
                .Where(e => e.TenantId == tenantId)
                .OrderByDescending(e => e.Fecha);

            var totalRegistros = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            var egresos = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PaginaActual = page;
            ViewBag.TotalPaginas = totalPaginas;

            return View(egresos);
        }

        public async Task<IActionResult> Create()
        {
            var tenantId = _tenantService.GetTenantId().Value;

            ViewBag.Categorias = Enum.GetValues(typeof(CategoriaEgreso))
                .Cast<CategoriaEgreso>()
                .Select(c => new SelectListItem
                {
                    Text = c.ToString(),
                    Value = ((int)c).ToString()
                }).ToList();

            ViewBag.Proveedores = await _context.Proveedores
                .Where(p => p.TenantId == tenantId && p.Activo)
                .OrderBy(p => p.Nombre)
                .Select(p => new SelectListItem
                {
                    Text = p.Nombre,
                    Value = p.Id.ToString()
                }).ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EgresoCaja egreso)
        {
            var tenantId = _tenantService.GetTenantId().Value;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Buscar caja abierta
            var cajaAbierta = await _context.Cajas
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.UsuarioId == userId && c.Estado == "ABIERTA");

            egreso.TenantId = tenantId;
            egreso.UsuarioId = userId;
            egreso.Fecha = DateTime.Now;
            egreso.CajaId = cajaAbierta?.Id;

            ModelState.Remove(nameof(egreso.TenantId));
            ModelState.Remove(nameof(egreso.UsuarioId));

            if (ModelState.IsValid)
            {
                _context.EgresosCaja.Add(egreso);
                await _context.SaveChangesAsync();

                _auditoria.Registrar("Crear", "EgresosCaja", egreso.Id, $"Egreso registrado: {egreso.Monto} - {egreso.Categoria}");

                TempData["Success"] = "Egreso registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categorias = Enum.GetValues(typeof(CategoriaEgreso))
                .Cast<CategoriaEgreso>()
                .Select(c => new SelectListItem
                {
                    Text = c.ToString(),
                    Value = ((int)c).ToString()
                }).ToList();

            ViewBag.Proveedores = await _context.Proveedores
                .Where(p => p.TenantId == tenantId && p.Activo)
                .OrderBy(p => p.Nombre)
                .Select(p => new SelectListItem
                {
                    Text = p.Nombre,
                    Value = p.Id.ToString()
                }).ToListAsync();

            return View(egreso);
        }
    }
}

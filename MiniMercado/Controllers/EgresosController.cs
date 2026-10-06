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
    [Authorize(Roles = "Admin,Caja")]
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

        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta, CategoriaEgreso? categoria, int? proveedorId, string? buscar, int page = 1)
        {
            if (page < 1) page = 1;
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            int pageSize = 15;

            var query = _context.EgresosCaja
                .Include(e => e.Usuario)
                .Include(e => e.Proveedor)
                .Include(e => e.Caja)
                .Where(e => e.TenantId == tenantId && !e.Anulado);

            // Filtros
            if (desde.HasValue)
            {
                query = query.Where(e => e.Fecha >= desde.Value);
            }
            if (hasta.HasValue)
            {
                var hastaFecha = hasta.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(e => e.Fecha <= hastaFecha);
            }
            if (categoria.HasValue)
            {
                query = query.Where(e => e.Categoria == categoria.Value);
            }
            if (proveedorId.HasValue)
            {
                query = query.Where(e => e.ProveedorId == proveedorId.Value);
            }
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim();
                query = query.Where(e => e.Descripcion.Contains(buscar) || (e.Beneficiario != null && e.Beneficiario.Contains(buscar)));
            }

            query = query.OrderByDescending(e => e.Fecha);

            var totalRegistros = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            var egresos = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PaginaActual = page;
            ViewBag.TotalPaginas = totalPaginas;

            // Preservar filtros para la vista
            ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
            ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");
            ViewBag.Categoria = categoria;
            ViewBag.ProveedorId = proveedorId;
            ViewBag.Buscar = buscar;

            // Listas para los dropdowns de filtros
            ViewBag.Categorias = Enum.GetValues(typeof(CategoriaEgreso))
                .Cast<CategoriaEgreso>()
                .Select(c => new SelectListItem
                {
                    Text = c.ToString(),
                    Value = ((int)c).ToString(),
                    Selected = categoria.HasValue && (int)c == (int)categoria.Value
                }).ToList();

            ViewBag.Proveedores = await _context.Proveedores
                .Where(p => p.TenantId == tenantId && p.Activo)
                .OrderBy(p => p.Nombre)
                .Select(p => new SelectListItem
                {
                    Text = p.Nombre,
                    Value = p.Id.ToString(),
                    Selected = proveedorId.HasValue && p.Id == proveedorId.Value
                }).ToListAsync();

            return View(egresos);
        }

        public async Task<IActionResult> Create()
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();

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
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Buscar caja abierta
            var cajaAbierta = await _context.Cajas
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.UsuarioId == userId && c.Estado == "ABIERTA");

            egreso.TenantId = tenantId;
            egreso.UsuarioId = userId;
            egreso.Fecha = DateTime.Now;
            egreso.CajaId = cajaAbierta?.Id;
            egreso.Anulado = false;

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

        public async Task<IActionResult> Edit(int id)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            var egreso = await _context.EgresosCaja
                .Include(e => e.Caja)
                .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

            if (egreso == null) return NotFound();

            if (!EsEditable(egreso))
            {
                TempData["Error"] = "Este egreso está bloqueado y no puede editarse.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categorias = Enum.GetValues(typeof(CategoriaEgreso))
                .Cast<CategoriaEgreso>()
                .Select(c => new SelectListItem
                {
                    Text = c.ToString(),
                    Value = ((int)c).ToString(),
                    Selected = (int)c == (int)egreso.Categoria
                }).ToList();

            ViewBag.Proveedores = await _context.Proveedores
                .Where(p => p.TenantId == tenantId && p.Activo)
                .OrderBy(p => p.Nombre)
                .Select(p => new SelectListItem
                {
                    Text = p.Nombre,
                    Value = p.Id.ToString(),
                    Selected = p.Id == egreso.ProveedorId
                }).ToListAsync();

            return View(egreso);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EgresoCaja model)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            var egreso = await _context.EgresosCaja
                .Include(e => e.Caja)
                .FirstOrDefaultAsync(e => e.Id == model.Id && e.TenantId == tenantId);

            if (egreso == null) return NotFound();

            if (!EsEditable(egreso))
            {
                TempData["Error"] = "Este egreso está bloqueado y no puede editarse.";
                return RedirectToAction(nameof(Index));
            }

            // Remociones de validación para campos que no se editan
            ModelState.Remove(nameof(model.TenantId));
            ModelState.Remove(nameof(model.UsuarioId));

            if (ModelState.IsValid)
            {
                egreso.Monto = model.Monto;
                egreso.Categoria = model.Categoria;
                egreso.Descripcion = model.Descripcion;
                egreso.Beneficiario = model.Beneficiario;
                egreso.ProveedorId = model.ProveedorId;
                egreso.Observaciones = model.Observaciones;

                await _context.SaveChangesAsync();

                _auditoria.Registrar("Editar", "EgresosCaja", egreso.Id, $"Egreso editado: {egreso.Monto} - {egreso.Categoria}");

                TempData["Success"] = "Egreso actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categorias = Enum.GetValues(typeof(CategoriaEgreso))
                .Cast<CategoriaEgreso>()
                .Select(c => new SelectListItem
                {
                    Text = c.ToString(),
                    Value = ((int)c).ToString(),
                    Selected = (int)c == (int)model.Categoria
                }).ToList();

            ViewBag.Proveedores = await _context.Proveedores
                .Where(p => p.TenantId == tenantId && p.Activo)
                .OrderBy(p => p.Nombre)
                .Select(p => new SelectListItem
                {
                    Text = p.Nombre,
                    Value = p.Id.ToString(),
                    Selected = p.Id == model.ProveedorId
                }).ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Anular(int id)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            var egreso = await _context.EgresosCaja
                .Include(e => e.Caja)
                .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

            if (egreso == null) return NotFound();

            if (!EsEditable(egreso))
            {
                TempData["Error"] = "Este egreso está bloqueado y no puede anularse.";
                return RedirectToAction(nameof(Index));
            }

            egreso.Anulado = true;
            await _context.SaveChangesAsync();

            _auditoria.Registrar("Anular", "EgresosCaja", egreso.Id, $"Egreso anulado: {egreso.Monto} - {egreso.Categoria}");

            TempData["Success"] = "Egreso anulado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private bool EsEditable(EgresoCaja egreso)
        {
            if (egreso.Anulado) return false;

            if (egreso.CajaId.HasValue)
            {
                // TIPO A: Egreso Operativo
                return egreso.Caja != null && egreso.Caja.Estado == "ABIERTA";
            }
            else
            {
                // TIPO B: Egreso Administrativo
                return (DateTime.Now - egreso.Fecha).TotalHours <= 48;
            }
        }
    }
}

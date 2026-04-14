using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Services;

namespace MiniMercado.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RubroController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;
        private readonly AuditoriaService _auditoria;

        public RubroController(MiniMercadoContext context, ITenantService tenantService, AuditoriaService auditoria)
        {
            _context = context;
            _tenantService = tenantService;
            _auditoria = auditoria;
        }

        // INDEX
        [Authorize(Roles = "AdminGlobal, Admin")]
        public async Task<IActionResult> Index(int page = 1)
        {
            if (page < 1) page = 1;
            var tenantId = _tenantService.GetTenantId().Value;
            int pageSize = 10;

            var query = _context.Rubros
                .Where(r => r.Activo && r.TenantId == tenantId)
                .Include(r => r.Productos)
                .AsQueryable();

            var totalRegistros = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            var rubros = await query
                .OrderBy(r => r.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PaginaActual = page;
            ViewBag.TotalPaginas = totalPaginas;

            return View(rubros);
        }

        // CREATE GET
        [Authorize(Roles = "AdminGlobal, Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // CREATE POST
        [HttpPost]
        [Authorize(Roles = "AdminGlobal, Admin")]
        public IActionResult Create(Rubro rubro)
        {
            if (!ModelState.IsValid)
                return View(rubro);

            var tenantId = _tenantService.GetTenantId();
            if (!tenantId.HasValue)
                return Forbid();

            rubro.TenantId = tenantId.Value;
            rubro.Activo = true;

            _context.Rubros.Add(rubro);
            _context.SaveChanges();

            _auditoria.Registrar("Crear", "Rubros", rubro.Id, "Se creó un rubro");

            return RedirectToAction(nameof(Index));
        }

        // EDIT GET
        [Authorize(Roles = "AdminGlobal, Admin")]
        public IActionResult Edit(int id)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var rubro = _context.Rubros
                .FirstOrDefault(r => r.Id == id && r.TenantId == tenantId);

            if (rubro == null)
                return NotFound();

            return View(rubro);
        }

        // EDIT POST
        [HttpPost]
        [Authorize(Roles = "AdminGlobal, Admin")]
        public IActionResult Edit(Rubro rubro)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            if (!ModelState.IsValid)
                return View(rubro);

            var rubroDb = _context.Rubros
                .FirstOrDefault(r => r.Id == rubro.Id && r.TenantId == tenantId);

            if (rubroDb == null)
                return NotFound();

            rubroDb.Nombre = rubro.Nombre;
            _context.SaveChanges();

            _auditoria.Registrar("Modificar", "Rubros", rubro.Id, "Se modificó un rubro");

            return RedirectToAction(nameof(Index));
        }

        // DELETE GET
        [Authorize(Roles = "AdminGlobal, Admin")]
        public IActionResult Delete(int id)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var rubro = _context.Rubros
                .FirstOrDefault(r => r.Id == id && r.Activo && r.TenantId == tenantId);

            if (rubro == null)
                return NotFound();

            return View(rubro);
        }

        // DELETE AJAX
        [HttpPost]
        [Authorize(Roles = "AdminGlobal, Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult DesactivarAjax(int id)
        {
            var tenantId = _tenantService.GetTenantId().Value;
            var rubro = _context.Rubros.FirstOrDefault(r => r.Id == id && r.TenantId == tenantId);

            if (rubro == null) return Json(new { ok = false, error = "Rubro no encontrado" });

            // Podríamos validar si tiene productos asociados aquí, pero el requerimiento es desactivar.
            rubro.Activo = false;
            _context.SaveChanges();

            _auditoria.Registrar("Eliminar", "Rubros", rubro.Id, $"Desactivación AJAX: {rubro.Nombre}");

            return Json(new { ok = true });
        }

        // DELETE POST
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "AdminGlobal, Admin")]
        public IActionResult DeleteConfirmed(int id)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var rubro = _context.Rubros
                .FirstOrDefault(r => r.Id == id && r.TenantId == tenantId);

            if (rubro == null)
                return NotFound();

            rubro.Activo = false;
            _context.SaveChanges();

            _auditoria.Registrar(
                "Eliminar",
                "Rubros",
                rubro.Id,
                $"Se eliminó el rubro: {rubro.Nombre}"
            );

            TempData["Mensaje"] = "✅ Rubro desactivado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // INACTIVOS
        [Authorize(Roles = "AdminGlobal, Admin")]
        public async Task<IActionResult> Inactivos(string? filtro, int page = 1)
        {
            if (page < 1) page = 1;
            var tenantId = _tenantService.GetTenantId().Value;
            int pageSize = 10;

            var query = _context.Rubros
                .Where(r => !r.Activo && r.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filtro))
            {
                filtro = filtro.ToLower();
                query = query.Where(r => r.Nombre.ToLower().Contains(filtro));
            }

            var totalRegistros = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            var rubros = await query
                .OrderBy(r => r.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PaginaActual = page;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.Filtro = filtro;

            return View(rubros);
        }

        [HttpPost]
        [Authorize(Roles = "AdminGlobal, Admin")]
        public IActionResult CrearDesdeModal(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return BadRequest("El nombre del rubro es obligatorio");

            var tenantId = _tenantService.GetTenantId().Value;

            var existe = _context.Rubros
                .Any(r => r.Nombre == nombre && r.TenantId == tenantId && r.Activo);

            if (existe)
                return BadRequest("El rubro ya existe");

            var rubro = new Rubro
            {
                Nombre = nombre,
                TenantId = tenantId,
                Activo = true
            };

            _context.Rubros.Add(rubro);
            _context.SaveChanges();

            _auditoria.Registrar("Crear", "Rubros", rubro.Id, $"Se creó un rubro desde modal: {rubro.Nombre}");

            return Json(new { id = rubro.Id, nombre = rubro.Nombre });
        }

        [HttpPost]
        [Authorize(Roles = "AdminGlobal, Admin")]
        public IActionResult CrearRapido([FromBody] RubroRapidoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Nombre))
                return Json(new { ok = false, error = "Nombre requerido" });

            var tenantId = _tenantService.GetTenantId().Value;

            var rubro = new Rubro
            {
                Nombre = request.Nombre,
                TenantId = tenantId,
                Activo = true
            };

            _context.Rubros.Add(rubro);
            _context.SaveChanges();

            _auditoria.Registrar("Crear", "Rubros", rubro.Id,
                $"Se creó el rubro rápido: {rubro.Nombre}");

            return Json(new { ok = true, id = rubro.Id, nombre = rubro.Nombre });
        }

        [HttpPost]
        [Authorize(Roles = "AdminGlobal, Admin")]
        public IActionResult GetOrCreateByCategoria([FromBody] CategoriaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Categoria))
                return Json(new { success = false, error = "Categoría vacía" });

            var tenantId = _tenantService.GetTenantId().Value;
            var nombreNormalizado = request.Categoria.Trim();

            // Buscar rubro existente para este tenant (insensible a mayúsculas)
            var rubro = _context.Rubros
                .FirstOrDefault(r => r.Nombre.ToLower() == nombreNormalizado.ToLower() && r.TenantId == tenantId && r.Activo);

            if (rubro == null)
            {
                // Crear si no existe
                rubro = new Rubro
                {
                    Nombre = nombreNormalizado,
                    TenantId = tenantId,
                    Activo = true
                };
                _context.Rubros.Add(rubro);
                _context.SaveChanges();

                _auditoria.Registrar("Crear", "Rubros", rubro.Id, $"Rubro creado automáticamente por categoría: {rubro.Nombre}");
            }

            return Json(new { success = true, id = rubro.Id, nombre = rubro.Nombre });
        }

        public class CategoriaRequest { public string Categoria { get; set; } }

        public class RubroRapidoRequest
        {
            public string Nombre { get; set; }
        }

        // REACTIVAR
        [Authorize(Roles = "AdminGlobal, Admin")]
        public IActionResult Reactivar(int id)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var rubro = _context.Rubros
                .FirstOrDefault(r => r.Id == id && r.TenantId == tenantId);

            if (rubro == null)
                return NotFound();

            rubro.Activo = true;
            _context.SaveChanges();

            return RedirectToAction(nameof(Inactivos));
        }
    }
}
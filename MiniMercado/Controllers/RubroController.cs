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
        public IActionResult Index()
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var rubros = _context.Rubros
                .Where(r => r.Activo && r.TenantId == tenantId)
                .Include(r => r.Productos)
                .ToList();

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
        public IActionResult Inactivos(string filtro)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var rubros = _context.Rubros
                .Where(r => !r.Activo && r.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filtro))
            {
                filtro = filtro.ToLower();
                rubros = rubros.Where(r => r.Nombre.ToLower().Contains(filtro));
            }

            return View(rubros.ToList());
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
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
                .Include(r => r.Productos)
                .AsEnumerable()
                .Where(r => r.Productos == null || r.Productos.All(p => p.TenantId == tenantId));

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

            _context.Rubros.Add(rubro);
            _context.SaveChanges();

            _auditoria.Registrar("Crear", "Rubros", rubro.Id, "Se creó un rubro");

            return RedirectToAction(nameof(Index));
        }

        // EDIT GET
        [Authorize(Roles = "AdminGlobal, Admin")]
        public IActionResult Edit(int id)
        {
            var rubro = _context.Rubros.Find(id);
            if (rubro == null)
                return NotFound();
            return View(rubro);
        }

        // EDIT POST
        [HttpPost]
        [Authorize(Roles = "AdminGlobal, Admin")]
        public IActionResult Edit(Rubro rubro)
        {
            if (!ModelState.IsValid)
                return View(rubro);

            var rubroDb = _context.Rubros.Find(rubro.Id);
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
            var rubro = _context.Rubros
                .Include(r => r.Productos)
                .FirstOrDefault(r => r.Id == id);

            if (rubro == null)
                return NotFound();

            return View(rubro);
        }

        // DELETE POST
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "AdminGlobal, Admin")]
        public IActionResult DeleteConfirmed(int id)
        {
            var rubro = _context.Rubros
                .Include(r => r.Productos)
                .FirstOrDefault(r => r.Id == id);

            if (rubro == null)
                return NotFound();

            // Set RubroId to null for related products
            if (rubro.Productos != null)
            {
                foreach (var p in rubro.Productos)
                {
                    p.RubroId = null;
                }
            }

            _context.Rubros.Remove(rubro);
            _context.SaveChanges();

            _auditoria.Registrar("Eliminar", "Rubros", id, "Se eliminó un rubro");

            return RedirectToAction(nameof(Index));
        }
    }
}

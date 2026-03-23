using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Services;
using System.Linq;

namespace MiniMercado.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductosController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;
        private readonly AuditoriaService _auditoria;

        public ProductosController(MiniMercadoContext context, ITenantService tenantService, AuditoriaService auditoria)
        {
            _context = context;
            _tenantService = tenantService;
            _auditoria = auditoria;
        }

        // GET products by Rubro (filtered by current tenant)
        [HttpGet]
        [Authorize(Roles = "Caja,Admin,AdminGlobal")]
        public IActionResult GetByRubro(int rubroId)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var productos = _context.Productos
                .AsNoTracking()
                .Where(p => p.RubroId == rubroId && p.TenantId == tenantId && p.Activo)
                .Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    p.Precio,
                    p.Stock,
                    p.EsPorPeso,
                    p.PrecioEditable
                })
                .ToList();

            return Json(productos);
        }

        // LISTAR
        [Authorize(Roles = "AdminGlobal, Admin")]
        public IActionResult Index(int? rubroId, string? filtro)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var productosQuery = _context.Productos
                .Where(p => p.Activo && p.TenantId == tenantId)
                .Include(p => p.Rubro)
                .AsQueryable();

            if (rubroId.HasValue)
                productosQuery = productosQuery.Where(p => p.RubroId == rubroId.Value);

            if (!string.IsNullOrEmpty(filtro))
            {
                filtro = filtro.ToLower();
                productosQuery = productosQuery.Where(p =>
                    p.Nombre.ToLower().Contains(filtro) ||
                    p.CodigoBarra.Contains(filtro));
            }

            var rubros = _context.Rubros
                .Where(r => r.TenantId == tenantId)
                .ToList();

            var model = new ProductoIndexViewModel
            {
                Productos = productosQuery.ToList(),
                Rubros = rubros,
                FiltroRubro = rubroId
            };

            return View(model);
        }

        // CREATE GET
        [Authorize(Roles = "AdminGlobal, Admin")]
        public IActionResult Create()
        {
            var tenantId = _tenantService.GetTenantId().Value;

            ViewBag.Rubros = _context.Rubros
                .Where(r => r.TenantId == tenantId)
                .ToList();

            return View();
        }

        // CREATE POST
        [HttpPost]
        [Authorize(Roles = "AdminGlobal,Admin")]
        public IActionResult Create(Producto producto)
        {
            producto.TenantId = _tenantService.GetTenantId().Value;

            ModelState.Remove(nameof(producto.TenantId));

            if (!ModelState.IsValid)
                return View(producto);

            _context.Productos.Add(producto);
            _context.SaveChanges();

            _auditoria.Registrar
            (
              "Crear",
              "Productos",
               producto.Id,
              "Se creo un producto"
            );

            return RedirectToAction(nameof(Index));
        }

        // EDIT GET
        [Authorize(Roles = "AdminGlobal,Admin")]
        public IActionResult Edit(int id)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var producto = _context.Productos
                .FirstOrDefault(p => p.Id == id && p.TenantId == tenantId);

            if (producto == null)
                return NotFound();

            ViewBag.Rubros = _context.Rubros
                .Where(r => r.TenantId == tenantId)
                .ToList();

            return View(producto);
        }

        // EDIT POST
        [HttpPost]
        [Authorize(Roles = "AdminGlobal,Admin")]
        public IActionResult Edit(Producto producto)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            ModelState.Remove(nameof(producto.TenantId));
            ModelState.Remove(nameof(producto.Rubro));

            if (!ModelState.IsValid)
            {
                ViewBag.Rubros = _context.Rubros
                    .Where(r => r.TenantId == tenantId)
                    .ToList();
                return View(producto);
            }

            var productoDb = _context.Productos
                .FirstOrDefault(p => p.Id == producto.Id && p.TenantId == tenantId);

            if (productoDb == null)
                return NotFound();

            productoDb.Nombre = producto.Nombre;
            productoDb.Precio = producto.Precio;
            productoDb.CodigoBarra = producto.CodigoBarra;
            productoDb.Stock = producto.Stock;
            productoDb.Activo = producto.Activo;
            productoDb.PrecioEditable = producto.PrecioEditable;
            productoDb.EsPorPeso = producto.EsPorPeso;
            productoDb.RubroId = producto.RubroId;

            _context.SaveChanges();

            _auditoria.Registrar(
                "Modificar",
                "Productos",
                producto.Id,
                "Se modificó el precio o stock"
            );

            return RedirectToAction(nameof(Index));
        }

        // DELETE GET
        [Authorize(Roles = "AdminGlobal,Admin")]
        public IActionResult Delete(int id)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var producto = _context.Productos
                .FirstOrDefault(p => p.Id == id && p.Activo && p.TenantId == tenantId);

            if (producto == null)
                return NotFound();

            return View(producto);
        }

        // DELETE POST
        [HttpPost]
        [Authorize(Roles = "AdminGlobal,Admin")]
        public IActionResult DeleteConfirmed(int id)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var producto = _context.Productos
                .FirstOrDefault(p => p.Id == id && p.TenantId == tenantId);

            if (producto == null)
                return NotFound();

            producto.Activo = false;

            _context.SaveChanges();

            var NombreProducto = producto.Nombre;
            _auditoria.Registrar
           (
               "Eliminar",
               "Productos",
               producto.Id,
                $"Se eliminó el producto: {NombreProducto}"
           );

            TempData["Mensaje"] = "✅ Producto desactivado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        //INACTIVOS
        public IActionResult Inactivos(string filtro)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var productos = _context.Productos
                .Where(p => !p.Activo && p.TenantId == tenantId);

            if (!string.IsNullOrEmpty(filtro))
            {
                filtro = filtro.ToLower();
                productos = productos.Where(p =>
                    p.Nombre.ToLower().Contains(filtro) ||
                    p.CodigoBarra.Contains(filtro)
                );
            }

            return View(productos.ToList());
        }

        //REACTIVAR
        public IActionResult Reactivar(int id)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var producto = _context.Productos
                .FirstOrDefault(p => p.Id == id && p.TenantId == tenantId);

            if (producto == null)
                return NotFound();

            producto.Activo = true;
            _context.SaveChanges();

            return RedirectToAction("Inactivos");
        }

    }
}

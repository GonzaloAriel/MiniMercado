
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;


namespace MiniMercado.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductosController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;
        public ProductosController(MiniMercadoContext context,
                ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        // LISTAR
        [Authorize(Roles = "AdminGlobal, Admin")]
        public IActionResult Index(string filtro)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var productos = _context.Productos
                .Where(p => p.Activo && p.TenantId == tenantId);

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

        // CREATE GET
        [Authorize(Roles = "AdminGlobal, Admin")]
        public IActionResult Create()
        {
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

            return View(producto);
        }

        // EDIT POST
        [HttpPost]
        [Authorize(Roles = "AdminGlobal,Admin")]
        public IActionResult Edit(Producto producto)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            ModelState.Remove(nameof(producto.TenantId));

            if (!ModelState.IsValid)
                return View(producto);

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

            _context.SaveChanges();

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

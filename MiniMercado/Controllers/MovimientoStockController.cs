using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;
using System.Linq;

namespace MiniMercado.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MovimientoStockController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;

        public MovimientoStockController(
               MiniMercadoContext context,
               ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        // GET: Ingreso de stock
        public IActionResult Ingreso()
        {
            var tenantId = _tenantService.GetTenantId().Value;

            // Eliminamos el SelectList pesado de productos
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> BuscarProductos(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return Json(new List<object>());
            
            var tenantId = _tenantService.GetTenantId().Value;

            var productos = await _context.Productos
                .Where(p => p.TenantId == tenantId && p.Activo &&
                       (p.Nombre.Contains(term) || p.CodigoBarra.Contains(term)))
                .OrderBy(p => p.Nombre)
                .Take(10)
                .Select(p => new
                {
                    id = p.Id,
                    text = p.Nombre + " (Stock: " + p.Stock + ")",
                    codigo = p.CodigoBarra
                })
                .ToListAsync();

            return Json(productos);
        }


        // POST: Guardar ingreso
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Ingreso(MovimientoStock movimiento, DateTime? FechaVencimiento)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            movimiento.TenantId = tenantId;
            ModelState.Remove(nameof(movimiento.TenantId));

            if (movimiento.ProductoId == null || movimiento.ProductoId == 0)
            {
                ModelState.AddModelError("ProductoId", "Debe seleccionar un producto de la lista");
            }

            if (!ModelState.IsValid)
            {
                return View(movimiento);
            }

            movimiento.Fecha = DateTime.Now;

            var producto = _context.Productos
                .FirstOrDefault(p => p.Id == movimiento.ProductoId && p.TenantId == tenantId);

            if (producto == null)
                return BadRequest();

            // 1. Actualizar Stock Total
            producto.Stock += movimiento.Cantidad;

            // 2. Crear LoteProducto
            var lote = new LoteProducto
            {
                ProductoId = producto.Id,
                TenantId = tenantId,
                Cantidad = movimiento.Cantidad,
                FechaVencimiento = FechaVencimiento,
                FechaIngreso = DateTime.Now
            };
            _context.LotesProducto.Add(lote);

            // 3. Registrar Movimiento
            _context.MovimientosStock.Add(movimiento);
            _context.SaveChanges();

            TempData["Mensaje"] = "✅ Ingreso de stock y lote registrados correctamente";

            return RedirectToAction("Index", "Productos");
        }


        // GET: Venta (salida)
        public IActionResult Venta()
        {
            var tenantId = _tenantService.GetTenantId().Value;

            ViewBag.Productos = new SelectList(
                _context.Productos
                    .Where(p => p.Activo && p.Stock > 0 && p.TenantId == tenantId),
                "Id",
                "Nombre"
            );

            return View();
        }


        // POST: GUARDAR VENTA 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Venta(MovimientoStock movimiento)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            movimiento.TenantId = tenantId;
            ModelState.Remove(nameof(movimiento.TenantId));

            if (!ModelState.IsValid)
            {
                ViewBag.Productos = new SelectList(
                    _context.Productos
                        .Where(p => p.Activo && p.Stock > 0 && p.TenantId == tenantId),
                    "Id",
                    "Nombre"
                );

                return View(movimiento);
            }

            var producto = _context.Productos
                .FirstOrDefault(p => p.Id == movimiento.ProductoId && p.TenantId == tenantId);

            if (producto == null)
            {
                ModelState.AddModelError("", "Producto no encontrado");
                return View(movimiento);
            }

            if (movimiento.Cantidad > producto.Stock)
            {
                ModelState.AddModelError("Cantidad",
                    $"Stock insuficiente. Disponible: {producto.Stock}");

                ViewBag.Productos = new SelectList(
                    _context.Productos
                        .Where(p => p.Activo && p.Stock > 0 && p.TenantId == tenantId),
                    "Id",
                    "Nombre"
                );

                return View(movimiento);
            }

            producto.Stock -= movimiento.Cantidad;

            movimiento.Tipo = "VENTA";
            movimiento.Fecha = DateTime.Now;

            _context.MovimientosStock.Add(movimiento);
            _context.SaveChanges();

            return RedirectToAction("Index", "Productos");
        }

    }

}


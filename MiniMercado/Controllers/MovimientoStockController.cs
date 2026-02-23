using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

            ViewBag.Productos = new SelectList(
                _context.Productos
                    .Where(p => p.Activo && p.TenantId == tenantId),
                "Id",
                "Nombre"
            );

            return View();
        }


        // POST: Guardar ingreso
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Ingreso(MovimientoStock movimiento)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            movimiento.TenantId = tenantId;
            ModelState.Remove(nameof(movimiento.TenantId));

            if (!ModelState.IsValid)
            {
                ViewBag.Productos = new SelectList(
                    _context.Productos
                        .Where(p => p.Activo && p.TenantId == tenantId),
                    "Id",
                    "Nombre"
                );

                return View(movimiento);
            }

            movimiento.Fecha = DateTime.Now;

            var producto = _context.Productos
                .FirstOrDefault(p => p.Id == movimiento.ProductoId && p.TenantId == tenantId);

            if (producto == null)
                return BadRequest();

            producto.Stock += movimiento.Cantidad;

            _context.MovimientosStock.Add(movimiento);
            _context.SaveChanges();

            TempData["Mensaje"] = "✅ Ingreso de stock registrado correctamente";

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


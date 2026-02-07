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

        public MovimientoStockController(MiniMercadoContext context)
        {
            _context = context;
        }

        // GET: Ingreso de stock
        public IActionResult Ingreso()
        {
            ViewBag.Productos = new SelectList(
                _context.Productos.Where(p => p.Activo),
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
            if (!ModelState.IsValid)
            {
                ViewBag.Productos = new SelectList(
                    _context.Productos.Where(p => p.Activo),
                    "Id",
                    "Nombre"
                );

                return View(movimiento);
            }

            // Guardar movimiento
            movimiento.Fecha = DateTime.Now;
            _context.MovimientosStock.Add(movimiento);

            // Actualizar stock
            var producto = _context.Productos.Find(movimiento.ProductoId);
            producto.Stock += movimiento.Cantidad;

            _context.SaveChanges();

            TempData["Mensaje"] = "✅ Ingreso de stock registrado correctamente";

            return RedirectToAction("Index", "Productos");
        }


        // GET: Venta (salida)
        public IActionResult Venta()
        {
            ViewBag.Productos = new SelectList(
                _context.Productos.Where(p => p.Activo && p.Stock > 0),
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
            if (!ModelState.IsValid)
            {
                ViewBag.Productos = new SelectList(
                    _context.Productos.Where(p => p.Activo && p.Stock > 0),
                    "Id",
                    "Nombre"
                );

                return View(movimiento);
            }

            var producto = _context.Productos.Find(movimiento.ProductoId);

            if (producto == null)
            {
                ModelState.AddModelError("", "Producto no encontrado");
                return View(movimiento);
            }

            // ❌ No permitir vender más de lo disponible
            if (movimiento.Cantidad > producto.Stock)
            {
                ModelState.AddModelError("Cantidad",
                    $"Stock insuficiente. Disponible: {producto.Stock}");

                ViewBag.Productos = new SelectList(
                    _context.Productos.Where(p => p.Activo && p.Stock > 0),
                    "Id",
                    "Nombre"
                );

                return View(movimiento);
            }

            // 🔻 Restar stock
            producto.Stock -= movimiento.Cantidad;

            movimiento.Tipo = "VENTA";
            movimiento.Fecha = DateTime.Now;

            _context.MovimientosStock.Add(movimiento);
            _context.SaveChanges();

            return RedirectToAction("Index", "Productos");
        }

    }




}


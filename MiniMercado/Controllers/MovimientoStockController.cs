using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MiniMercado.Data;
using MiniMercado.Models;
using System.Linq;

namespace MiniMercado.Controllers
{
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
    }




}


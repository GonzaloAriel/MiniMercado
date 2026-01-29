
using Microsoft.AspNetCore.Mvc;
using MiniMercado.Data;
using MiniMercado.Models;
using Microsoft.EntityFrameworkCore;

namespace MiniMercado.Controllers
{
    public class VentaController : Controller
    {
        private readonly MiniMercadoContext _context;

        public VentaController(MiniMercadoContext context)
        {
            _context = context;
        }
        public IActionResult Create()
        {
            ViewBag.Productos = _context.Productos
                .Where(p => p.Activo && p.Stock > 0)
                .ToList();

            var detalles = new List<VentaDetalle>
            {
                new VentaDetalle() // al menos una fila
            };

            return View(detalles);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(List<VentaDetalle> detalles)
        {
            if (detalles == null || !detalles.Any())
            {
                ModelState.AddModelError("", "Debe agregar al menos un producto");
                return View();
            }

            decimal total = 0;

            foreach (var item in detalles)
            {
                var producto = _context.Productos.Find(item.ProductoId);

                if (producto == null || item.Cantidad > producto.Stock)
                {
                    ModelState.AddModelError("",
                        $"Stock insuficiente para {producto?.Nombre}");
                    return View();
                }

                producto.Stock -= item.Cantidad;
                item.PrecioUnitario = producto.Precio;
                total += item.PrecioUnitario * item.Cantidad;

                // Registrar movimiento
                _context.MovimientosStock.Add(new MovimientoStock
                {
                    ProductoId = producto.Id,
                    Cantidad = item.Cantidad,
                    Tipo = "VENTA"
                });
            }

            var venta = new Venta
            {
                Total = total,
                Detalles = detalles
            };

            _context.Ventas.Add(venta);
            _context.SaveChanges();

            return RedirectToAction("Index", "Productos");
        }


    }

}

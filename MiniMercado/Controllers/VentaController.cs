
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;

namespace MiniMercado.Controllers
{
    [Authorize]
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

            return View(new List<VentaDetalle> { new VentaDetalle() });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(List<VentaDetalle> detalles)
        {
            if (!ModelState.IsValid || detalles == null || !detalles.Any())
            {
                ViewBag.Productos = _context.Productos
                    .Where(p => p.Activo && p.Stock > 0)
                    .ToList();

                return View(detalles ?? new List<VentaDetalle> { new VentaDetalle() });
            }

            decimal total = 0;

            foreach (var item in detalles)
            {
                var producto = _context.Productos.Find(item.ProductoId);
                if (producto == null)
                    return BadRequest();

                // 🔒 Precio SIEMPRE parte del producto
                // Si es editable, ya viene modificado desde el input
                // pero lo normalizamos como decimal puro
                item.PrecioUnitario = producto.PrecioEditable
                    ? decimal.Round(item.PrecioUnitario, 2)
                    : producto.Precio;

                if (!producto.EsPorPeso)
                {
                    item.Cantidad = Math.Floor(item.Cantidad);
                }

                // Stock
                producto.Stock -= item.Cantidad;

                // Total
                total += item.PrecioUnitario * item.Cantidad;

                _context.MovimientosStock.Add(new MovimientoStock
                {
                    ProductoId = producto.Id,
                    Cantidad = item.Cantidad,
                    Tipo = "VENTA"
                });
            }

            //Creamos la venta
            var venta = new Venta
            {
                Total = total,
                Detalles = detalles
            };

            //Registramos la venta
            _context.Ventas.Add(venta);

            //Guardamos los cambios en la base de datos
            _context.SaveChanges();

            TempData["Mensaje"] = "✅ Venta registrada correctamente";
            TempData["VentaId"] = venta.Id;

            return RedirectToAction("Confirmacion");
        }

        public IActionResult Confirmacion()
        {
            ViewBag.Mensaje = TempData["Mensaje"];
            ViewBag.VentaId = TempData["VentaId"];
            return View();
        }

        public IActionResult Index(DateTime? desde, DateTime? hasta, int? ventaId)
        {
            var ventas = _context.Ventas.AsQueryable();

            if (ventaId.HasValue)
                ventas = ventas.Where(v => v.Id == ventaId.Value);

            if (desde.HasValue)
                ventas = ventas.Where(v => v.Fecha >= desde.Value);

            if (hasta.HasValue)
                ventas = ventas.Where(v => v.Fecha <= hasta.Value);

            return View(
                ventas
                .OrderByDescending(v => v.Fecha)
                .ToList()
            );
        }


        public IActionResult Detalle(int id)
        {
            var venta = _context.Ventas
                .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefault(v => v.Id == id);

            if (venta == null) return NotFound();

            return View(venta);
        }

        public IActionResult Ticket(int id)
        {
            var venta = _context.Ventas
                .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
                .FirstOrDefault(v => v.Id == id);

            if (venta == null) return NotFound();

            return View(venta);
        }

        public IActionResult Caja(DateTime? fecha)
        {
            DateTime dia = fecha ?? DateTime.Today;

            DateTime desde = dia.Date;
            DateTime hasta = dia.Date.AddDays(1);

            var ventas = _context.Ventas
                .Where(v => v.Fecha >= desde && v.Fecha < hasta)
                .OrderBy(v => v.Fecha)
                .ToList();

            var total = ventas.Sum(v => v.Total);

            ViewBag.Fecha = dia;
            ViewBag.Total = total;
            ViewBag.CantidadVentas = ventas.Count;

            return View(ventas);
        }


    }

}

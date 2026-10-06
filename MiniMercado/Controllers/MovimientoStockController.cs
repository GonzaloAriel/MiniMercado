using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Services;
using System.Linq;

namespace MiniMercado.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MovimientoStockController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;
        private readonly ProductoService _productoService;

        public MovimientoStockController(
               MiniMercadoContext context,
               ITenantService tenantService,
               ProductoService productoService)
        {
            _context = context;
            _tenantService = tenantService;
            _productoService = productoService;
        }

        public IActionResult Ingreso(int? productoId)
        {
            if (productoId.HasValue)
            {
                if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();

                var producto = _context.Productos
                    .FirstOrDefault(p => p.Id == productoId && p.TenantId == tenantId);

                if (producto != null)
                {
                    ViewBag.ProductoId = producto.Id;
                    ViewBag.ProductoNombre = producto.Nombre;
                    ViewBag.StockActual = producto.Stock;
                }
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Ingreso(MovimientoStock movimiento, DateTime? FechaVencimiento)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            movimiento.TenantId = tenantId;
            ModelState.Remove(nameof(movimiento.TenantId));

            if (movimiento.ProductoId == null || movimiento.ProductoId == 0)
                ModelState.AddModelError("ProductoId", "Seleccione un producto");

            if (!ModelState.IsValid) return View(movimiento);

            var producto = _context.Productos.FirstOrDefault(p => p.Id == movimiento.ProductoId && p.TenantId == tenantId);
            if (producto == null) return BadRequest();

            decimal cantidadRestante = movimiento.Cantidad;

            // 1. COMPENSACIÓN DE STOCK NEGATIVO
            if (producto.Stock < 0)
            {
                decimal deuda = Math.Abs(producto.Stock);
                decimal compensacion = Math.Min(deuda, cantidadRestante);
                
                cantidadRestante -= compensacion;

                var movComp = new MovimientoStock
                {
                    ProductoId = producto.Id,
                    TenantId = tenantId,
                    Cantidad = compensacion,
                    Tipo = "COMP_NEG", 
                    Fecha = DateTime.Now,
                    Observacion = "Compensación automática de stock negativo"
                };
                _context.MovimientosStock.Add(movComp);
            }

            // 2. CREAR LOTE SI SOBRA CANTIDAD
            if (cantidadRestante > 0)
            {
                var lote = new LoteProducto
                {
                    ProductoId = producto.Id,
                    TenantId = tenantId,
                    Cantidad = cantidadRestante,
                    FechaVencimiento = FechaVencimiento,
                    FechaIngreso = DateTime.Now
                };
                _context.LotesProducto.Add(lote);
            }

            // 3. REGISTRAR INGRESO ORIGINAL
            movimiento.Tipo = "INGRESO";
            movimiento.Fecha = DateTime.Now;
            _context.MovimientosStock.Add(movimiento);

            _context.SaveChanges();

            // 4. SINCRONIZAR
            _productoService.SincronizarStock(producto.Id, tenantId);

            TempData["Mensaje"] = "✅ Ingreso de stock registrado correctamente.";
            return RedirectToAction("Index", "Productos");
        }
        
        // ... (Venta y otros métodos se mantienen igual pero asegurando llamar a _productoService.SincronizarStock)
    }
}
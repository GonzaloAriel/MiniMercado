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
        private readonly ProductoService _productoService;

        public ProductosController(MiniMercadoContext context, 
                                   ITenantService tenantService, 
                                   AuditoriaService auditoria,
                                   ProductoService productoService)
        {
            _context = context;
            _tenantService = tenantService;
            _auditoria = auditoria;
            _productoService = productoService;
        }

        // BÚSQUEDA EN CATÁLOGO GLOBAL (AJAX)
        [HttpGet]
        [Authorize(Roles = "AdminGlobal,Admin")]
        public IActionResult BuscarProductoBase(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 3) return Json(new List<object>());

            var resultados = _context.ProductosBase
                .AsNoTracking()
                .Where(pb => pb.Nombre.Contains(q) || pb.CodigoBarras.Contains(q) || pb.Marca.Contains(q))
                .Take(10)
                .Select(pb => new
                {
                    pb.Id,
                    pb.Nombre,
                    pb.CodigoBarras,
                    pb.Marca,
                    pb.Categoria,
                    pb.PrecioReferencia
                })
                .ToList();

            return Json(resultados);
        }

        [HttpGet]
        [Authorize(Roles = "AdminGlobal, Admin")]
        public IActionResult GetProductoBase(int id)
        {
            var pb = _context.ProductosBase.AsNoTracking().FirstOrDefault(x => x.Id == id);
            if (pb == null) return NotFound();

            return Json(new
            {
                nombre = pb.Nombre,
                codigo = pb.CodigoBarras,
                categoria = pb.Categoria,
                precioReferencia = pb.PrecioReferencia
            });
        }

        // BÚSQUEDA INTELIGENTE (Local -> Global)
        [HttpGet]
        [Authorize(Roles = "Caja,Admin")]
        public IActionResult BuscarPorCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return BadRequest();

            var tenantId = _tenantService.GetTenantId().Value;
            var producto = _productoService.BuscarProductoPorCodigo(codigo, tenantId);

            if (producto == null) return NotFound();

            return Json(new
            {
                producto.Id,
                Nombre = producto.NombreDisplay,
                Codigo = producto.CodigoDisplay,
                producto.Precio,
                producto.Stock,
                producto.EsPorPeso,
                producto.PrecioEditable
            });
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
                .Include(p => p.ProductoBase)
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
            var tenantId = _tenantService.GetTenantId().Value;
            producto.TenantId = tenantId;

            ModelState.Remove(nameof(producto.TenantId));

            if (!ModelState.IsValid)
            {
                ViewBag.Rubros = _context.Rubros
                    .Where(r => r.TenantId == tenantId)
                    .ToList();
                return View(producto);
            }

            // 🔹 Validación previa: Verificar si ya existe el código en el mismo tenant
            var existe = _context.Productos
                .Any(p => p.CodigoBarra == producto.CodigoBarra && p.TenantId == tenantId);

            if (existe)
            {
                ModelState.AddModelError("CodigoBarra", "Ya existe un producto con ese código de barras.");
                ViewBag.Rubros = _context.Rubros
                    .Where(r => r.TenantId == tenantId)
                    .ToList();
                return View(producto);
            }

            try
            {
                _context.Productos.Add(producto);
                _context.SaveChanges();

                _auditoria.Registrar("Crear", "Productos", producto.Id, "Se creo un producto");

                // Redirigir a carga de stock inicial por lotes
                return RedirectToAction("Ingreso", "MovimientoStock", new { productoId = producto.Id });
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("CodigoBarra", "Error: Ya existe un producto con ese código de barras.");
                ViewBag.Rubros = _context.Rubros.Where(r => r.TenantId == tenantId).ToList();
                return View(producto);
            }
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

            // Actualizar SOLO campos permitidos
            productoDb.Nombre = producto.Nombre;
            productoDb.Precio = producto.Precio;
            productoDb.CodigoBarra = producto.CodigoBarra;
            productoDb.Activo = producto.Activo;
            productoDb.PrecioEditable = producto.PrecioEditable;
            productoDb.EsPorPeso = producto.EsPorPeso;
            productoDb.RubroId = producto.RubroId;

            // Recalcular stock desde lotes antes de guardar
            _productoService.SincronizarStock(productoDb);

            _auditoria.Registrar(
                "Modificar",
                "Productos",
                producto.Id,
                "Se modificó el producto (Stock sincronizado)"
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

        // DELETE AJAX
        [HttpPost]
        [Authorize(Roles = "AdminGlobal,Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult DesactivarAjax(int id)
        {
            var tenantId = _tenantService.GetTenantId().Value;
            var producto = _context.Productos.FirstOrDefault(p => p.Id == id && p.TenantId == tenantId);

            if (producto == null) return Json(new { ok = false, error = "Producto no encontrado" });

            producto.Activo = false;
            _context.SaveChanges();

            _auditoria.Registrar("Eliminar", "Productos", producto.Id, $"Desactivación AJAX: {producto.Nombre}");

            return Json(new { ok = true });
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

        // DETALLE DE PRODUCTO CON LOTES
        [Authorize(Roles = "AdminGlobal,Admin")]
        public IActionResult Detalle(int id)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var producto = _context.Productos
                .Include(p => p.Rubro)
                .Include(p => p.ProductoBase)
                .FirstOrDefault(p => p.Id == id && p.TenantId == tenantId);

            if (producto == null) return NotFound();

            var lotes = _context.LotesProducto
                .Where(l => l.ProductoId == id && l.TenantId == tenantId && l.Cantidad > 0)
                .OrderBy(l => l.FechaVencimiento ?? DateTime.MaxValue)
                .ToList();

            ViewBag.Lotes = lotes;
            ViewBag.StockTotal = lotes.Sum(l => l.Cantidad);

            return View(producto);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarLote(int id, string motivo)
        {
            var tenantId = _tenantService.GetTenantId().Value;
            var lote = await _context.LotesProducto.FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantId);
            
            if (lote == null) return NotFound();

            var productoId = lote.ProductoId;
            var cantidad = lote.Cantidad;

            // Registrar movimiento de merma/vencido
            var movimiento = new MovimientoStock
            {
                ProductoId = productoId,
                TenantId = tenantId,
                Cantidad = cantidad,
                Tipo = string.IsNullOrEmpty(motivo) ? "MERMA" : motivo.ToUpper(),
                Fecha = DateTime.Now,
                Observacion = $"Eliminación de lote ID: {lote.Id}. Motivo: {motivo}"
            };

            _context.MovimientosStock.Add(movimiento);
            _context.LotesProducto.Remove(lote);
            await _context.SaveChangesAsync();

            SincronizarStock(productoId, tenantId);

            TempData["Mensaje"] = "✅ Lote eliminado y stock sincronizado.";
            return RedirectToAction(nameof(Detalle), new { id = productoId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarLote(int id, decimal cantidad, DateTime? fechaVencimiento)
        {
            var tenantId = _tenantService.GetTenantId().Value;
            var lote = await _context.LotesProducto.FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantId);
            
            if (lote == null) return NotFound();

            lote.Cantidad = cantidad;
            lote.FechaVencimiento = fechaVencimiento;
            
            await _context.SaveChangesAsync();
            SincronizarStock(lote.ProductoId, tenantId);

            TempData["Mensaje"] = "✅ Lote actualizado correctamente.";
            return RedirectToAction(nameof(Detalle), new { id = lote.ProductoId });
        }

        private void SincronizarStock(int productoId, int tenantId)
        {
            var producto = _context.Productos.FirstOrDefault(p => p.Id == productoId && p.TenantId == tenantId);
            if (producto != null)
            {
                producto.Stock = _context.LotesProducto
                    .Where(l => l.ProductoId == productoId && l.TenantId == tenantId)
                    .Sum(l => (decimal?)l.Cantidad) ?? 0;
                _context.SaveChanges();
            }
        }

    }
}

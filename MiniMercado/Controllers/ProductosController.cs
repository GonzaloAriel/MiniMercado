using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Models.Importacion;
using MiniMercado.Services;
using System.Linq;
using ClosedXML.Excel;
using System.IO;

namespace MiniMercado.Controllers
{
    [Authorize(Roles = "Admin,Caja,AdminGlobal")]
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
        [Authorize(Roles = "AdminGlobal,Admin")]
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
        [Authorize(Roles = "Caja,Admin")]
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
        [Authorize(Roles = "Admin")]
        public IActionResult Index(int? rubroId, string? filtro, int page = 1)
        {
            var tenantId = _tenantService.GetTenantId().Value;
            int pageSize = 15;

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

            // Conteo total para paginación
            var totalRegistros = productosQuery.Count();
            var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            // Aplicar Skip y Take
            var productos = productosQuery
                .OrderBy(p => p.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var rubros = _context.Rubros
                .Where(r => r.TenantId == tenantId)
                .ToList();

            // 🎯 Calcular vencimiento más próximo para cada producto basado en Lotes con stock
            foreach (var p in productos)
            {
                p.FechaProximoVencimiento = _context.LotesProducto
                    .Where(l => l.ProductoId == p.Id && l.Cantidad > 0 && l.FechaVencimiento != null && l.TenantId == tenantId)
                    .Min(l => l.FechaVencimiento);
            }

            var model = new ProductoIndexViewModel
            {
                Productos = productos,
                Rubros = rubros,
                FiltroRubro = rubroId,
                PaginaActual = page,
                TotalPaginas = totalPaginas,
                FiltroNombre = filtro
            };

            return View(model);
        }

        // CREATE GET
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            var tenantId = _tenantService.GetTenantId().Value;

            ViewBag.Rubros = _context.Rubros
                .Where(r => r.TenantId == tenantId)
                .ToList();

            ViewBag.Proveedores = _context.Proveedores
                .Where(p => p.TenantId == tenantId && p.Activo)
                .ToList();

            return View();
        }

        // CREATE POST
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Create(Producto producto)
        {
            var tenantId = _tenantService.GetTenantId().Value;
            producto.TenantId = tenantId;

            ModelState.Remove(nameof(producto.TenantId));
            ModelState.Remove(nameof(producto.Rubro));

            if (!ModelState.IsValid)
            {
                ViewBag.Rubros = _context.Rubros
                    .Where(r => r.TenantId == tenantId)
                    .ToList();
                ViewBag.Proveedores = _context.Proveedores
                    .Where(p => p.TenantId == tenantId && p.Activo)
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
                ViewBag.Proveedores = _context.Proveedores
                    .Where(p => p.TenantId == tenantId && p.Activo)
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
                ViewBag.Proveedores = _context.Proveedores.Where(p => p.TenantId == tenantId && p.Activo).ToList();
                return View(producto);
            }
        }

        // EDIT GET
        [Authorize(Roles = "Admin")]
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

            ViewBag.Proveedores = _context.Proveedores
                .Where(p => p.TenantId == tenantId && p.Activo)
                .ToList();

            return View(producto);
        }

        // EDIT POST
        [HttpPost]
        [Authorize(Roles = "Admin")]
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
                ViewBag.Proveedores = _context.Proveedores
                    .Where(p => p.TenantId == tenantId && p.Activo)
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
            productoDb.ProveedorId = producto.ProveedorId;

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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        public IActionResult Detalle(int id)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var producto = _context.Productos
                .Include(p => p.Rubro)
                .Include(p => p.ProductoBase)
                .FirstOrDefault(p => p.Id == id && p.TenantId == tenantId);

            if (producto == null) return NotFound();

            var lotes = _context.LotesProducto
     .Where(l => l.ProductoId == id &&
                 l.TenantId == tenantId)
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

        [Authorize(Roles = "Admin")]
        public IActionResult Importar()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Importar(IFormFile archivo, [FromServices] ProductoImportService importService)
        {
            if (archivo == null || archivo.Length == 0)
            {
                TempData["Error"] = "Debe subir un archivo.";
                return View();
            }

            try
            {
                var previewItems = importService.ObtenerPreview(archivo.OpenReadStream());
                var viewModel = new ImportPreviewViewModel { Items = previewItems };

                return View("ImportarPreview", viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View();
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmarImportacion(List<ImportItemPreviewViewModel> items)
        {
            if (items == null || !items.Any())
            {
                TempData["Error"] = "No hay productos válidos para importar.";
                return RedirectToAction(nameof(Importar));
            }

            var tenantId = _tenantService.GetTenantId();

            // Obtener todos los rubros actuales del tenant para cachear de forma segura
            var rubrosExistentesLista = await _context.Rubros
                .Where(r => r.TenantId == tenantId)
                .ToListAsync();

            var rubrosExistentes = rubrosExistentesLista
                .GroupBy(r => r.Nombre.Trim().ToLower())
                .ToDictionary(
                    g => g.Key,
                    g => g.First()
                );

            // Asegurar "Sin clasificar"
            if (!rubrosExistentes.ContainsKey("sin clasificar"))
            {
                var rubroDefault = new Rubro
                {
                    Nombre = "Sin clasificar",
                    TenantId = tenantId.Value
                };
                _context.Rubros.Add(rubroDefault);
                await _context.SaveChangesAsync();
                rubrosExistentes.Add("sin clasificar", rubroDefault);
            }

            int importados = 0;
            int actualizados = 0;
            int errores = 0;
            var productosParaSincronizar = new List<Producto>();

            foreach (var item in items.Where(i => i.EsValido))
            {
                try
                {
                    // Lógica de Rubro
                    var nombreRubro = string.IsNullOrWhiteSpace(item.Rubro) ? "Sin clasificar" : item.Rubro.Trim();
                    var rubroKey = nombreRubro.ToLower();

                    if (!rubrosExistentes.TryGetValue(rubroKey, out var rubro))
                    {
                        rubro = new Rubro
                        {
                            Nombre = nombreRubro,
                            TenantId = tenantId.Value
                        };
                        _context.Rubros.Add(rubro);
                        await _context.SaveChangesAsync();
                        rubrosExistentes.Add(rubroKey, rubro);
                    }

                    var existente = await _context.Productos
                        .FirstOrDefaultAsync(p =>
                            p.CodigoBarra == item.CodigoBarra &&
                            p.TenantId == tenantId);

                    if (existente != null)
                    {
                        // 1. Actualizar datos descriptivos
                        existente.Nombre = item.Nombre;
                        existente.Precio = item.Precio;
                        existente.RubroId = rubro.Id;

                        // 2. Si tiene stock previo pero no tiene lotes, regularizar con un lote de migración
                        var tieneLotes = await _context.LotesProducto.AnyAsync(l => l.ProductoId == existente.Id);
                        if (!tieneLotes && existente.Stock > 0)
                        {
                            var loteMigracion = new LoteProducto
                            {
                                ProductoId = existente.Id,
                                Cantidad = existente.Stock,
                                TenantId = tenantId.Value,
                                FechaIngreso = DateTime.Now
                            };
                            _context.LotesProducto.Add(loteMigracion);

                            _context.MovimientosStock.Add(new MovimientoStock
                            {
                                ProductoId = existente.Id,
                                Cantidad = existente.Stock,
                                Tipo = "MIGRACION_STOCK_PREVIO",
                                Observacion = "Regularización de stock previo a importación Excel",
                                TenantId = tenantId.Value,
                                Fecha = DateTime.Now
                            });
                        }

                        // 3. Crear SIEMPRE el nuevo lote por la cantidad del Excel (incluso si es 0)
                        var nuevoLote = new LoteProducto
                        {
                            ProductoId = existente.Id,
                            Cantidad = item.Stock,
                            TenantId = tenantId.Value,
                            FechaIngreso = DateTime.Now
                        };
                        _context.LotesProducto.Add(nuevoLote);

                        // 4. Registrar movimiento de importación SOLO si el stock es mayor a 0
                        if (item.Stock > 0)
                        {
                            _context.MovimientosStock.Add(new MovimientoStock
                            {
                                ProductoId = existente.Id,
                                Cantidad = item.Stock,
                                Tipo = "IMPORTACION_EXCEL",
                                Observacion = "Importación masiva desde Excel",
                                TenantId = tenantId.Value,
                                Fecha = DateTime.Now
                            });
                        }

                        productosParaSincronizar.Add(existente);
                        actualizados++;
                    }
                    else
                    {
                        // PRODUCTO NUEVO
                        var producto = new Producto
                        {
                            CodigoBarra = item.CodigoBarra,
                            Nombre = item.Nombre,
                            Precio = item.Precio,
                            Stock = 0, // Se sincronizará después
                            TenantId = tenantId.Value,
                            Activo = true,
                            RubroId = rubro.Id
                        };

                        _context.Productos.Add(producto);

                        // Crear lote inicial SIEMPRE (incluso si es 0)
                        var loteInicial = new LoteProducto
                        {
                            Producto = producto, // EF maneja la relación
                            Cantidad = item.Stock,
                            TenantId = tenantId.Value,
                            FechaIngreso = DateTime.Now
                        };
                        _context.LotesProducto.Add(loteInicial);

                        // Registrar movimiento SOLO si el stock es mayor a 0
                        if (item.Stock > 0)
                        {
                            _context.MovimientosStock.Add(new MovimientoStock
                            {
                                Producto = producto,
                                Cantidad = item.Stock,
                                Tipo = "IMPORTACION_EXCEL",
                                Observacion = "Importación masiva desde Excel (Nuevo)",
                                TenantId = tenantId.Value,
                                Fecha = DateTime.Now
                            });
                        }

                        productosParaSincronizar.Add(producto);
                        importados++;
                    }
                }
                catch
                {
                    errores++;
                }
            }

            await _context.SaveChangesAsync();

            // Sincronizar stock de todos los productos afectados
            foreach (var p in productosParaSincronizar)
            {
                _productoService.SincronizarStock(p);
            }

            TempData["Importados"] = importados;
            TempData["Actualizados"] = actualizados;
            TempData["Errores"] = errores;

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult DescargarPlantillaImportacion()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Productos");

                // Encabezados
                worksheet.Cell(1, 1).Value = "Código";
                worksheet.Cell(1, 2).Value = "Producto";
                worksheet.Cell(1, 3).Value = "Precio";
                worksheet.Cell(1, 4).Value = "Stock";
                worksheet.Cell(1, 5).Value = "Categoría";

                // Formato encabezados
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;

                // Ejemplos
                worksheet.Cell(2, 1).Value = "7791234567890";
                worksheet.Cell(2, 2).Value = "Coca Cola 2L";
                worksheet.Cell(2, 3).Value = 3500;
                worksheet.Cell(2, 4).Value = 10;
                worksheet.Cell(2, 5).Value = "Bebidas";

                worksheet.Cell(3, 1).Value = "7790001112223";
                worksheet.Cell(3, 2).Value = "Pan Lactal";
                worksheet.Cell(3, 3).Value = 2500;
                worksheet.Cell(3, 4).Value = 5;
                worksheet.Cell(3, 5).Value = "Panificados";

                worksheet.Cell(4, 1).Value = "1234567890123";
                worksheet.Cell(4, 2).Value = "Cerveza Quilmes";
                worksheet.Cell(4, 3).Value = 1800;
                worksheet.Cell(4, 4).Value = 24;
                worksheet.Cell(4, 5).Value = "Cervezas";

                // Ajuste y congelar
                worksheet.Columns().AdjustToContents();
                worksheet.SheetView.FreezeRows(1);

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Plantilla_Importacion_Productos.xlsx");
                }
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RepararStockImportado()
        {
            var productosAReparar = await _context.Productos
                .Include(p => p.Tenant)
                .Where(p => !_context.LotesProducto.Any(l => l.ProductoId == p.Id))
                .ToListAsync();

            ViewBag.Total = productosAReparar.Count;

            return View(productosAReparar);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EjecutarReparacion()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var productosAReparar = await _context.Productos
                    .Where(p => !_context.LotesProducto.Any(l =>
                        l.ProductoId == p.Id &&
                        l.TenantId == p.TenantId))
                    .ToListAsync();

                if (!productosAReparar.Any())
                {
                    TempData["Info"] = "No se encontraron productos sin lotes asociados.";
                    return RedirectToAction(nameof(RepararStockImportado));
                }

                int reparados = 0;

                foreach (var producto in productosAReparar)
                {
                    // Validación extra de seguridad
                    bool tieneLotes = await _context.LotesProducto
                        .AnyAsync(l =>
                            l.ProductoId == producto.Id &&
                            l.TenantId == producto.TenantId);

                    if (tieneLotes)
                        continue;

                    var lote = new LoteProducto
                    {
                        ProductoId = producto.Id,
                        Cantidad = producto.Stock,
                        FechaIngreso = DateTime.Now,
                        FechaVencimiento = null,
                        TenantId = producto.TenantId
                    };

                    _context.LotesProducto.Add(lote);

                    _context.MovimientosStock.Add(new MovimientoStock
                    {
                        ProductoId = producto.Id,
                        Cantidad = producto.Stock,
                        Tipo = "MIGRACION_IMPORTADOR",
                        Observacion = "Lote generado automáticamente desde herramienta de migración de productos importados",
                        TenantId = producto.TenantId,
                        Fecha = DateTime.Now
                    });

                    reparados++;
                }

                await _context.SaveChangesAsync();

                // Sincronizar stock desde lotes
                foreach (var producto in productosAReparar)
                {
                    _productoService.SincronizarStock(producto);
                }

                // Auditoría agrupada por tenant
                var auditorias = productosAReparar
                    .GroupBy(p => p.TenantId);

                foreach (var grupo in auditorias)
                {
                    _auditoria.Registrar(
                        "MigracionLotes",
                        "Productos",
                        0,
                        $"Se generaron lotes iniciales para {grupo.Count()} productos que no tenían lotes asociados.",
                        grupo.Key
                    );
                }

                await transaction.CommitAsync();

                TempData["Success"] =
                    $"Migración completada correctamente. Productos procesados: {reparados}.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                TempData["Error"] =
                    $"Error durante la migración: {ex.Message}";
            }

            return RedirectToAction(nameof(RepararStockImportado));
        }
    }
}

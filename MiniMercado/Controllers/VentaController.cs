
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Services;
using System.Security.Claims;
using static System.Net.WebRequestMethods;

namespace MiniMercado.Controllers
{

    [Authorize(Roles = "Admin,Caja")]
    public class VentaController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;
        private readonly AuditoriaService _auditoria;
        private readonly IHttpContextAccessor _http;
        private readonly ProductoService _productoService;
        private readonly INotificacionService _notificacionService;

        public VentaController(MiniMercadoContext context, 
                               ITenantService tenantService, 
                               AuditoriaService auditoria, 
                               IHttpContextAccessor http,
                               ProductoService productoService,
                               INotificacionService notificacionService)
        {
            _context = context;
            _tenantService = tenantService;
            _auditoria = auditoria;
            _http = http;
            _productoService = productoService;
            _notificacionService = notificacionService;
        }

        [HttpPost]
        public IActionResult BuscarProductos(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return BadRequest();
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            texto = texto.Trim();

            // 1. Buscar productos del tenant
            // Incluimos Rubro por si necesitamos mostrarlo (opcional)
            var productosQuery = _context.Productos
                .Where(p => p.TenantId == tenantId && p.Activo &&
                    (p.Nombre.Contains(texto) || p.CodigoBarra.Contains(texto)))
                .ToList();

            // 2. Ordenar por relevancia en memoria
            var result = productosQuery
                .Select(p => new
                {
                    id = p.Id,
                    nombre = p.Nombre,
                    precio = p.Precio,
                    stock = p.Stock,
                    esPorPeso = p.EsPorPeso,
                    codigoBarra = p.CodigoBarra
                })
                .OrderBy(p => p.codigoBarra == texto ? 0 : 1) // Coincidencia exacta de código
                .ThenBy(p => p.nombre.Equals(texto, StringComparison.OrdinalIgnoreCase) ? 0 : 1) // Coincidencia exacta de nombre
                .ThenBy(p => p.nombre.StartsWith(texto, StringComparison.OrdinalIgnoreCase) ? 0 : 1) // Empieza con
                .ThenBy(p => p.nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) ? 0 : 1) // Contiene
                .Take(10)
                .ToList();

            // 3. Fallback a ProductoBase si no hay resultados locales (usando búsqueda por código exacto)
            if (!result.Any())
            {
                var productoCreado = _productoService.BuscarProductoPorCodigo(texto, tenantId);
                if (productoCreado != null)
                {
                    result.Add(new
                    {
                        id = productoCreado.Id,
                        nombre = productoCreado.Nombre,
                        precio = productoCreado.Precio,
                        stock = productoCreado.Stock,
                        esPorPeso = true,
                        codigoBarra = productoCreado.CodigoBarra
                    });
                }
            }

            return Json(result);
        }

        [HttpPost]
        public IActionResult BuscarOAgregarProducto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return BadRequest();
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();

            // 1. Buscar en local (Tenant) por nombre o código exacto
            var producto = _context.Productos
                .Include(p => p.ProductoBase)
                .FirstOrDefault(p => p.TenantId == tenantId && p.Activo && 
                    (p.Nombre.Contains(texto) || p.CodigoBarra == texto));

            // 2. Si no existe, buscar en ProductoBase y autocrear
            if (producto == null)
            {
                producto = _productoService.BuscarProductoPorCodigo(texto, tenantId);
            }

            if (producto == null) return NotFound();

            return Json(new
            {
                id = producto.Id,
                nombre = producto.Nombre,
                precio = producto.Precio,
                stock = producto.Stock,
                esPorPeso = true
            });
        }

        public IActionResult Create()
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var config = _context.Configuraciones.FirstOrDefault(c => c.TenantId == tenantId);
            if (config != null && config.UsarCaja)
            {
                var caja = _context.Cajas.FirstOrDefault(c => c.TenantId == tenantId && c.UsuarioId == userId && c.Estado == "ABIERTA");
                if (caja == null)
                {
                    ViewBag.ErrorCaja = "⚠️ No hay una caja abierta. La venta se registrará fuera de caja.";
                }
            }

            return View(new List<VentaDetalle>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(List<VentaDetalle> detalles, bool EsFiado, int? ClienteId, string? MetodoPago, decimal? MontoRecibido)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Obtener configuración
            var config = _context.Configuraciones.FirstOrDefault(c => c.TenantId == tenantId);
            if (config == null)
            {
                config = new Configuracion { TenantId = tenantId, UsarCaja = false };
                _context.Configuraciones.Add(config);
                await _context.SaveChangesAsync();
            }

            Caja? caja = null;
            if (config.UsarCaja)
            {
                // MODO CAJA: Buscar caja ABIERTA del usuario
                caja = _context.Cajas.FirstOrDefault(c => c.TenantId == tenantId && c.UsuarioId == userId && c.Estado == "ABIERTA");
            }
            else
            {
                // MODO SIMPLE: Buscar o crear caja AUTOMATICA del día
                var hoy = DateTime.Today;
                caja = _context.Cajas.FirstOrDefault(c => c.TenantId == tenantId && c.Estado == "AUTOMATICA" && c.FechaApertura >= hoy);
                if (caja == null)
                {
                    caja = new Caja
                    {
                        TenantId = tenantId,
                        UsuarioId = userId,
                        FechaApertura = DateTime.Now,
                        Estado = "AUTOMATICA",
                        MontoInicial = 0,
                        TotalVentas = 0
                    };
                    _context.Cajas.Add(caja);

                    // Se guarda antes de armar la venta para que la caja tenga Id. Si se
                    // dejaba pendiente, la primera venta del dia quedaba con CajaId null
                    // (porque caja.Id era 0 al asignarlo), y esa venta aparecia despues
                    // como "fuera de caja" en el resumen de cierre aunque si se hubiera
                    // cobrado en el cajon.
                    await _context.SaveChangesAsync();
                }
            }

            // A partir de aca se tocan stock, lotes, movimientos, la venta, el pago y la
            // auditoria. Todo eso va en una sola transaccion. Antes se guardaba la venta
            // con un SaveChanges, el pago con otro y la auditoria con un tercero, sin
            // transaccion en ningun punto: si fallaba el paso del medio (corte de
            // conexion, un veto de la base) la venta quedaba registrada sin su pago, con
            // el stock ya descontado y la caja acreditada. El cajon receiptia la plata y el
            // sistema no la ve, sin forma de saber que paso.
            //
            // Con await using, si el codigo sale por una excepcion o por uno de los
            // return View() de validacion, DisposeAsync revierte lo que se haya hecho.
            await using var transaccion = await _context.Database.BeginTransactionAsync();

            decimal total = 0;

            foreach (var item in detalles)
            {
                if (item.ProductoId == null)
                {
                    ModelState.AddModelError("",
                        "Hay productos sin seleccionar. Por favor, revise la venta.");

                    foreach (var d in detalles.Where(x => x.ProductoId.HasValue))
                    {
                        d.Producto = _context.Productos.Find(d.ProductoId);
                    }
                    return View(detalles);
                }

                var producto = _context.Productos
     .Include(p => p.ProductoBase)
     .FirstOrDefault(p => p.Id == item.ProductoId && p.TenantId == tenantId);

                // El chequeo de arriba solo cubre ProductoId nulo. El formulario tambien
                // puede mandar 0, y un ProductoId de otro tenant no existe para este
                // local: en los dos casos producto llega null y desreferenciarlo (linea de
                // precio) devolvia un 500 al cajero en mitad de la venta.
                if (producto == null)
                {
                    ModelState.AddModelError("",
                        "Hay un producto que no existe o no pertenece a este local. Por favor, revise la venta.");

                    return View(detalles);
                }

                // Se valida antes de tocar stock, lotes ni total. Antes se filtraban las
                // lineas invalidas recien despues de sumar (Cantidad negativa restaba del
                // total pero la linea se perdia del detalle, con lo cual venta.Total
                // dejaba de coincidir con la suma de sus renglones y el stock se
                // descontaba al reves).
                if (item.Cantidad <= 0)
                {
                    ModelState.AddModelError("",
                        "Las cantidades deben ser mayores a cero. Por favor, revise la venta.");

                    return View(detalles);
                }

                if (producto.ProductoBase != null)
                {
                    producto.EsPorPeso = true;
                }

                item.PrecioUnitario = producto.PrecioEditable
                    ? decimal.Round(item.PrecioUnitario, 2)
                    : producto.Precio;

                item.TenantId = tenantId;

                if (!producto.EsPorPeso)
                {
                    item.Cantidad = Math.Floor(item.Cantidad);
                }

                // --- LÓGICA FIFO (LOTES) ---
                decimal cantidadRestante = item.Cantidad;

                var lotes = _context.LotesProducto
                    .Where(l => l.ProductoId == producto.Id && l.TenantId == tenantId && l.Cantidad > 0)
                    .OrderBy(l => l.FechaVencimiento ?? DateTime.MaxValue)
                    .ThenBy(l => l.FechaIngreso)
                    .ToList();

                foreach (var lote in lotes)
                {
                    if (cantidadRestante <= 0) break;

                    decimal aDescontar = Math.Min(lote.Cantidad, cantidadRestante);
                    lote.Cantidad -= aDescontar;
                    cantidadRestante -= aDescontar;
                }

                if (cantidadRestante > 0)
                {
                    // Si no hay lotes suficientes, igual descontamos del stock general (para no bloquear venta si hubo error previo)
                    // o podrías lanzar error. Siguiendo instrucciones: "Si no alcanza stock en lotes -> error"
                    // Pero para no romper la UX actual y permitir stock negativo si el negocio lo permite:
                    // producto.Stock -= item.Cantidad; // Ya estaba así.
                }

                // Stock Total (Referencia)
                producto.Stock -= item.Cantidad;

                // Total
                total += item.PrecioUnitario * item.Cantidad;

                _context.MovimientosStock.Add(new MovimientoStock
                {
                    ProductoId = producto.Id,
                    Cantidad = item.Cantidad,
                    Tipo = "VENTA",
                    TenantId = tenantId
                });
            }

            //Creamos la venta

            detalles = detalles
                        .Where(d => d.ProductoId != 0 && d.Cantidad > 0) //filtramos fila
                        .ToList();

            var venta = new Venta
            {
                Total = total,
                Fecha = DateTime.Now,
                Activa = false,
                TenantId = tenantId,
                Detalles = detalles,
                UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Usuario = _http.HttpContext.User?.Identity?.Name,
                EsFiado = EsFiado,
                ClienteId = EsFiado ? ClienteId : null,
                CajaId = caja?.Id > 0 ? caja.Id : null
            };


            //Registramos la venta
            _context.Ventas.Add(venta);

            // Actualizamos la caja
            if (caja != null)
            {
                caja.TotalVentas += venta.Total;

                // La caja ya viene persistida: la AUTOMATICA se guardo al crearla y la
                // ABIERTA existia de antes. Agregarla de nuevo con Id 0 ya no puede
                // ocurrir, asi que no hace falta un Add extra.
            }

            //Guardamos los cambios en la base de datos (dentro de la transaccion)
            await _context.SaveChangesAsync();

            // REGISTRO DE PAGO (ya no desacoplado: va en la misma transaccion que la venta)
            if (!EsFiado)
            {
                var pago = new Pago
                {
                    VentaId = venta.Id,
                    TenantId = tenantId,
                    Metodo = string.IsNullOrWhiteSpace(MetodoPago) ? "EFECTIVO" : MetodoPago.ToUpper(),
                    Monto = venta.Total,
                    Fecha = DateTime.Now
                };

                if (pago.Metodo == "EFECTIVO")
                {
                    pago.MontoRecibido = MontoRecibido ?? venta.Total;
                    pago.Vuelto = pago.MontoRecibido - venta.Total;
                }

                _context.Pagos.Add(pago);
                await _context.SaveChangesAsync();
            }

            //Auditoria
            // Su SaveChanges entra en la misma transaccion: AuditoriaService es scoped y
            // comparte el MiniMercadoContext del request.
            _auditoria.Registrar(
                        "Crear",
                        "Ventas",
                        venta.Id,
                        "Se registró una nueva venta"
             );

            // Solo ahora se confirma. Si algo fallo antes, no se guardo ni la venta ni el
            // pago ni el stock.
            await transaccion.CommitAsync();

            TempData["Mensaje"] = "✅ Venta registrada correctamente";

            return RedirectToAction("Confirmacion", new { ventaId = venta.Id });
        }

        // POST: Confirmar venta
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Confirmacion(int ventaId, string? NombreVenta)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();

            var venta = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                     NombreVenta, UsuarioId, Usuario, EsFiado, 
                                     ClienteId, TenantId, CajaId 
                              FROM Ventas 
                              WHERE Id = {0} AND TenantId = {1}", ventaId, tenantId)
                .FirstOrDefault();

            if (venta == null)
                return NotFound();

            venta.NombreVenta = string.IsNullOrWhiteSpace(NombreVenta)
                ? null
                : NombreVenta;

            _context.SaveChanges();

            TempData["MensajeNombreVenta"] = "✅ Nombre de venta guardado correctamente.";

            return RedirectToAction("Confirmacion", new { ventaId = venta.Id });
        }

        // GET: Confirmación de venta
        public IActionResult Confirmacion(int ventaId)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();

            // Leer venta sin JOIN automático con Cliente
            var venta = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                             NombreVenta, UsuarioId, Usuario, EsFiado, 
                             ClienteId, TenantId, CajaId 
                      FROM Ventas 
                      WHERE Id = {0} AND TenantId = {1}", ventaId, tenantId)
                .FirstOrDefault();

            if (venta == null)
                return NotFound();

            // Cargar detalles por separado
            venta.Detalles = _context.VentaDetalles
                .Where(d => d.VentaId == ventaId)
                .Include(d => d.Producto)
                .ToList();

            ViewBag.VentaId = ventaId;
            ViewBag.Mensaje = TempData["Mensaje"];
            ViewBag.MensajeNombreVenta = TempData["MensajeNombreVenta"];

            return View(venta);
        }

        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta, int? ventaId, string? nombreVenta, int page = 1)
        {
            if (page < 1) page = 1;
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            int pageSize = 10;

            var query = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                     NombreVenta, UsuarioId, Usuario, EsFiado, 
                                     ClienteId, TenantId, CajaId 
                              FROM Ventas 
                              WHERE Activa = 0 AND TenantId = {0}", tenantId)
                .AsQueryable();

            if (ventaId.HasValue)
                query = query.Where(v => v.Id == ventaId.Value);

            if (!string.IsNullOrWhiteSpace(nombreVenta))
                query = query.Where(v => v.NombreVenta.Contains(nombreVenta));

            if (desde.HasValue)
                query = query.Where(v => v.Fecha >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(v => v.Fecha <= hasta.Value);

            var totalRegistros = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            var ventas = await query
                .OrderByDescending(v => v.Fecha)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PaginaActual = page;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
            ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");
            ViewBag.VentaId = ventaId;
            ViewBag.NombreVenta = nombreVenta;

            return View(ventas);
        }


        public IActionResult Detalle(int id)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();

            var venta = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                     NombreVenta, UsuarioId, Usuario, EsFiado, 
                                     ClienteId, TenantId, CajaId 
                              FROM Ventas 
                              WHERE Id = {0} AND TenantId = {1}", id, tenantId)
                .FirstOrDefault();

            if (venta == null || venta.Activa)
                return NotFound();

            venta.Detalles = _context.VentaDetalles
                .Where(v => v.VentaId == id && v.TenantId == tenantId)
                .Include(d => d.Producto)
                .ToList();

            return View(venta);
        }

        public IActionResult Ticket(int id)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();

            var venta = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                     NombreVenta, UsuarioId, Usuario, EsFiado, 
                                     ClienteId, TenantId, CajaId 
                              FROM Ventas 
                              WHERE Id = {0} AND TenantId = {1}", id, tenantId)
                .FirstOrDefault();

            if (venta == null || venta.Activa)
                return NotFound();

            venta.Detalles = _context.VentaDetalles
                .Where(v => v.VentaId == id && v.TenantId == tenantId)
                .Include(d => d.Producto)
                .ToList();

            var tenant = _context.Tenants
                .FirstOrDefault(t => t.Id == tenantId);

            ViewBag.Tenant = tenant;

            return View(venta);
        }

        public async Task<IActionResult> Caja(DateTime? fecha, int page = 1)
        {
            if (page < 1) page = 1;
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            int pageSize = 10;

            DateTime dia = fecha ?? DateTime.Today;

            DateTime desde = dia.Date;
            DateTime hasta = dia.Date.AddDays(1);

            var query = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                     NombreVenta, UsuarioId, Usuario, EsFiado, 
                                     ClienteId, TenantId, CajaId 
                              FROM Ventas 
                              WHERE Activa = 0 AND TenantId = {0}", tenantId)
                .Where(v => v.Fecha >= desde && v.Fecha < hasta)
                .AsQueryable();

            var totalRegistros = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            var ventas = await query
                .OrderBy(v => v.Fecha)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalMonto = await query.SumAsync(v => v.Total);

            ViewBag.Fecha = dia;
            ViewBag.Total = totalMonto;
            ViewBag.CantidadVentas = totalRegistros;
            ViewBag.PaginaActual = page;
            ViewBag.TotalPaginas = totalPaginas;

            return View(ventas);
        }




    }

}

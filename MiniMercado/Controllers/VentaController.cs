
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

    [Authorize]
    public class VentaController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;
        private readonly AuditoriaService _auditoria;
        private readonly IHttpContextAccessor _http;
        private void CargarProductos()
        {
            var tenantId = _tenantService.GetTenantId().Value;

            ViewBag.Productos = _context.Productos
                .Include(p => p.ProductoBase)
                .Where(p => p.Activo && p.TenantId == tenantId)
                .ToList();
        }

        public VentaController(MiniMercadoContext context, ITenantService tenantService, AuditoriaService auditoria, IHttpContextAccessor http)
        {
            _context = context;
            _tenantService = tenantService;
            _auditoria = auditoria;
            _http = http;
        }

        public IActionResult Create()
        {
            var tenantId = _tenantService.GetTenantId().Value;

            ViewBag.Productos = _context.Productos
                .Include(p => p.ProductoBase)
                .Where(p => p.Activo && p.TenantId == tenantId)
                .ToList();

            return View(new List<VentaDetalle> { new VentaDetalle() });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(List<VentaDetalle> detalles, bool EsFiado, int? ClienteId)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            // Limpiar errores automáticos del ModelBinder
            var keysConError = ModelState.Keys
                .Where(k => k.Contains("detalles"))
                .ToList();


            foreach (var key in keysConError)
            {
                if (ModelState[key].Errors.Any(e =>
                    e.ErrorMessage.Contains("invalid", StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState[key].Errors.Clear();
                }
            }
            //--------------------------------------------

            detalles = detalles
            .Where(d => d.ProductoId.HasValue)
            .ToList();

            if (EsFiado && !ClienteId.HasValue)
            {
                ModelState.AddModelError("", "Debe seleccionar un cliente para realizar una venta al fiado.");
            }

            if (!ModelState.IsValid || detalles == null || !detalles.Any())
            {
                CargarProductos();

                if (detalles == null || !detalles.Any())
                {
                    ModelState.AddModelError("", "Debe agregar al menos un producto a la venta.");
                }

                return View(detalles ?? new List<VentaDetalle> { new VentaDetalle() });
            }

            decimal total = 0;

            foreach (var item in detalles)
            {
                if (item.ProductoId == null)
                {
                    ModelState.AddModelError("",
                        "Hay productos sin seleccionar. Por favor, revise la venta.");

                    CargarProductos();
                    return View(detalles);
                }

                var producto = _context.Productos
                    .FirstOrDefault(p => p.Id == item.ProductoId && p.TenantId == tenantId);

                if (producto == null)
                    return BadRequest();

                item.PrecioUnitario = producto.PrecioEditable
                    ? decimal.Round(item.PrecioUnitario, 2)
                    : producto.Precio;

                item.TenantId = tenantId;

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
                ClienteId = EsFiado ? ClienteId : null
            };


            //Registramos la venta
            _context.Ventas.Add(venta);

            //Guardamos los cambios en la base de datos
            _context.SaveChanges();

            //Auditoria
            _auditoria.Registrar(
                        "Crear",
                        "Ventas",
                        venta.Id,
                        "Se registró una nueva venta"
             );

            TempData["Mensaje"] = "✅ Venta registrada correctamente";

            return RedirectToAction("Confirmacion", new { ventaId = venta.Id });
        }

        // POST: Confirmar venta
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Confirmacion(int ventaId, string? NombreVenta)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var venta = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                     NombreVenta, UsuarioId, Usuario, EsFiado, 
                                     ClienteId, TenantId 
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
            var tenantId = _tenantService.GetTenantId().Value;

            // Leer venta sin JOIN automático con Cliente
            var venta = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                             NombreVenta, UsuarioId, Usuario, EsFiado, 
                             ClienteId, TenantId 
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

        public IActionResult Index(DateTime? desde, DateTime? hasta, int? ventaId, string? nombreVenta)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            var ventas = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                     NombreVenta, UsuarioId, Usuario, EsFiado, 
                                     ClienteId, TenantId 
                              FROM Ventas 
                              WHERE Activa = 0 AND TenantId = {0}", tenantId)
                .AsQueryable();

            if (ventaId.HasValue)
                ventas = ventas.Where(v => v.Id == ventaId.Value);

            if (!string.IsNullOrWhiteSpace(nombreVenta))
                ventas = ventas.Where(v => v.NombreVenta.Contains(nombreVenta));

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
            var tenantId = _tenantService.GetTenantId().Value;

            var venta = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                     NombreVenta, UsuarioId, Usuario, EsFiado, 
                                     ClienteId, TenantId 
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
            var tenantId = _tenantService.GetTenantId().Value;

            var venta = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                     NombreVenta, UsuarioId, Usuario, EsFiado, 
                                     ClienteId, TenantId 
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

        public IActionResult Caja(DateTime? fecha)
        {
            var tenantId = _tenantService.GetTenantId().Value;

            DateTime dia = fecha ?? DateTime.Today;

            DateTime desde = dia.Date;
            DateTime hasta = dia.Date.AddDays(1);

            var ventas = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                     NombreVenta, UsuarioId, Usuario, EsFiado, 
                                     ClienteId, TenantId 
                              FROM Ventas 
                              WHERE Activa = 0 AND TenantId = {0}", tenantId)
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

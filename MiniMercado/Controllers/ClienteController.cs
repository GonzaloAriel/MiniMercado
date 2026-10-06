using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Services;

namespace MiniMercado.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ClienteController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;
        private readonly AuditoriaService _auditoria;

        public ClienteController(MiniMercadoContext context, ITenantService tenantService, AuditoriaService auditoria)
        {
            _context = context;
            _tenantService = tenantService;
            _auditoria = auditoria;
        }

        // INDEX
        public async Task<IActionResult> Index(string? filtro, int page = 1)
        {
            if (page < 1) page = 1;
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            int pageSize = 10;

            var query = _context.Clientes
                .Where(c => c.Activo && c.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filtro))
            {
                filtro = filtro.ToLower();
                query = query.Where(c =>
                    c.Nombre.ToLower().Contains(filtro) ||
                    (c.Telefono != null && c.Telefono.Contains(filtro)));
            }

            var totalRegistros = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            var lista = await query
                .OrderBy(c => c.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var ventasFiadas = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                     NombreVenta, UsuarioId, Usuario, EsFiado, 
                                     ClienteId, TenantId, CajaId 
                              FROM Ventas 
                              WHERE EsFiado = 1 AND Activa = 0 
                              AND TenantId = {0} AND ClienteId IS NOT NULL", tenantId)
                .ToList()
                .GroupBy(v => v.ClienteId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(v => v.Total));

            var pagos = _context.PagosFiado
                .Where(p => p.TenantId == tenantId)
                .Select(p => new { p.ClienteId, p.Monto })
                .ToList()
                .GroupBy(p => p.ClienteId)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Monto));

            ViewBag.Deudas = ventasFiadas;
            ViewBag.Pagos = pagos;
            ViewBag.PaginaActual = page;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.Filtro = filtro;

            return View(lista);
        }

        // CREATE GET
        public IActionResult Create()
        {
            return View();
        }

        // CREATE POST
        [HttpPost]
        public IActionResult Create(Cliente cliente)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            cliente.TenantId = tenantId;
            ModelState.Remove(nameof(cliente.TenantId));

            if (!ModelState.IsValid)
                return View(cliente);

            _context.Clientes.Add(cliente);
            _context.SaveChanges();

            _auditoria.Registrar("Crear", "Clientes", cliente.Id, $"Se creó el cliente {cliente.Nombre}");

            return RedirectToAction(nameof(Index));
        }

        // EDIT GET
        public IActionResult Edit(int id)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();

            var cliente = _context.Clientes
                .FirstOrDefault(c => c.Id == id && c.TenantId == tenantId);

            if (cliente == null)
                return NotFound();

            return View(cliente);
        }

        // EDIT POST
        [HttpPost]
        public IActionResult Edit(Cliente cliente)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            ModelState.Remove(nameof(cliente.TenantId));

            if (!ModelState.IsValid)
                return View(cliente);

            var clienteDb = _context.Clientes
                .FirstOrDefault(c => c.Id == cliente.Id && c.TenantId == tenantId);

            if (clienteDb == null)
                return NotFound();

            clienteDb.Nombre = cliente.Nombre;
            clienteDb.Telefono = cliente.Telefono;
            clienteDb.LimiteCredito = cliente.LimiteCredito;

            _context.SaveChanges();

            _auditoria.Registrar("Modificar", "Clientes", cliente.Id, $"Se modificó el cliente {cliente.Nombre}");

            return RedirectToAction(nameof(Index));
        }

        // DETALLE + PAGOS
        public IActionResult Detalle(int id)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();

            var cliente = _context.Clientes
                .FirstOrDefault(c => c.Id == id && c.TenantId == tenantId);

            if (cliente == null)
                return NotFound();

            var ventasFiadas = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                     NombreVenta, UsuarioId, Usuario, EsFiado, 
                                     ClienteId, TenantId, CajaId 
                              FROM Ventas 
                              WHERE ClienteId = {0} AND EsFiado = 1 
                              AND Activa = 0 AND TenantId = {1}", id, tenantId)
                .OrderByDescending(v => v.Fecha)
                .ToList();

            var pagos = _context.PagosFiado
                .Where(p => p.ClienteId == id && p.TenantId == tenantId)
                .OrderByDescending(p => p.Fecha)
                .ToList();

            decimal totalDeuda = ventasFiadas.Sum(v => v.Total);
            decimal totalPagado = pagos.Sum(p => p.Monto);
            decimal saldo = totalDeuda - totalPagado;

            ViewBag.VentasFiadas = ventasFiadas;
            ViewBag.Pagos = pagos;
            ViewBag.TotalDeuda = totalDeuda;
            ViewBag.TotalPagado = totalPagado;
            ViewBag.Saldo = saldo;

            return View(cliente);
        }

        // REGISTRAR PAGO GET
        public IActionResult RegistrarPago(int id)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();

            var cliente = _context.Clientes
                .FirstOrDefault(c => c.Id == id && c.TenantId == tenantId);

            if (cliente == null)
                return NotFound();

            var totalDeuda = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                     NombreVenta, UsuarioId, Usuario, EsFiado, 
                                     ClienteId, TenantId, CajaId 
                              FROM Ventas 
                              WHERE ClienteId = {0} AND EsFiado = 1 
                              AND Activa = 0 AND TenantId = {1}", id, tenantId)
                .Sum(v => v.Total);

            var totalPagado = _context.PagosFiado
                .Where(p => p.ClienteId == id && p.TenantId == tenantId)
                .Sum(p => p.Monto);

            ViewBag.Cliente = cliente;
            ViewBag.Saldo = totalDeuda - totalPagado;

            return View(new PagoFiado { ClienteId = id });
        }

        // REGISTRAR PAGO POST
        [HttpPost]
        public IActionResult RegistrarPago(PagoFiado pago)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();

            ModelState.Remove(nameof(pago.TenantId));
            ModelState.Remove(nameof(pago.Cliente));
            ModelState.Remove(nameof(pago.Id));

            if (!ModelState.IsValid)
            {
                var cliente = _context.Clientes
                    .FirstOrDefault(c => c.Id == pago.ClienteId && c.TenantId == tenantId);
                ViewBag.Cliente = cliente;
                ViewBag.Saldo = 0;
                return View(pago);
            }

            pago.Id = 0; 
            pago.TenantId = tenantId;
            pago.Fecha = DateTime.Now;

            _context.PagosFiado.Add(pago);
            _context.SaveChanges();

            _auditoria.Registrar("Pago", "PagosFiado", pago.Id,
                $"Pago de ${pago.Monto} del cliente Id {pago.ClienteId}");

            return RedirectToAction(nameof(Detalle), new { id = pago.ClienteId });
        }

        // BUSCAR CLIENTE (para AJAX en venta)
        [HttpGet]
        [Authorize(Roles = "Admin,Caja")]
        public IActionResult Buscar(string q)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();

            var clientes = _context.Clientes
                .Where(c => c.Activo && c.TenantId == tenantId &&
                    c.Nombre.ToLower().Contains(q.ToLower()))
                .Select(c => new { c.Id, c.Nombre, c.Telefono })
                .Take(10)
                .ToList();

            return Json(clientes);
        }

        // DESACTIVAR AJAX
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult DesactivarAjax(int id)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            var cliente = _context.Clientes.FirstOrDefault(c => c.Id == id && c.TenantId == tenantId);

            if (cliente == null) return Json(new { ok = false, error = "Cliente no encontrado" });

            cliente.Activo = false;
            _context.SaveChanges();

            _auditoria.Registrar("Eliminar", "Clientes", id, $"Desactivación AJAX: {cliente.Nombre}");

            return Json(new { ok = true });
        }

        // DESACTIVAR
        public IActionResult Desactivar(int id)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();

            var cliente = _context.Clientes
                .FirstOrDefault(c => c.Id == id && c.TenantId == tenantId);

            if (cliente == null)
                return NotFound();

            cliente.Activo = false;
            _context.SaveChanges();

            _auditoria.Registrar("Eliminar", "Clientes", id, $"Se desactivó el cliente {cliente.Nombre}");

            TempData["Mensaje"] = "✅ Cliente desactivado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Caja")]
        public IActionResult Saldo(int id)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();

            var totalDeuda = _context.Ventas
                .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                     NombreVenta, UsuarioId, Usuario, EsFiado, 
                                     ClienteId, TenantId, CajaId 
                              FROM Ventas 
                              WHERE ClienteId = {0} AND EsFiado = 1 
                              AND Activa = 0 AND TenantId = {1}", id, tenantId)
                .Sum(v => v.Total);

            var totalPagado = _context.PagosFiado
                .Where(p => p.ClienteId == id && p.TenantId == tenantId)
                .Sum(p => p.Monto);

            return Json(new { saldo = totalDeuda - totalPagado });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Caja")]
        public IActionResult CrearRapido([FromBody] ClienteRapidoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Nombre))
                return Json(new { ok = false, error = "Nombre requerido" });

            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();

            var cliente = new Cliente
            {
                Nombre = request.Nombre,
                Telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono,
                TenantId = tenantId,
                Activo = true,
                LimiteCredito = 0
            };

            _context.Clientes.Add(cliente);
            _context.SaveChanges();

            _auditoria.Registrar("Crear", "Clientes", cliente.Id,
                $"Se creó el cliente rápido: {cliente.Nombre}");

            return Json(new { ok = true, id = cliente.Id, nombre = cliente.Nombre });
        }

        public class ClienteRapidoRequest
        {
            public string Nombre { get; set; }
            public string? Telefono { get; set; }
        }
    }
}

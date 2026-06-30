using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Services;
using System;
using System.Linq;

namespace MiniMercado.Controllers
{
    [Authorize(Roles = "AdminGlobal,Admin,Caja")]
    public class EtiquetasController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;
        private readonly ProductoService _productoService;

        public EtiquetasController(MiniMercadoContext context, ITenantService tenantService, ProductoService productoService)
        {
            _context = context;
            _tenantService = tenantService;
            _productoService = productoService;
        }

        public IActionResult Index()
        {
            var tenantIdNullable = _tenantService.GetTenantId();
            if (!tenantIdNullable.HasValue) return Unauthorized();
            var tenantId = tenantIdNullable.Value;

            var productosElaborados = _context.Productos
                .Where(p => p.TenantId == tenantId && p.Activo && p.CodigoBarra.StartsWith("20"))
                .OrderByDescending(p => p.Id)
                .ToList();

            return View(productosElaborados);
        }

        // --- MÓDULO IMPRESIÓN ---

        public IActionResult Imprimir(int? productoId)
        {
            if (productoId.HasValue)
            {
                var tenantIdNullable = _tenantService.GetTenantId();
                if (tenantIdNullable.HasValue)
                {
                    ViewBag.Producto = _context.Productos.FirstOrDefault(p => p.Id == productoId.Value && p.TenantId == tenantIdNullable.Value);
                }
            }
            return View();
        }

        public IActionResult Buscar(string q)
        {
            var tenantIdNullable = _tenantService.GetTenantId();
            if (!tenantIdNullable.HasValue) return Unauthorized();
            var tenantId = tenantIdNullable.Value;

            var productos = _context.Productos
                .Where(p => p.TenantId == tenantId && p.Activo && 
                           (p.Nombre.Contains(q) || p.CodigoBarra.Contains(q)))
                .Take(10)
                .Select(p => new { p.Id, p.Nombre, p.CodigoBarra, p.Precio })
                .ToList();

            return Json(productos);
        }

        [HttpPost]
        public IActionResult RegistrarImpresion(int productoId, int cantidad, string tipoEtiqueta, bool incluyePrecio)
        {
            var tenantIdNullable = _tenantService.GetTenantId();
            if (!tenantIdNullable.HasValue) return Unauthorized();
            
            var impresion = new ImpresionEtiqueta
            {
                ProductoId = productoId,
                TenantId = tenantIdNullable.Value,
                Cantidad = cantidad,
                TipoEtiqueta = tipoEtiqueta,
                IncluyePrecio = incluyePrecio,
                UsuarioId = User.Identity.Name // O el ID de usuario correspondiente
            };

            _context.ImpresionesEtiquetas.Add(impresion);
            _context.SaveChanges();

            return RedirectToAction(nameof(VistaPrevia), new { impresionId = impresion.Id });
        }

        public IActionResult VistaPrevia(int impresionId)
        {
            var tenantIdNullable = _tenantService.GetTenantId();
            if (!tenantIdNullable.HasValue) return Unauthorized();
            var tenantId = tenantIdNullable.Value;

            var impresion = _context.ImpresionesEtiquetas
                .Include(i => i.Producto)
                .Include(i => i.Tenant)
                .FirstOrDefault(i => i.Id == impresionId && i.TenantId == tenantId);

            if (impresion == null) return NotFound();

            return View(impresion);
        }

        public IActionResult Historial()
        {
            var tenantIdNullable = _tenantService.GetTenantId();
            if (!tenantIdNullable.HasValue) return Unauthorized();
            var tenantId = tenantIdNullable.Value;

            var historial = _context.ImpresionesEtiquetas
                .Where(i => i.TenantId == tenantId)
                .OrderByDescending(i => i.FechaImpresion)
                .Include(i => i.Producto)
                .Take(50)
                .ToList();

            return View(historial);
        }

        public IActionResult Reimprimir(int id)
        {
            var impresion = _context.ImpresionesEtiquetas
                .Include(i => i.Producto)
                .FirstOrDefault(i => i.Id == id);

            if (impresion == null) return NotFound();

            // Pasamos los datos precargados a la vista de impresión
            ViewBag.Producto = impresion.Producto;
            ViewBag.Cantidad = impresion.Cantidad;
            ViewBag.TipoEtiqueta = impresion.TipoEtiqueta;
            ViewBag.IncluyePrecio = impresion.IncluyePrecio;

            return View("Imprimir");
        }

        // --- MÓDULO CREACIÓN ---

        public IActionResult Create()
        {
            var tenantIdNullable = _tenantService.GetTenantId();
            if (!tenantIdNullable.HasValue) return Unauthorized();
            var tenantId = tenantIdNullable.Value;
            ViewBag.RubroId = new SelectList(_context.Rubros.Where(r => r.TenantId == tenantId && r.Activo), "Id", "Nombre");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Producto producto, decimal stockInicial)
        {
            var tenantIdNullable = _tenantService.GetTenantId();
            if (!tenantIdNullable.HasValue) return Unauthorized();
            var tenantId = tenantIdNullable.Value;
            
            // 1. Generar código interno
            var ultimoCodigo = _context.Productos
                .Where(p => p.TenantId == tenantId && p.CodigoBarra.StartsWith("20"))
                .Select(p => p.CodigoBarra)
                .AsEnumerable()
                .Select(c => {
                    long val;
                    return long.TryParse(c, out val) ? val : 200000000000;
                })
                .DefaultIfEmpty(200000000000)
                .Max();

            producto.CodigoBarra = (ultimoCodigo + 1).ToString();
            producto.TenantId = tenantId;
            producto.Activo = true;
            producto.Stock = 0; // Se inicializará con el lote

            // 2. Limpiar errores de propiedades que no están en el formulario o son calculadas
            ModelState.Remove(nameof(Producto.CodigoBarra));
            ModelState.Remove(nameof(Producto.CodigoDisplay));

            // 3. Validar el modelo manualmente ahora que tiene CodigoBarra
            if (TryValidateModel(producto) && ModelState.IsValid)
            {
                _context.Productos.Add(producto);
                _context.SaveChanges();

                if (stockInicial > 0)
                {
                    var lote = new LoteProducto
                    {
                        ProductoId = producto.Id,
                        TenantId = tenantId,
                        Cantidad = stockInicial,
                        FechaIngreso = DateTime.Now
                    };
                    _context.LotesProducto.Add(lote);
                    _context.SaveChanges();
                    _productoService.SincronizarStock(producto);
                }

                return RedirectToAction(nameof(Success), new { id = producto.Id });
            }

            // Si llega aquí, es porque la validación falló
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            
            // 4. Corregir posible duplicación: Solo mostrar errores únicos
            TempData["Error"] = "Error al crear producto: " + string.Join(" | ", errors.Distinct());
            
            ViewBag.RubroId = new SelectList(_context.Rubros.Where(r => r.TenantId == tenantId && r.Activo), "Id", "Nombre", producto.RubroId);
            return View(producto);
        }

        public IActionResult Success(int id)
        {
            var tenantIdNullable = _tenantService.GetTenantId();
            if (!tenantIdNullable.HasValue) return Unauthorized();
            var tenantId = tenantIdNullable.Value;

            var producto = _context.Productos.FirstOrDefault(p => p.Id == id && p.TenantId == tenantId);
            if (producto == null) return NotFound();
            return View(producto);
        }
    }
}

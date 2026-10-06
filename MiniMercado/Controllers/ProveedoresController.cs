using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Models.ViewModels;
using MiniMercado.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniMercado.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProveedoresController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;
        private readonly AuditoriaService _auditoria;
        private readonly ProveedorService _proveedorService;

        public ProveedoresController(MiniMercadoContext context, ITenantService tenantService, AuditoriaService auditoria, ProveedorService proveedorService)
        {
            _context = context;
            _tenantService = tenantService;
            _auditoria = auditoria;
            _proveedorService = proveedorService;
        }

        // GET: Proveedores
        public async Task<IActionResult> Index(int? proveedorId, int? productoId, string? q, int page = 1)
        {
            if (page < 1) page = 1;
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            int pageSize = 10;

            if (productoId.HasValue && !proveedorId.HasValue)
            {
                ViewBag.MensajeInfo = "Este producto no tiene proveedor asignado. Mostrando lista completa.";
            }

            var result = await _proveedorService.ObtenerFiltradosAsync(tenantId, proveedorId, q, page, pageSize);

            ViewBag.Proveedores = result.Lista;
            ViewBag.CurrentFilter = q;
            ViewBag.PaginaActual = page;
            ViewBag.TotalPaginas = (int)Math.Ceiling(result.Total / (double)pageSize);
            ViewBag.ProductoId = productoId;
            ViewBag.ProveedorId = proveedorId;
            
            return View();
        }

        // GET: Proveedores/Detalle/5
        public async Task<IActionResult> Detalle(int? id)
        {
            if (id == null) return NotFound();

            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            var proveedor = await _context.Proveedores
                .Include(p => p.Productos)
                    .ThenInclude(p => p.ProductoBase)
                .FirstOrDefaultAsync(m => m.Id == id && m.TenantId == tenantId);

            if (proveedor == null) return NotFound();

            return View(proveedor);
        }

        // GET: Proveedores/Create
        public IActionResult Create()
        {
            return View(new ProveedorViewModel());
        }

        // POST: Proveedores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProveedorViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
                var proveedor = new Proveedor
                {
                    TenantId = tenantId,
                    Nombre = model.Nombre,
                    Contacto = model.Contacto,
                    Telefono = model.Telefono,
                    Email = model.Email,
                    Direccion = model.Direccion,
                    CUIT = model.CUIT,
                    Notas = model.Notas,
                    Activo = true,
                    FechaAlta = DateTime.Now
                };

                _context.Add(proveedor);
                await _context.SaveChangesAsync();
                
                _auditoria.Registrar("CREATE", "Proveedores", proveedor.Id, $"Creado proveedor: {proveedor.Nombre}");

                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Proveedores/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            var proveedor = await _context.Proveedores
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

            if (proveedor == null) return NotFound();

            var model = new ProveedorViewModel
            {
                Id = proveedor.Id,
                Nombre = proveedor.Nombre,
                Contacto = proveedor.Contacto,
                Telefono = proveedor.Telefono,
                Email = proveedor.Email,
                Direccion = proveedor.Direccion,
                CUIT = proveedor.CUIT,
                Notas = proveedor.Notas,
                Activo = proveedor.Activo
            };

            return View(model);
        }

        // POST: Proveedores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProveedorViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
                var proveedor = await _context.Proveedores
                    .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

                if (proveedor == null) return NotFound();

                proveedor.Nombre = model.Nombre;
                proveedor.Contacto = model.Contacto;
                proveedor.Telefono = model.Telefono;
                proveedor.Email = model.Email;
                proveedor.Direccion = model.Direccion;
                proveedor.CUIT = model.CUIT;
                proveedor.Notas = model.Notas;
                proveedor.Activo = model.Activo;

                try
                {
                    _context.Update(proveedor);
                    await _context.SaveChangesAsync();
                    
                    _auditoria.Registrar("EDIT", "Proveedores", proveedor.Id, $"Editado proveedor: {proveedor.Nombre}");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProveedorExists(proveedor.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // POST: Proveedores/ToggleActivo/5
        [HttpPost]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            var proveedor = await _context.Proveedores
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

            if (proveedor == null) return NotFound();

            proveedor.Activo = !proveedor.Activo;
            await _context.SaveChangesAsync();
            
            _auditoria.Registrar("TOGGLE_ACTIVO", "Proveedores", proveedor.Id, $"{(proveedor.Activo ? "Activado" : "Desactivado")} proveedor: {proveedor.Nombre}");

            return RedirectToAction(nameof(Index));
        }

        private bool ProveedorExists(int id)
        {
            return _tenantService.TryGetTenantId(out var tenantId)
                && _context.Proveedores.Any(e => e.Id == id && e.TenantId == tenantId);
        }
    }
}

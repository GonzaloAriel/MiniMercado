using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Services;
using System;

[Authorize(Roles = "Admin,Caja")]
public class VentaAdminController : Controller
{
    private readonly MiniMercadoContext _context;
    private readonly ITenantService _tenantService;
    private readonly AuditoriaService _auditoria;

    public VentaAdminController(
            MiniMercadoContext context,
            ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    // =====================================================
    // LISTADO DE VENTAS
    // =====================================================
    [Authorize(Roles = "Admin,Caja")]
    public IActionResult Index(DateTime? desde, DateTime? hasta, string? estado)
    {
        var tenantId = _tenantService.GetTenantId().Value;

        var ventas = _context.Ventas
            .Where(v => v.TenantId == tenantId)
            .Include(v => v.Detalles)
            .OrderByDescending(v => v.Fecha)
            .AsQueryable();

        if (desde.HasValue)
            ventas = ventas.Where(v => v.Fecha >= desde.Value);

        if (hasta.HasValue)
            ventas = ventas.Where(v => v.Fecha <= hasta.Value.AddDays(1));

        if (estado == "activa")
            ventas = ventas.Where(v => !v.Activa); // Activas

        if (estado == "anulada")
            ventas = ventas.Where(v => v.Activa); // Anuladas

        // Asignar el valor a ViewBag.Estado para usar en la vista
        ViewBag.Estado = estado;

        ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
        ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");

        return View(ventas.ToList());
    }

    // =====================================================
    // DETALLE DE VENTA
    // =====================================================
    [Authorize(Roles = "Admin,Caja")]
    public IActionResult Detalle(int id)
    {
        var tenantId = _tenantService.GetTenantId().Value;

        var venta = _context.Ventas
            .Where(v => v.TenantId == tenantId && v.Id == id)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .FirstOrDefault();

        if (venta == null)
            return NotFound();

        return View(venta);
    }

    // =====================================================
    // CONFIRMAR ANULACIÓN (GET)
    // =====================================================
    [Authorize(Roles = "Admin,Caja")]
    public IActionResult Anular(int id)
    {
        var tenantId = _tenantService.GetTenantId().Value;

        var venta = _context.Ventas
            .FirstOrDefault(v => v.Id == id && v.TenantId == tenantId);

        if (venta == null)
            return NotFound();

        if (venta.Activa)
        {
            TempData["Mensaje"] = "⚠ Esta venta ya está anulada.";
            return RedirectToAction(nameof(Index));
        }

        return View(venta);
    }

    // =====================================================
    // CONFIRMAR ANULACIÓN (POST)
    // =====================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Caja")]
    public IActionResult ConfirmarAnular(int id, string? motivoAnulacion)
    {
        var tenantId = _tenantService.GetTenantId().Value;

        var venta = _context.Ventas
            .Where(v => v.Id == id && v.TenantId == tenantId)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .FirstOrDefault();

        if (venta == null)
            return NotFound();

        if (venta.Activa)
        {
            TempData["Mensaje"] = "⚠ La venta ya estaba anulada.";
            return RedirectToAction(nameof(Index));
        }

        // Restaurar stock
        foreach (var detalle in venta.Detalles)
        {
            var producto = detalle.Producto;
            producto.Stock += detalle.Cantidad;

            _context.MovimientosStock.Add(new MovimientoStock
            {
                ProductoId = producto.Id,
                Cantidad = detalle.Cantidad,
                Tipo = "ANULACION",
                Fecha = DateTime.Now,
                TenantId = tenantId
            });
        }

        // Marcar venta como anulada y guardar motivo
        venta.Activa = true;
        venta.MotivoAnulacion = string.IsNullOrWhiteSpace(motivoAnulacion)
            ? null
            : motivoAnulacion;

        _context.SaveChanges();

        TempData["Mensaje"] = $"✅ Venta #{venta.Id} anulada correctamente.";

        return RedirectToAction(nameof(Index));
    }
}



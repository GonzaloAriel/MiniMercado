using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;
using System;

[Authorize(Roles = "Admin")]
public class VentaAdminController : Controller
{
    private readonly MiniMercadoContext _context;
    private readonly ITenantService _tenantService;

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
    [Authorize(Roles = "AdminGlobal,Admin")]
    public IActionResult Index()
    {
        var tenantId = _tenantService.GetTenantId().Value;

        var ventas = _context.Ventas
            .Where(v => v.TenantId == tenantId)
            .Include(v => v.Detalles)
            .OrderByDescending(v => v.Fecha)
            .ToList();

        return View(ventas);
    }

    // =====================================================
    // DETALLE DE VENTA
    // =====================================================
    [Authorize(Roles = "AdminGlobal,Admin")]
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
    [Authorize(Roles = "AdminGlobal,Admin")]
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
    [Authorize(Roles = "AdminGlobal,Admin")]
    public IActionResult ConfirmarAnular(int id)
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

        // 🔁 Restaurar stock
        foreach (var detalle in venta.Detalles)
        {
            var producto = detalle.Producto;

            producto.Stock += detalle.Cantidad;

            // Registrar movimiento de stock
            _context.MovimientosStock.Add(new MovimientoStock
            {
                ProductoId = producto.Id,
                Cantidad = detalle.Cantidad,
                Tipo = "ANULACION",
                Fecha = DateTime.Now,
                TenantId = tenantId
            });
        }

        // Marcar venta como anulada
        venta.Activa = true;

        _context.SaveChanges();

        TempData["Mensaje"] = $"✅ Venta #{venta.Id} anulada correctamente.";

        return RedirectToAction(nameof(Index));
    }
}



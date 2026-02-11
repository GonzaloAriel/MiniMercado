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

    public VentaAdminController(MiniMercadoContext context)
    {
        _context = context;
    }

    // =====================================================
    // LISTADO DE VENTAS
    // =====================================================
    public IActionResult Index()
    {
        var ventas = _context.Ventas
            .Include(v => v.Detalles)
            .OrderByDescending(v => v.Fecha)
            .ToList();

        return View(ventas);
    }

    // =====================================================
    // DETALLE DE VENTA
    // =====================================================
    public IActionResult Detalle(int id)
    {
        var venta = _context.Ventas
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .FirstOrDefault(v => v.Id == id);

        if (venta == null)
            return NotFound();

        return View(venta);
    }

    // =====================================================
    // CONFIRMAR ANULACIÓN (GET)
    // =====================================================
    public IActionResult Anular(int id)
    {
        var venta = _context.Ventas
            .FirstOrDefault(v => v.Id == id);

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
    public IActionResult ConfirmarAnular(int id)
    {
        var venta = _context.Ventas
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .FirstOrDefault(v => v.Id == id);

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
                Tipo = "ANULACION"
            });
        }

        // Marcar venta como anulada
        venta.Activa = true;

        _context.SaveChanges();

        TempData["Mensaje"] = $"✅ Venta #{venta.Id} anulada correctamente.";

        return RedirectToAction(nameof(Index));
    }
}



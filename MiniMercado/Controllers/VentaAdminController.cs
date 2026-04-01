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
            ITenantService tenantService,
            AuditoriaService auditoria)
    {
        _context = context;
        _tenantService = tenantService;
        _auditoria = auditoria;
    }

    // ANULAR AJAX
    [HttpPost]
    [Authorize(Roles = "Admin,Caja")]
    [ValidateAntiForgeryToken]
    public IActionResult AnularAjax(int id, string? motivoAnulacion)
    {
        var tenantId = _tenantService.GetTenantId().Value;
        var venta = _context.Ventas
            .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                 NombreVenta, UsuarioId, Usuario, EsFiado, 
                                 ClienteId, TenantId, CajaId 
                          FROM Ventas 
                          WHERE Id = {0} AND TenantId = {1}", id, tenantId)
            .FirstOrDefault();

        if (venta == null) return Json(new { ok = false, error = "Venta no encontrada" });
        if (venta.Activa) return Json(new { ok = false, error = "La venta ya estaba anulada" });

        venta.Detalles = _context.VentaDetalles
            .Where(d => d.VentaId == id && d.TenantId == tenantId)
            .Include(d => d.Producto)
            .ToList();

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

        venta.Activa = true;
        venta.MotivoAnulacion = motivoAnulacion;
        _context.SaveChanges();

        _auditoria.Registrar("Anular", "Ventas", venta.Id, "Anulación AJAX");

        return Json(new { ok = true });
    }

    // =====================================================
    // LISTADO DE VENTAS
    // =====================================================
    [Authorize(Roles = "Admin,Caja")]
    public IActionResult Index(DateTime? desde, DateTime? hasta, string? estado)
    {
        var tenantId = _tenantService.GetTenantId().Value;

        var ventas = _context.Ventas
            .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                 NombreVenta, UsuarioId, Usuario, EsFiado, 
                                 ClienteId, TenantId, CajaId 
                          FROM Ventas 
                          WHERE TenantId = {0}", tenantId)
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

        var listaVentas = ventas.ToList();

        // Cargar detalles manualmente para evitar JOINs automáticos en la consulta base
        foreach (var v in listaVentas)
        {
            v.Detalles = _context.VentaDetalles
                .Where(d => d.VentaId == v.Id && d.TenantId == tenantId)
                .ToList();
        }

        return View(listaVentas);
    }

    // =====================================================
    // DETALLE DE VENTA
    // =====================================================
    [Authorize(Roles = "Admin,Caja")]
    public IActionResult Detalle(int id)
    {
        var tenantId = _tenantService.GetTenantId().Value;

        var venta = _context.Ventas
            .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                 NombreVenta, UsuarioId, Usuario, EsFiado, 
                                 ClienteId, TenantId, CajaId 
                          FROM Ventas 
                          WHERE Id = {0} AND TenantId = {1}", id, tenantId)
            .FirstOrDefault();

        if (venta == null)
            return NotFound();

        venta.Detalles = _context.VentaDetalles
            .Where(v => v.VentaId == id && v.TenantId == tenantId)
            .Include(d => d.Producto)
            .ToList();

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
            .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                 NombreVenta, UsuarioId, Usuario, EsFiado, 
                                 ClienteId, TenantId, CajaId 
                          FROM Ventas 
                          WHERE Id = {0} AND TenantId = {1}", id, tenantId)
            .FirstOrDefault();

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
            .FromSqlRaw(@"SELECT Id, Fecha, Total, Activa, MotivoAnulacion, 
                                 NombreVenta, UsuarioId, Usuario, EsFiado, 
                                 ClienteId, TenantId, CajaId 
                          FROM Ventas 
                          WHERE Id = {0} AND TenantId = {1}", id, tenantId)
            .FirstOrDefault();

        if (venta == null)
            return NotFound();

        if (venta.Activa)
        {
            TempData["Mensaje"] = "⚠ La venta ya estaba anulada.";
            return RedirectToAction(nameof(Index));
        }

        venta.Detalles = _context.VentaDetalles
            .Where(d => d.VentaId == id && d.TenantId == tenantId)
            .Include(d => d.Producto)
            .ToList();

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

        //Auditoria
        _auditoria.Registrar(
                    "Anular",
                    "Ventas",
                    venta.Id,
                    "Se anulo una venta"
         );

        TempData["Mensaje"] = $"✅ Venta #{venta.Id} anulada correctamente.";

        return RedirectToAction(nameof(Index));
    }
}

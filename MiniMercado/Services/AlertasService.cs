using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;

namespace MiniMercado.Services
{
    public class AlertasService
    {
        private readonly MiniMercadoContext _context;
        private readonly INotificacionService _notificacionService;

        public AlertasService(MiniMercadoContext context, INotificacionService notificacionService)
        {
            _context = context;
            _notificacionService = notificacionService;
        }

        public async Task VerificarYCrearNotificaciones(int tenantId)
        {
            var hoy = DateTime.Today;

            // 1. STOCK NEGATIVO
            var negativos = GetStockNegativo(tenantId);
            foreach (var p in negativos)
            {
                await CrearNotificacionInteligente(tenantId, "STOCK_NEGATIVO", "Stock negativo",
                    $"El producto '{p.Nombre}' tiene stock negativo ({p.Stock})", 
                    $"/MovimientoStock/Ingreso?productoId={p.Id}", p.Id);
            }

            // 2. STOCK BAJO
            var bajoStock = GetStockBajo(tenantId);
            foreach (var p in bajoStock)
            {
                await CrearNotificacionInteligente(tenantId, "STOCK_BAJO", "Stock bajo",
                    $"El producto '{p.Nombre}' tiene stock bajo ({p.Stock})", 
                    "/Alertas/StockBajo", p.Id);
            }

            // 3. VENCIMIENTOS
            var vencimientos = GetVencimientos(tenantId);
            foreach (var lote in vencimientos)
            {
                bool vencido = lote.FechaVencimiento < hoy;
                string tipo = vencido ? "VENCIDO" : "POR_VENCER";
                string titulo = vencido ? "Producto vencido" : "Próximo a vencer";
                string msg = vencido 
                    ? $"El producto '{lote.Producto.Nombre}' tiene un lote vencido ({lote.FechaVencimiento?.ToString("dd/MM/yyyy")})"
                    : $"El producto '{lote.Producto.Nombre}' tiene un lote que vence pronto ({lote.FechaVencimiento?.ToString("dd/MM/yyyy")})";

                await CrearNotificacionInteligente(tenantId, tipo, titulo, msg, "/Alertas/Vencimientos", lote.ProductoId);
            }
        }

        private async Task CrearNotificacionInteligente(int tenantId, string tipo, string titulo, string mensaje, string url, int productoId)
        {
            var limite24Horas = DateTime.Now.AddHours(-24);
            
            // 🎯 VALIDACIÓN ANTI-DUPLICADOS (24hs)
            // Buscamos por Tenant, Tipo y Producto (identificado en la URL o el mensaje)
            bool yaNotificado = await _context.Notificaciones
                .AnyAsync(n => n.TenantId == tenantId 
                               && n.Tipo == tipo 
                               && n.Fecha >= limite24Horas
                               && (n.Url.Contains($"productoId={productoId}") || n.Mensaje.Contains($"'{productoId}'") || n.Mensaje.Contains(mensaje)));

            if (!yaNotificado)
            {
                await _notificacionService.CrearNotificacion(titulo, mensaje, tipo, url);
            }
        }

        // --- MÉTODOS DE CONSULTA (Fuente de Verdad) ---

        public async Task<(List<LoteProducto> Lista, int Total)> GetVencimientosPaginated(int tenantId, int page, int pageSize)
        {
            var hoy = DateTime.Today;
            var limite = hoy.AddDays(7);

            var query = _context.LotesProducto
                .Include(l => l.Producto)
                .Where(l => l.TenantId == tenantId &&
                            l.Cantidad > 0 &&
                            l.FechaVencimiento != null &&
                            l.FechaVencimiento <= limite);

            var total = await query.CountAsync();
            var lista = await query
                .OrderBy(l => l.FechaVencimiento)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (lista, total);
        }

        public async Task<(List<Producto> Lista, int Total)> GetStockBajoPaginated(int tenantId, int page, int pageSize)
        {
            var query = _context.Productos
                .Include(p => p.Proveedor)
                .Where(p => p.TenantId == tenantId && p.Activo && p.Stock > 0 && p.Stock <= 5);

            var total = await query.CountAsync();
            var lista = await query
                .OrderBy(p => p.Stock)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (lista, total);
        }

        public async Task<(List<Producto> Lista, int Total)> GetStockNegativoPaginated(int tenantId, int page, int pageSize)
        {
            var query = _context.Productos
                .Where(p => p.TenantId == tenantId && p.Activo && p.Stock < 0);

            var total = await query.CountAsync();
            var lista = await query
                .OrderBy(p => p.Stock)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (lista, total);
        }

        public List<LoteProducto> GetVencimientos(int tenantId)
        {
            var hoy = DateTime.Today;
            var limite = hoy.AddDays(7);

            return _context.LotesProducto
                .Include(l => l.Producto)
                .Where(l => l.TenantId == tenantId &&
                            l.Cantidad > 0 &&
                            l.FechaVencimiento != null &&
                            l.FechaVencimiento <= limite)
                .OrderBy(l => l.FechaVencimiento)
                .ToList();
        }

        public List<Producto> GetStockBajo(int tenantId)
        {
            return _context.Productos
                .Include(p => p.Proveedor)
                .Where(p => p.TenantId == tenantId && p.Activo && p.Stock > 0 && p.Stock <= 5)
                .OrderBy(p => p.Stock)
                .ToList();
        }

        public List<Producto> GetStockNegativo(int tenantId)
        {
            return _context.Productos
                .Where(p => p.TenantId == tenantId && p.Activo && p.Stock < 0)
                .OrderBy(p => p.Stock)
                .ToList();
        }

        public List<AlertaStockItem> GetProductosVencidos(int tenantId, int count = 5)
        {
            var hoy = DateTime.Now;

            return _context.LotesProducto
                .Include(l => l.Producto)
                .Where(l => l.TenantId == tenantId && l.Cantidad > 0 && l.FechaVencimiento != null && l.FechaVencimiento < hoy)
                .GroupBy(l => new { l.ProductoId, l.Producto.Nombre })
                .Select(g => new AlertaStockItem
                {
                    ProductoId = g.Key.ProductoId,
                    Nombre = g.Key.Nombre,
                    Cantidad = g.Sum(x => x.Cantidad),
                    Fecha = g.Min(x => x.FechaVencimiento).Value
                })
                .OrderBy(x => x.Fecha)
                .Take(count)
                .ToList();
        }

        public List<AlertaStockItem> GetProductosPorVencer(int tenantId, int dias = 3, int count = 5)
        {
            var hoy = DateTime.Now;
            var limiteProximo = hoy.AddDays(dias);

            return _context.LotesProducto
                .Include(l => l.Producto)
                .Where(l => l.TenantId == tenantId && l.Cantidad > 0 && l.FechaVencimiento != null && l.FechaVencimiento >= hoy && l.FechaVencimiento <= limiteProximo)
                .GroupBy(l => new { l.ProductoId, l.Producto.Nombre })
                .Select(g => new AlertaStockItem
                {
                    ProductoId = g.Key.ProductoId,
                    Nombre = g.Key.Nombre,
                    Cantidad = g.Sum(x => x.Cantidad),
                    Fecha = g.Min(x => x.FechaVencimiento).Value
                })
                .OrderBy(x => x.Fecha)
                .Take(count)
                .ToList();
        }
    }

    public class AlertaStockItem
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; }
        public decimal Cantidad { get; set; }
        public DateTime Fecha { get; set; }
    }
}

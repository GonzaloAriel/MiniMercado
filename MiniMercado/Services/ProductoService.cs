using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;

namespace MiniMercado.Services
{
    public class ProductoService
    {
        private readonly MiniMercadoContext _context;

        public ProductoService(MiniMercadoContext context)
        {
            _context = context;
        }

        public Producto? BuscarProductoPorCodigo(string codigo, int tenantId)
        {
            var producto = _context.Productos
                .Include(p => p.ProductoBase)
                .FirstOrDefault(p => p.CodigoBarra == codigo && p.TenantId == tenantId);

            if (producto != null) return producto;

            var baseInfo = _context.ProductosBase
                .FirstOrDefault(pb => pb.CodigoBarras == codigo);

            if (baseInfo != null)
            {
                int? rubroId = null;
                if (!string.IsNullOrWhiteSpace(baseInfo.Categoria))
                {
                    var rubro = _context.Rubros
                        .FirstOrDefault(r => r.Nombre.ToLower() == baseInfo.Categoria.ToLower() && r.TenantId == tenantId && r.Activo);

                    if (rubro == null)
                    {
                        rubro = new Rubro
                        {
                            Nombre = baseInfo.Categoria,
                            TenantId = tenantId,
                            Activo = true
                        };
                        _context.Rubros.Add(rubro);
                        _context.SaveChanges();
                    }
                    rubroId = rubro.Id;
                }

                producto = new Producto
                {
                    TenantId = tenantId,
                    ProductoBaseId = baseInfo.Id,
                    Nombre = baseInfo.Nombre,
                    CodigoBarra = baseInfo.CodigoBarras,
                    Precio = baseInfo.PrecioReferencia ?? 0,
                    Stock = 0,
                    Activo = true,
                    RubroId = rubroId
                };

                _context.Productos.Add(producto);
                _context.SaveChanges();
                return producto;
            }

            return null;
        }

        public decimal GetStockReal(int productoId, int tenantId)
        {
            return _context.LotesProducto
                .Where(l => l.ProductoId == productoId 
                         && l.TenantId == tenantId 
                         && l.Cantidad > 0)
                .Sum(l => (decimal?)l.Cantidad) ?? 0;
        }

        public void SincronizarStock(int productoId, int tenantId)
        {
            var producto = _context.Productos.Find(productoId);
            if (producto != null && producto.TenantId == tenantId)
            {
                SincronizarStock(producto);
            }
        }

        public void SincronizarStock(Producto producto)
        {
            var totalLotes = GetStockReal(producto.Id, producto.TenantId);

            // 🔥 Solo sincronizar si hay lotes. 
            // Si no hay lotes, respetamos el stock actual (que puede ser negativo).
            if (totalLotes > 0)
            {
                producto.Stock = totalLotes;
            }
            
            _context.SaveChanges();
        }
    }
}
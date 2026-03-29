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

        /// <summary>
        /// Busca un producto en el tenant local. Si no existe, lo busca en el catálogo global
        /// y lo crea automáticamente para el tenant actual.
        /// </summary>
        public Producto? BuscarProductoPorCodigo(string codigo, int tenantId)
        {
            // 1. Buscar en local (Tenant)
            var producto = _context.Productos
                .Include(p => p.ProductoBase)
                .FirstOrDefault(p => p.CodigoBarra == codigo && p.TenantId == tenantId);

            if (producto != null) return producto;

            // 2. Si no existe localmente, buscar en el Catálogo Global (ProductoBase)
            var baseInfo = _context.ProductosBase
                .FirstOrDefault(pb => pb.CodigoBarras == codigo);

            // 3. Si existe en el catálogo global, lo autocreamos para este tenant
            if (baseInfo != null)
            {
                // Buscamos o creamos el rubro basado en la categoría
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
                else
                {
                    // Fallback: Buscamos el primer rubro activo del tenant para no romper la validación [Required]
                    var primerRubro = _context.Rubros
                        .FirstOrDefault(r => r.TenantId == tenantId && r.Activo);
                    rubroId = primerRubro?.Id;
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

            // 4. No existe en ningún lado
            return null;
        }
    }
}
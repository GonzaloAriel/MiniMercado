using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;

namespace MiniMercado.Services
{
    public class ProveedorService
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;

        public ProveedorService(MiniMercadoContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        private int GetTenantId() => _tenantService.GetTenantId() ?? throw new Exception("Tenant no identificado");

        public async Task<List<dynamic>> ObtenerFiltradosAsync(int tenantId, int? proveedorId, string? q)
        {
            var query = _context.Proveedores
                .Where(p => p.TenantId == tenantId)
                .AsQueryable();

            if (proveedorId.HasValue)
            {
                query = query.Where(p => p.Id == proveedorId.Value);
            }

            if (!string.IsNullOrEmpty(q))
            {
                q = q.ToLower();
                query = query.Where(p => p.Nombre.ToLower().Contains(q) || (p.CUIT != null && p.CUIT.Contains(q)));
            }

            return await query
                .Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    p.Contacto,
                    p.Telefono,
                    p.Activo,
                    p.CUIT,
                    CantidadProductos = _context.Productos.Count(pr => pr.ProveedorId == p.Id && pr.TenantId == tenantId)
                })
                .Cast<dynamic>()
                .ToListAsync();
        }

        public async Task<List<Proveedor>> GetAllAsync()
        {
            var tenantId = GetTenantId();
            return await _context.Proveedores
                .Where(p => p.TenantId == tenantId)
                .OrderByDescending(p => p.Activo)
                .ThenBy(p => p.Nombre)
                .ToListAsync();
        }

        public async Task<Proveedor?> GetByIdAsync(int id)
        {
            var tenantId = GetTenantId();
            return await _context.Proveedores
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
        }

        public async Task CreateAsync(Proveedor proveedor)
        {
            proveedor.TenantId = GetTenantId();
            proveedor.FechaAlta = DateTime.Now;
            proveedor.Activo = true;
            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Proveedor proveedor)
        {
            var tenantId = GetTenantId();
            var dbProveedor = await _context.Proveedores
                .FirstOrDefaultAsync(p => p.Id == proveedor.Id && p.TenantId == tenantId);

            if (dbProveedor != null)
            {
                dbProveedor.Nombre = proveedor.Nombre;
                dbProveedor.Telefono = proveedor.Telefono;
                dbProveedor.Email = proveedor.Email;
                dbProveedor.Direccion = proveedor.Direccion;
                dbProveedor.CUIT = proveedor.CUIT;
                dbProveedor.Activo = proveedor.Activo;
                
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            var tenantId = GetTenantId();
            var proveedor = await _context.Proveedores
                .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

            if (proveedor != null)
            {
                proveedor.Activo = false;
                await _context.SaveChangesAsync();
            }
        }
    }
}

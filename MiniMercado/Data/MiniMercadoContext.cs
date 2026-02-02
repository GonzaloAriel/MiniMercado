using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Models;

namespace MiniMercado.Data
{
    public class MiniMercadoContext
        : IdentityDbContext<ApplicationUser>
    {
        public MiniMercadoContext(DbContextOptions<MiniMercadoContext> options)
            : base(options)
        {
        }

        public DbSet<Producto> Productos { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<VentaDetalle> VentaDetalles { get; set; }
        public DbSet<MovimientoStock> MovimientosStock { get; set; }
    }
}

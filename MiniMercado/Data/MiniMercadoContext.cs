using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Models;
using MiniMercado.Services;

namespace MiniMercado.Data
{
    public class MiniMercadoContext : IdentityDbContext<ApplicationUser>
    {
        // 1. Propiedad pública para el TenantId actual
        public int? CurrentTenantId { get; set; }

        public MiniMercadoContext(
            DbContextOptions<MiniMercadoContext> options,
            ITenantService tenantService)
            : base(options)
        {
            // 2. Asignación directa en el constructor
            CurrentTenantId = tenantService.GetTenantId();
        }

        public DbSet<Producto> Productos { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<VentaDetalle> VentaDetalles { get; set; }
        public DbSet<MovimientoStock> MovimientosStock { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Auditoria> Auditorias { get; set; }
        public DbSet<Rubro> Rubros { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<PagoFiado> PagosFiado { get; set; }
        public DbSet<CobroTenant> CobrosTenants { get; set; }
        public DbSet<ProductoBase> ProductosBase { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }
        public DbSet<LoteProducto> LotesProducto { get; set; }
        public DbSet<Caja> Cajas { get; set; }
        public DbSet<Configuracion> Configuraciones { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<ProductoProveedor> ProductoProveedores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 3. FILTROS GLOBALES (MANUALES Y ESTABLES)
            // Lógica: Si CurrentTenantId es null (AdminGlobal), devuelve true.
            // De lo contrario, compara TenantId.

            modelBuilder.Entity<Producto>().HasQueryFilter(e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId);
            modelBuilder.Entity<Venta>().HasQueryFilter(e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId);
            modelBuilder.Entity<VentaDetalle>().HasQueryFilter(e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId);
            modelBuilder.Entity<MovimientoStock>().HasQueryFilter(e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId);
            modelBuilder.Entity<Rubro>().HasQueryFilter(e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId);
            modelBuilder.Entity<Cliente>().HasQueryFilter(e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId);
            modelBuilder.Entity<PagoFiado>().HasQueryFilter(e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId);
            modelBuilder.Entity<CobroTenant>().HasQueryFilter(e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId);
            modelBuilder.Entity<Notificacion>().HasQueryFilter(e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId);
            modelBuilder.Entity<LoteProducto>().HasQueryFilter(e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId);
            modelBuilder.Entity<Caja>().HasQueryFilter(e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId);
            modelBuilder.Entity<Configuracion>().HasQueryFilter(e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId);
            modelBuilder.Entity<Proveedor>().HasQueryFilter(e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId);

            // CONFIGURACIONES FLUENT API EXISTENTES

            modelBuilder.Entity<ProductoProveedor>()
                .HasKey(pp => new { pp.ProductoId, pp.ProveedorId });

            modelBuilder.Entity<ProductoProveedor>()
                .HasOne(pp => pp.Producto)
                .WithMany(p => p.Proveedores)
                .HasForeignKey(pp => pp.ProductoId);

            modelBuilder.Entity<ProductoProveedor>()
                .HasOne(pp => pp.Proveedor)
                .WithMany()
                .HasForeignKey(pp => pp.ProveedorId);

            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Proveedor)
                .WithMany(pr => pr.Productos)
                .HasForeignKey(p => p.ProveedorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Proveedor>()
                .HasOne(p => p.Tenant)
                .WithMany()
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Caja>()
                .HasOne(c => c.Tenant)
                .WithMany()
                .HasForeignKey(c => c.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Caja>()
                .HasOne(c => c.Usuario)
                .WithMany()
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Configuracion>()
                .HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(c => c.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LoteProducto>()
                .HasOne(l => l.Producto)
                .WithMany()
                .HasForeignKey(l => l.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LoteProducto>()
                .HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(l => l.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notificacion>()
                .HasOne(n => n.Tenant)
                .WithMany()
                .HasForeignKey(n => n.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CobroTenant>()
                .HasOne(c => c.Tenant)
                .WithMany(t => t.Cobros)
                .HasForeignKey(c => c.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Tenant)
                .WithMany()
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Tenant)
                .WithMany()
                .HasForeignKey(v => v.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VentaDetalle>()
                .HasOne(vd => vd.Tenant)
                .WithMany()
                .HasForeignKey(vd => vd.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovimientoStock>()
                .HasOne(ms => ms.Tenant)
                .WithMany()
                .HasForeignKey(ms => ms.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Rubro>()
                .HasMany(r => r.Productos)
                .WithOne(p => p.Rubro)
                .HasForeignKey(p => p.RubroId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Rubro>()
                .HasOne(r => r.Tenant)
                .WithMany()
                .HasForeignKey(r => r.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Venta>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(v => v.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cliente>()
                .HasOne(c => c.Tenant)
                .WithMany()
                .HasForeignKey(c => c.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PagoFiado>()
                .HasOne(p => p.Cliente)
                .WithMany(c => c.Pagos)
                .HasForeignKey(p => p.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PagoFiado>()
                .HasOne(p => p.Tenant)
                .WithMany()
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // ÍNDICES
            modelBuilder.Entity<Tenant>().HasIndex(t => t.FechaVencimiento);
            modelBuilder.Entity<CobroTenant>().HasIndex(c => c.TenantId);
            modelBuilder.Entity<CobroTenant>().HasIndex(c => c.FechaPago);
            modelBuilder.Entity<ProductoBase>().HasIndex(p => p.CodigoBarras).IsUnique();

            modelBuilder.Entity<Producto>()
                .HasIndex(p => new { p.TenantId, p.CodigoBarra })
                .IsUnique();

            modelBuilder.Entity<Producto>()
                .HasOne(p => p.ProductoBase)
                .WithMany()
                .HasForeignKey(p => p.ProductoBaseId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
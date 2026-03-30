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
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Auditoria> Auditorias { get; set; }
        public DbSet<Rubro> Rubros { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<PagoFiado> PagosFiado { get; set; }
        public DbSet<CobroTenant> CobrosTenants { get; set; }
        public DbSet<ProductoBase> ProductosBase { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            // Rubro - Producto : Uno a Muchos
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

            // Cliente - Venta : Uno a Muchos

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

            // --- OPTIMIZACIÓN: ÍNDICES ---

            // Tenant
            modelBuilder.Entity<Tenant>()
                .HasIndex(t => t.FechaVencimiento);

            // CobroTenant
            modelBuilder.Entity<CobroTenant>()
                .HasIndex(c => c.TenantId);

            modelBuilder.Entity<CobroTenant>()
                .HasIndex(c => c.FechaPago);

            // ProductoBase (Catálogo Global)
            modelBuilder.Entity<ProductoBase>()
                .HasIndex(p => p.CodigoBarras)
                .IsUnique();

            // Producto -> ProductoBase (Opcional)
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.ProductoBase)
                .WithMany()
                .HasForeignKey(p => p.ProductoBaseId)
                .OnDelete(DeleteBehavior.SetNull);

           
        }
    }
}
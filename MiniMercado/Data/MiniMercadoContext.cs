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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            modelBuilder.Entity<Producto>()
                        .HasQueryFilter(p =>
                            IsGlobalAdmin() || p.TenantId == GetCurrentTenantId()
                        );


            modelBuilder.Entity<Venta>()
                        .HasQueryFilter(v =>
                            IsGlobalAdmin() || v.TenantId == GetCurrentTenantId()
                         );



            modelBuilder.Entity<VentaDetalle>()
                        .HasQueryFilter(vd =>
                            IsGlobalAdmin() || vd.TenantId == GetCurrentTenantId()
                         );



            modelBuilder.Entity<MovimientoStock>()
                        .HasQueryFilter(ms =>
                            IsGlobalAdmin() || ms.TenantId == GetCurrentTenantId()
                         );

            modelBuilder.Entity<Venta>()
                        .HasOne<ApplicationUser>()
                        .WithMany()
                        .HasForeignKey(v => v.UsuarioId)
                        .OnDelete(DeleteBehavior.Restrict
                        );

        }

        private readonly IHttpContextAccessor _httpContextAccessor;

        public MiniMercadoContext(
            DbContextOptions<MiniMercadoContext> options,
            IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private int? GetCurrentTenantId()
        {
            var tenantIdClaim = _httpContextAccessor
                .HttpContext?
                .User?
                .FindFirst("TenantId");

            if (tenantIdClaim == null)
                return null;

            return int.Parse(tenantIdClaim.Value);
        }

        private bool IsGlobalAdmin()
        {
            return _httpContextAccessor
                .HttpContext?
                .User?
                .IsInRole("AdminGlobal") ?? false;
        }

    }
}

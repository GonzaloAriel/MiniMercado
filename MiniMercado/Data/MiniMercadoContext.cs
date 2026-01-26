using Microsoft.EntityFrameworkCore;
using MiniMercado.Models;
using System.Collections.Generic;

namespace MiniMercado.Data
{
    public class MiniMercadoContext : DbContext
    {
        public MiniMercadoContext(DbContextOptions<MiniMercadoContext> options)
            : base(options)
        {
        }

        public DbSet<Producto> Productos { get; set; }
        public DbSet<MovimientoStock> MovimientosStock { get; set; }

    }
}

using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MiniMercado.Tests
{
    /// <summary>
    /// Verifica la infraestructura de los tests: que Migrate() construye una base
    /// completa, que los query filters aisan por tenant y que Dispose la elimina.
    /// Si esto falla, ningun otro test de este proyecto es creible.
    /// </summary>
    public class BaseDePruebaTests : BaseDePrueba
    {
        [Fact]
        public void MigraTodasLasTablas()
        {
            // ADO.NET directo: DatabaseFacade.SqlQueryRaw es EF Core 7, y el objetivo
            // justamente es verificar el esquema real que dejaron las migraciones.
            var tablas = new List<string>();
            var conexion = Context.Database.GetDbConnection();

            using (var comando = conexion.CreateCommand())
            {
                if (conexion.State != System.Data.ConnectionState.Open) conexion.Open();

                comando.CommandText =
                    "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
                using var lector = comando.ExecuteReader();
                while (lector.Read()) tablas.Add(lector.GetString(0));
            }

            Assert.Contains("Ventas", tablas);
            Assert.Contains("VentaDetalles", tablas);
            Assert.Contains("Cajas", tablas);
            Assert.Contains("CobrosTenants", tablas);
            Assert.Contains("Productos", tablas);
        }

        [Fact]
        public void ElEsquemaPermiteEscribirYLeerUnTenant()
        {
            // Ida y vuelta: si falta una columna o un tipo quedo mal en la migracion,
            // esto falla antes que cualquier test de negocio.
            var id = SembrarTenant();

            using var contexto = NuevoContexto(id);
            var guardado = contexto.Tenants.Single();

            Assert.Equal(id, guardado.Id);
            Assert.Equal("Tenant de prueba", guardado.NombreComercio);
            Assert.Equal(5000m, guardado.PrecioMensual);
        }

        [Fact]
        public void ElQueryFilterAislaLasVentasPorTenant()
        {
            // Contexto sin tenant (AdminGlobal) ve las dos ventas.
            var ventaA = CrearVenta(SembrarTenant("A"));
            var ventaB = CrearVenta(SembrarTenant("B"));

            Assert.Equal(2, Context.Ventas.Count());

            // Cada tenant solo ve la suya.
            using var contextoA = NuevoContexto(ventaA);
            Assert.Equal(1, contextoA.Ventas.Count());
            Assert.Equal(ventaA, contextoA.Ventas.Single().TenantId);

            using var contextoB = NuevoContexto(ventaB);
            Assert.Equal(1, contextoB.Ventas.Count());
            Assert.Equal(ventaB, contextoB.Ventas.Single().TenantId);

            // Y un tenant sin ventas no ve nada.
            using var contextoC = NuevoContexto(9999);
            Assert.Empty(contextoC.Ventas.ToList());
        }

        private int CrearVenta(int tenantId)
        {
            Context.Ventas.Add(new MiniMercado.Models.Venta
            {
                TenantId = tenantId,
                Total = 100m,
                Fecha = DateTime.Now
            });
            Context.SaveChanges();
            return tenantId;
        }
    }
}
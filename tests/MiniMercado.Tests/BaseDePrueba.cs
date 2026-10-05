using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MiniMercado.Data;
using MiniMercado.Services;

namespace MiniMercado.Tests
{
    /// <summary>
    /// Stub de tenant para los tests. <see cref="MiniMercadoContext"/> lo exige en el
    /// constructor y cachea el valor en CurrentTenantId, que es el que usan los query
    /// filters globales.
    ///
    /// Devolver null equivale al rol AdminGlobal (D-014): es el unico rol sin TenantId.
    /// </summary>
    public class TenantServiceStub : ITenantService
    {
        public int? TenantId { get; }

        public TenantServiceStub(int? tenantId = null)
        {
            TenantId = tenantId;
        }

        public int? GetTenantId() => TenantId;
    }

    /// <summary>
    /// Crea una base de datos descartable en el SQL Server local y la construye
    /// SOLO con migraciones (<c>Database.Migrate()</c>), que es el mismo camino que
    /// usa la app. No se usa EnsureCreated: ademas de no servir para comparar contra el
    /// modelo (ver P-025), en EF Core 6 aplica las migraciones si el contexto las tiene.
    ///
    /// El connection string llega por variable de entorno, nunca por appsettings.json
    /// (P-010). El nombre de la base lleva un GUID para que dos corridas simultaneas
    /// no se pisen.
    /// </summary>
    public abstract class BaseDePrueba : IDisposable
    {
        private readonly string _connectionString;
        private readonly string _nombreBase;

        protected BaseDePrueba()
        {
            _nombreBase = "MiniMercadoDB_test_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            _connectionString = CambiarBase(ConnectionStringMaestro(), _nombreBase);

            Context = NuevoContexto(null);
            Context.Database.Migrate();
        }

        /// <summary>
        /// Contexto sin tenant (equivalente a AdminGlobal, D-014). Se usa para migrar y
        /// para sembrar datos globales.
        /// </summary>
        protected MiniMercadoContext Context { get; }

        /// <summary>
        /// Crea un contexto nuevo con el tenant indicado. Hace falta uno por test porque
        /// MiniMercadoContext cachea el TenantId en el constructor: si el Id del tenant
        /// recien sembrado no se conoce hasta despues del SaveChanges, no hay forma de
        /// tener un contexto ya filtrado por el desde el principio.
        /// </summary>
        protected MiniMercadoContext NuevoContexto(int? tenantId)
        {
            return NuevoContexto(tenantId, Array.Empty<IInterceptor>());
        }

        /// <summary>
        /// Igual que <see cref="NuevoContexto(int?)"/> pero con interceptores, para poder
        /// simular un fallo en un SaveChanges concreto (ver P-033).
        /// </summary>
        protected MiniMercadoContext NuevoContexto(int? tenantId, params IInterceptor[] interceptors)
        {
            var builder = new DbContextOptionsBuilder<MiniMercadoContext>()
                .UseSqlServer(_connectionString);

            if (interceptors.Length > 0) builder.AddInterceptors(interceptors);

            return new MiniMercadoContext(builder.Options, new TenantServiceStub(tenantId));
        }

        /// <summary>
        /// Cajas.UsuarioId tiene FK a AspNetUsers, asi que sembrar una caja exige que el
        /// usuario exista de verdad o el INSERT revienta con DbUpdateException.
        /// </summary>
        protected void SembrarUsuario(string userId)
        {
            Context.Users.Add(new MiniMercado.Models.ApplicationUser
            {
                Id = userId,
                UserName = userId,
                Email = userId + "@test.local",
                NormalizedUserName = userId.ToUpperInvariant(),
                NormalizedEmail = (userId + "@test.local").ToUpperInvariant()
            });
            Context.SaveChanges();
        }

        /// <summary>
        /// Siembra un tenant y devuelve su Id. La base esta recien migrada y vacia, asi
        /// que el Id lo asigna el identity de la base.
        /// </summary>
        protected int SembrarTenant(string nombre = "Tenant de prueba")
        {
            Context.Tenants.Add(new MiniMercado.Models.Tenant
            {
                NombreComercio = nombre,
                Activo = true,
                PrecioMensual = 5000m,
                Plan = "Mensual"
            });
            Context.SaveChanges();

            return Context.Tenants.Single(t => t.NombreComercio == nombre).Id;
        }

        private static string ConnectionStringMaestro()
        {
            var maestro = Environment.GetEnvironmentVariable("ConnectionStrings__MiniMercadoDB");
            if (string.IsNullOrWhiteSpace(maestro))
                throw new InvalidOperationException(
                    "Falta la variable de entorno ConnectionStrings__MiniMercadoDB. " +
                    "Los tests corren contra SQL Server real, no en memoria: " +
                    "apuntala a un servidor cualquiera y el nombre de la base se reemplaza " +
                    "por uno descartable. Ver 00_MEMORIA.md.");

            return maestro;
        }

        private static string CambiarBase(string connectionString, string nombreBase)
        {
            var partes = connectionString.Split(';').ToList();

            for (var i = 0; i < partes.Count; i++)
            {
                if (partes[i].TrimStart().StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                {
                    partes[i] = "Database=" + nombreBase;
                    return string.Join(";", partes);
                }
            }

            partes.Add("Database=" + nombreBase);
            return string.Join(";", partes);
        }

        public void Dispose()
        {
            // Hay que dropear desde master: no se puede DROP DATABASE la base a la que
            // esta conectada la sesion actual. El nombre viene de un GUID generado aca
            // mismo, asi que se interpola sin riesgo de inyeccion.
            Context.Dispose();

            using var conexion = new SqlConnection(CambiarBase(_connectionString, "master"));
            conexion.Open();

            using var comando = conexion.CreateCommand();
            comando.CommandText =
                "IF DB_ID(@nombre) IS NOT NULL BEGIN " +
                "  ALTER DATABASE [" + _nombreBase + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                "  DROP DATABASE [" + _nombreBase + "]; " +
                "END";

            comando.Parameters.AddWithValue("@nombre", _nombreBase);
            comando.ExecuteNonQuery();
        }
    }
}
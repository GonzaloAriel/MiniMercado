using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Controllers;
using MiniMercado.Data;
using MiniMercado.Services;
using Xunit;

namespace MiniMercado.Tests
{
    /// <summary>
    /// P-007: 85 sitios desenvolvian el claim TenantId con .Value a secas. Sin claim
    /// (AdminGlobal, usuario sin credencial) eso lanzaba excepcion y el usuario veia
    /// un 500. El cierre es un helper unico que falla cerrado
    /// (<c>TenantServiceExtensions.TryGetTenantId</c>): si no hay claim, el caller
    /// deniega con Unauthorized antes de tocar cualquier dato.
    ///
    /// Estos tests fijan las dos mitades del cierre:
    ///   1. comportamiento - sin tenant la accion se niega, no revienta;
    ///   2. guardia de fuente - <c>GetTenantId().Value</c> no puede volver a aparecer.
    ///
    /// No necesitan base de datos: la guardia corre antes de cualquier consulta. Si un
    /// dia alguien mueve la guardia despues de tocar la base, el test se pone rojo de
    /// cualquier manera (primero por excepcion, y si no, porque la connection string
    /// falsa no conecta).
    /// </summary>
    public class SinTenantIdTests
    {
        // Connection string falsa a proposito: nunca se abre una conexion porque las
        // guardias devuelven antes de la primera consulta.
        private const string ConnectionStringFalsa =
            "Server=127.0.0.1,1433;Database=MiniMercadoDB_no_existe;User Id=sa;Password=falsa;TrustServerCertificate=True;";

        private static MiniMercadoContext Contexto()
        {
            var opciones = new DbContextOptionsBuilder<MiniMercadoContext>()
                .UseSqlServer(ConnectionStringFalsa)
                .Options;

            return new MiniMercadoContext(opciones, new TenantServiceStub(null));
        }

        private static HttpContextAccessor Accessor()
        {
            return new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        }

        private static T SinClaims<T>(T controller) where T : ControllerBase
        {
            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ControllerActionDescriptor());

            controller.ControllerContext = new ControllerContext(actionContext);
            return controller;
        }

        // --- Comportamiento: sin tenant la accion se niega, no lanza ---

        [Fact]
        public void ClienteEditSinTenantSeNiegaEnVezDeExplotar()
        {
            var contexto = Contexto();
            var tenant = new TenantServiceStub(null);
            var controller = new ClienteController(contexto, tenant,
                new AuditoriaService(contexto, tenant, Accessor()));

            var resultado = SinClaims(controller).Edit(1);

            Assert.IsType<UnauthorizedResult>(resultado);
        }

        [Fact]
        public void CajaAbrirSinTenantSeNiegaEnVezDeExplotar()
        {
            var controller = new CajaController(Contexto(), new TenantServiceStub(null));

            var resultado = SinClaims(controller).AbrirCaja();

            Assert.IsType<UnauthorizedResult>(resultado);
        }

        [Fact]
        public void RubroEditSinTenantSeNiegaEnVezDeExplotar()
        {
            var contexto = Contexto();
            var tenant = new TenantServiceStub(null);
            var controller = new RubroController(contexto, tenant,
                new AuditoriaService(contexto, tenant, Accessor()));

            var resultado = SinClaims(controller).Edit(1);

            Assert.IsType<UnauthorizedResult>(resultado);
        }

        [Fact]
        public async Task ProductosApiBuscarSinTenantSeNiegaEnVezDeExplotar()
        {
            var controller = new ProductosApiController(Contexto(), new TenantServiceStub(null));

            var resultado = await SinClaims(controller).Buscar("telefonos");

            Assert.IsType<UnauthorizedResult>(resultado);
        }

        // --- AuditoriaService: no puede devolver IActionResult, falla con mensaje claro ---

        [Fact]
        public void AuditoriaSinTenantFallaConMensajeClaro()
        {
            var servicio = new AuditoriaService(Contexto(), new TenantServiceStub(null), Accessor());

            var excepcion = Assert.Throws<InvalidOperationException>(
                () => servicio.Registrar("Crear", "Clientes", 1, "alta de prueba"));

            Assert.Contains("P-007", excepcion.Message);
            Assert.Contains("TenantId", excepcion.Message);
        }

        // --- Guardia de fuente: el patron no puede volver ---

        [Fact]
        public void NingunaFuenteVuelveADesenvolverElClaimDeTenantConValue()
        {
            var directorio = Path.Combine(AppContext.BaseDirectory, "fuente", "src");
            Assert.True(Directory.Exists(directorio),
                $"No se encontro la copia de fuentes en {directorio}; revisar el ItemGroup " +
                "'fuente\\src' del csproj de tests.");

            var infractores = Directory
                .GetFiles(directorio, "*.cs", SearchOption.AllDirectories)
                .Where(archivo => File.ReadAllText(archivo).Contains("GetTenantId().Value"))
                .Select(Path.GetFileName)
                .ToList();

            Assert.True(infractores.Count == 0,
                "Volvio a aparecer GetTenantId().Value en: " + string.Join(", ", infractores) +
                ". Hay que usar TenantServiceExtensions.TryGetTenantId y denegar de forma " +
                "explicita si no hay claim (P-007).");
        }
    }
}

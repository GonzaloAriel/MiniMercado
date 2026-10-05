using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MiniMercado.Controllers;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Services;
using Xunit;

namespace MiniMercado.Tests
{
    /// <summary>
    /// P-033: la venta y su pago tienen que ser atomicos.
    ///
    /// Antes el POST de venta guardaba en tres SaveChanges sin transaccion: la venta, el
    /// pago y la auditoria. Si fallaba el del medio, la venta quedaba registrada sin pago,
    /// con el stock ya descontado y el total ya acreditado a la caja. El cajon receiptiaba
    /// la plata y el sistema no la veia.
    ///
    /// El fallo se inyecta con <see cref="InterceptorFallaEnLaNSave"/>, que tira en la
    /// llamada N de SaveChangesAsync. Las aserciones se hacen siempre sobre un contexto
    /// nuevo: el change tracker del que fallo sigue con las entidades en memoria, y lo que
    /// importa es que quedo en la base.
    /// </summary>
    public class AtomicidadVentaTests : BaseDePrueba
    {
        private const string Usuario = "vendedor-1";

        private int _tenantId;
        private Producto _producto;

        private VentaController CrearController(params IInterceptor[] interceptors)
        {
            var contexto = NuevoContexto(_tenantId, interceptors);
            var tenant = new TenantServiceStub(_tenantId);
            var accessor = new HttpContextAccessor { HttpContext = NuevoHttpContext(Usuario) };

            return new VentaController(
                    contexto,
                    tenant,
                    new AuditoriaService(contexto, tenant, accessor),
                    accessor,
                    new ProductoService(contexto),
                    new NotificacionService(contexto, tenant))
                .ConClaims(Usuario);
        }

        private static DefaultHttpContext NuevoHttpContext(string userId)
        {
            var identidad = new System.Security.Claims.ClaimsIdentity(
                new[]
                {
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.NameIdentifier, userId),
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.Name, userId),
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.Role, "Caja")
                },
                "TestAuth");

            return new DefaultHttpContext { User = new System.Security.Claims.ClaimsPrincipal(identidad) };
        }

        /// <summary>
        /// Deja el tenant con configuracion UsarCaja = true y una caja ABIERTA ya
        /// abierta. Asi la secuencia de guardados es fija y los indices del interceptor
        /// no dependen del modo:
        ///
        ///   async #1 -> venta + detalles + stock + movimientos + total de la caja
        ///   async #2 -> pago
        ///   sync      -> AuditoriaService.Registrar
        ///
        /// Importante: en modo automatico (UsarCaja = false) hay un async #0 extra que
        /// guarda la caja nueva, y los indices se corren. Por eso estos tests no usan ese
        /// modo; el camino automatico se cubre en VentaPosTests.
        /// </summary>
        private int Preparar(decimal stock = 10m)
        {
            _tenantId = SembrarTenant();
            SembrarUsuario(Usuario);

            var contexto = NuevoContexto(_tenantId);
            contexto.Configuraciones.Add(new Configuracion { TenantId = _tenantId, UsarCaja = true });
            contexto.Productos.Add(new Producto
            {
                TenantId = _tenantId,
                CodigoBarra = "TEST01",
                Nombre = "Producto",
                Precio = 100m,
                Stock = stock,
                EsPorPeso = false
            });

            var caja = new Caja
            {
                TenantId = _tenantId,
                UsuarioId = Usuario,
                FechaApertura = DateTime.Now,
                MontoInicial = 5000m,
                TotalVentas = 0m,
                Estado = "ABIERTA"
            };
            contexto.Cajas.Add(caja);
            contexto.SaveChanges();

            _producto = NuevoContexto(_tenantId).Productos.AsNoTracking().Single();
            return caja.Id;
        }

        private List<VentaDetalle> Lineas() => new()
        {
            new VentaDetalle { ProductoId = _producto.Id, Cantidad = 2m, PrecioUnitario = 0m }
        };

        private MiniMercadoContext ContextoDeVerificacion() => NuevoContexto(_tenantId);

        // --- El caso central de P-033 ---

        [Fact]
        public async Task SiFallaElPagoNoQuedaNingunaVenta()
        {
            Preparar();

            // 1 = venta + stock + movimientos, 2 = pago. Falla al guardar el pago, que es
            // exactamente el hueco que dejaba la venta huérfana.
            var interceptor = new InterceptorFallaEnLaNSave(nesima: 2);
            var controller = CrearController(interceptor);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => controller.Create(Lineas(), false, null, "EFECTIVO", 200m));

            Assert.Equal(2, interceptor.Llamadas);

            using var verificacion = ContextoDeVerificacion();

            Assert.Empty(verificacion.Ventas.AsNoTracking().ToList());
            Assert.Empty(verificacion.VentaDetalles.AsNoTracking().ToList());
            Assert.Empty(verificacion.Pagos.AsNoTracking().ToList());
            Assert.Empty(verificacion.MovimientosStock.AsNoTracking().ToList());
            Assert.Empty(verificacion.Auditorias.AsNoTracking().ToList());

            // El stock tampoco se toco: si se hubiera descontado sin venta, el inventario
            // queda desincronizado de forma silenciosa.
            Assert.Equal(10m, verificacion.Productos.AsNoTracking().Single().Stock);
        }

        [Fact]
        public async Task SiFallaElPagoLaCajaNoSeAcredita()
        {
            var cajaId = Preparar();

            var controller = CrearController(new InterceptorFallaEnLaNSave(nesima: 2));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => controller.Create(Lineas(), false, null, "EFECTIVO", 200m));

            using var verificacion = ContextoDeVerificacion();

            // Si la venta se hubiera guardado, el cierre de caja contaria 200 que nunca
            // entraron al cajon.
            Assert.Equal(0m, verificacion.Cajas.AsNoTracking().Single(c => c.Id == cajaId).TotalVentas);
        }

        [Fact]
        public async Task SiFallaLaAuditoriaTampocoQuedaVenta()
        {
            // El tercer SaveChanges es el sincrono de AuditoriaService.Registrar. Tambien
            // tiene que caer dentro de la transaccion: si la auditoria falla, la venta no
            // se registro y queda sin rastro.
            Preparar();

            var controller = CrearController(new InterceptorFallaEnLaNSave(nesima: 3));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => controller.Create(Lineas(), false, null, "EFECTIVO", 200m));

            using var verificacion = ContextoDeVerificacion();

            Assert.Empty(verificacion.Ventas.AsNoTracking().ToList());
            Assert.Empty(verificacion.Pagos.AsNoTracking().ToList());
            Assert.Equal(10m, verificacion.Productos.AsNoTracking().Single().Stock);
        }

        [Fact]
        public async Task SiFallaElPrimerGuardadoNoQuedaNada()
        {
            // OJO: este test pasa con y sin transaccion, a proposito. Si el primer
            // guardado es el que falla, no hay nada previo que revertir. Queda como
            // guarda de que el fallo temprano no deja restos, no como detector del bug.
            // Los que si lo detectan son los tres de arriba: al desactivar la
            // transaccion se ponen rojos y con ella vuelven a verde.
            Preparar();

            var controller = CrearController(new InterceptorFallaEnLaNSave(nesima: 1));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => controller.Create(Lineas(), false, null, "EFECTIVO", 200m));

            using var verificacion = ContextoDeVerificacion();

            Assert.Empty(verificacion.Ventas.AsNoTracking().ToList());
            Assert.Empty(verificacion.MovimientosStock.AsNoTracking().ToList());
            Assert.Equal(10m, verificacion.Productos.AsNoTracking().Single().Stock);
        }

        // --- Y que no se haya roto el camino feliz ---

        [Fact]
        public async Task SinFalloSeGuardaVentaPagoYAuditoria()
        {
            Preparar();

            // Interceptor que nunca dispara: cuenta las llamadas y no tira.
            var controller = CrearController();

            var resultado = await controller.Create(Lineas(), false, null, "EFECTIVO", 200m);

            Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToActionResult>(resultado);

            using var verificacion = ContextoDeVerificacion();

            var venta = verificacion.Ventas.AsNoTracking().Single();
            var pago = verificacion.Pagos.AsNoTracking().Single();

            Assert.Equal(200m, venta.Total);
            Assert.Equal(venta.Id, pago.VentaId);
            Assert.Equal(200m, pago.Monto);
            Assert.Single(verificacion.Auditorias.AsNoTracking().ToList());
            Assert.Equal(8m, verificacion.Productos.AsNoTracking().Single().Stock);
        }

        [Fact]
        public async Task UnaVentaFiadaGuardaVentaSinPagoYSinFalla()
        {
            // Las ventas fiadas no generan pago, asi que solo hay un SaveChanges. Con la
            // transaccion tienen que seguir guardandose bien.
            Preparar();

            var controller = CrearController();

            await controller.Create(Lineas(), true, null, null, null);

            using var verificacion = ContextoDeVerificacion();

            Assert.Single(verificacion.Ventas.AsNoTracking().ToList());
            Assert.Empty(verificacion.Pagos.AsNoTracking().ToList());
            Assert.Single(verificacion.Auditorias.AsNoTracking().ToList());
        }

        [Fact]
        public async Task UnaVentaRechazadaPorValidacionNoDejaRestosDeLaTransaccion()
        {
            // El camino de return View() de las validaciones tambien tiene que revertir:
            // para entonces el loop ya toco lotes, stock y movimientos en memoria.
            Preparar(stock: 10m);

            var controller = CrearController();

            var resultado = await controller.Create(
                new List<VentaDetalle>
                {
                    new VentaDetalle { ProductoId = _producto.Id, Cantidad = 2m, PrecioUnitario = 0m },
                    new VentaDetalle { ProductoId = 0, Cantidad = 1m, PrecioUnitario = 0m }
                },
                false, null, "EFECTIVO", null);

            Assert.IsType<Microsoft.AspNetCore.Mvc.ViewResult>(resultado);

            using var verificacion = ContextoDeVerificacion();

            Assert.Empty(verificacion.Ventas.AsNoTracking().ToList());
            Assert.Empty(verificacion.MovimientosStock.AsNoTracking().ToList());
            Assert.Equal(10m, verificacion.Productos.AsNoTracking().Single().Stock);
        }
    }
}
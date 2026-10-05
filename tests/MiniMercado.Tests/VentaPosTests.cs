using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Controllers;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Services;
using Xunit;

namespace MiniMercado.Tests
{
    /// <summary>
    /// Flujo 3 de 3 de P-017: venta del POS.
    ///
    /// A diferencia de los otros dos flujos, aca toda la logica esta inline en el action
    /// Create (VentaController.cs:140-326): calculo del total, consumo FIFO de lotes,
    /// descuento de stock, actualizacion de caja y registro del pago. No hay service que
    /// probar, asi que se instancia el controller con sus 6 dependencias reales.
    ///
    /// CONVENCION INVERTIDA DE Activa: en todo el codigo Venta.Activa = false significa
    /// "venta vigente" y Activa = true significa "anulada". No es un error: es el criterio
    /// que ya usan VentaAdminController (anula poniendo Activa = true),
    /// AdminDashboardController (!v.Activa para ventas validas) y las consultas T-SQL.
    /// Renombrarla tocaria 20+ lugares y las queries crudas, asi que queda documentada aca
    /// y cubierta por tests para que nadie la "arregle" a medias.
    /// </summary>
    public class VentaPosTests : BaseDePrueba
    {
        private const string Usuario = "vendedor-1";

        private int _tenantId;
        private MiniMercadoContext _contexto;
        private VentaController _controller;

        private void Preparar(bool usarCaja = false)
        {
            _tenantId = SembrarTenant();
            SembrarUsuario(Usuario);
            _contexto = NuevoContexto(_tenantId);

            _contexto.Configuraciones.Add(new Configuracion
            {
                TenantId = _tenantId,
                UsarCaja = usarCaja
            });
            _contexto.SaveChanges();

            var tenant = new TenantServiceStub(_tenantId);
            var accessor = new HttpContextAccessor { HttpContext = NuevoHttpContext(Usuario) };

            _controller = new VentaController(
                    _contexto,
                    tenant,
                    new AuditoriaService(_contexto, tenant, accessor),
                    accessor,
                    new ProductoService(_contexto),
                    new NotificacionService(_contexto, tenant))
                .ConClaims(Usuario);
        }

        private static DefaultHttpContext NuevoHttpContext(string userId)
        {
            var identidad = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, userId),
                    new Claim(ClaimTypes.Role, "Caja")
                },
                "TestAuth");

            return new DefaultHttpContext { User = new ClaimsPrincipal(identidad) };
        }

        private Producto SembrarProducto(
            decimal precio,
            decimal stock = 100m,
            bool esPorPeso = false,
            bool precioEditable = false,
            string nombre = "Producto")
        {
            var producto = new Producto
            {
                TenantId = _tenantId,
                CodigoBarra = Guid.NewGuid().ToString("N").Substring(0, 8),
                Nombre = nombre,
                Precio = precio,
                Stock = stock,
                EsPorPeso = esPorPeso,
                PrecioEditable = precioEditable,
                Activo = true
            };

            _contexto.Productos.Add(producto);
            _contexto.SaveChanges();
            _contexto.ChangeTracker.Clear();

            return producto;
        }

        private LoteProducto NuevoLote(int productoId, decimal cantidad, DateTime vence) => new()
        {
            ProductoId = productoId,
            TenantId = _tenantId,
            Cantidad = cantidad,
            FechaVencimiento = vence,
            FechaIngreso = DateTime.Now
        };

        private static VentaDetalle Linea(int productoId, decimal cantidad, decimal? precio = null) => new()
        {
            ProductoId = productoId,
            Cantidad = cantidad,
            PrecioUnitario = precio ?? 0m
        };

        private Task<IActionResult> CrearVenta(
            List<VentaDetalle> detalles,
            bool esFiado = false,
            int? clienteId = null,
            string? metodoPago = "EFECTIVO",
            decimal? montoRecibido = null) =>
            _controller.Create(detalles, esFiado, clienteId, metodoPago, montoRecibido);

        private Venta VentaUnica()
        {
            return NuevoContexto(_tenantId).Ventas.AsNoTracking().Single();
        }

        private decimal SumaDeDetalles(int ventaId)
        {
            return NuevoContexto(_tenantId).VentaDetalles
                .AsNoTracking()
                .Where(d => d.VentaId == ventaId)
                .Sum(d => d.PrecioUnitario * d.Cantidad);
        }

        // --- Total y consistencia ---

        [Fact]
        public async Task CalculaElTotalDeUnaVentaSimple()
        {
            Preparar();
            var producto = SembrarProducto(precio: 250m);

            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 3) });

            Assert.Equal(750m, VentaUnica().Total);
        }

        [Fact]
        public async Task ElTotalCoincideConLaSumaDeSusDetalles()
        {
            // Invariante que tiene que cumplirse siempre: venta.Total es lo que paga el
            // cliente y lo que suma la caja, mientras que la suma de los detalles es lo que
            // el usuario ve renglón por renglón. Si divergen, el arqueo de caja nunca cierra.
            Preparar();
            var a = SembrarProducto(precio: 100m, nombre: "A");
            var b = SembrarProducto(precio: 175.50m, nombre: "B");

            await CrearVenta(new List<VentaDetalle>
            {
                Linea(a.Id, 2),
                Linea(b.Id, 3)
            });

            var venta = VentaUnica();

            // 2 x 100 + 3 x 175,50 = 726,50
            Assert.Equal(726.50m, venta.Total);
            Assert.Equal(SumaDeDetalles(venta.Id), venta.Total);
        }

        [Fact]
        public async Task UnaCantidadNegativaSeRechazaSinVender()
        {
            // Regresion: el loop de calculo corria sobre TODAS las lineas (linea del
            // total) pero el filtro que arma los detalles exigia Cantidad > 0. Con una
            // cantidad negativa el total la restaba y la linea se perdia del detalle, con
            // lo cual venta.Total (0) no coincidia con la suma de sus renglones (1000):
            // la venta quedaba registrada como gratis. Ahora la linea se valida antes de
            // tocar stock, lotes ni total, y se rechaza la venta completa.
            Preparar();
            var producto = SembrarProducto(precio: 500m, stock: 10m);

            var resultado = await CrearVenta(new List<VentaDetalle>
            {
                Linea(producto.Id, 2),
                Linea(producto.Id, -2)
            });

            Assert.IsType<ViewResult>(resultado);
            Assert.NotEmpty(_controller.ModelState);

            using var contexto = NuevoContexto(_tenantId);
            Assert.Empty(contexto.Ventas.AsNoTracking().ToList());
            Assert.Empty(contexto.VentaDetalles.AsNoTracking().ToList());
            Assert.Empty(contexto.MovimientosStock.AsNoTracking().ToList());
            Assert.Equal(10m, contexto.Productos.AsNoTracking().Single(p => p.Id == producto.Id).Stock);
        }

        [Fact]
        public async Task UnaCantidadCeroSeRechaza()
        {
            // Una linea en cero tampoco es una venta: antes se sumaba 0 al total y luego
            // se perdia del detalle, dejando un renglon fantasma en el formulario.
            Preparar();
            var producto = SembrarProducto(precio: 500m, stock: 10m);

            var resultado = await CrearVenta(new List<VentaDetalle>
            {
                Linea(producto.Id, 1),
                Linea(producto.Id, 0)
            });

            Assert.IsType<ViewResult>(resultado);
            Assert.Empty(NuevoContexto(_tenantId).Ventas.AsNoTracking().ToList());
        }

        [Fact]
        public async Task UnaLineaSinProductoSeRechazaSinVenderALuego()
        {
            // Regresion: el chequeo de ProductoId nullo (linea 186) no cubre el 0 que puede
            // mandar el formulario. Con ProductoId = 0 la busqueda devuelve null y la
            // desreferencia de producto.PrecioEditable (linea 207) revienta con NRE,
            // devoluyendo un 500 al cajero en mitad de una venta.
            //
            // El rechazo tiene que ser de la venta COMPLETA, igual que hace ya el caso de
            // ProductoId nulo: devolver la vista con el error y no registrar una venta
            // parcial a la que le falta un renglón.
            Preparar();
            var producto = SembrarProducto(precio: 300m);

            var resultado = await CrearVenta(new List<VentaDetalle>
            {
                Linea(producto.Id, 1),
                Linea(productoId: 0, cantidad: 1)
            });

            // Assert.IsType no devuelve el objeto en xunit 2.6, hay que castear aparte.
            Assert.IsType<ViewResult>(resultado);
            Assert.NotNull(((ViewResult)resultado).Model);

            Assert.NotEmpty(_controller.ModelState);
            Assert.Empty(NuevoContexto(_tenantId).Ventas.AsNoTracking().ToList());

            // El stock tampoco se toca: no se veto la venta.
            var stock = NuevoContexto(_tenantId).Productos
                .AsNoTracking().Single(p => p.Id == producto.Id).Stock;
            Assert.Equal(100m, stock);
        }

        [Fact]
        public async Task UnaLineaConProductoDeOtroTenantSeRechaza()
        {
            // Aislamiento multi-tenant: el producto ajeno no existe para este vendedor.
            Preparar();
            var propio = SembrarProducto(precio: 400m, nombre: "Propio");

            var otroTenant = SembrarTenant("Otro");
            using (var contextoOtro = NuevoContexto(otroTenant))
            {
                contextoOtro.Productos.Add(new Producto
                {
                    TenantId = otroTenant,
                    CodigoBarra = "AJENO01",
                    Nombre = "Ajeno",
                    Precio = 9999m,
                    Stock = 50m,
                    EsPorPeso = false
                });
                contextoOtro.SaveChanges();
            }

            var idAjeno = NuevoContexto(otroTenant).Productos
                .AsNoTracking().Single(p => p.CodigoBarra == "AJENO01").Id;

            var resultado = await CrearVenta(new List<VentaDetalle>
            {
                Linea(propio.Id, 1),
                Linea(idAjeno, 1)
            });

            Assert.IsType<ViewResult>(resultado);
            Assert.Empty(NuevoContexto(_tenantId).Ventas.AsNoTracking().ToList());
            Assert.Empty(NuevoContexto(_tenantId).VentaDetalles.AsNoTracking().ToList());
        }

        // --- Precio ---

        [Fact]
        public async Task UsaElPrecioDelProductoCuandoNoEsEditable()
        {
            Preparar();
            var producto = SembrarProducto(precio: 1200m, precioEditable: false);

            // El formulario manda 1 para tratar de imposing un precio propio.
            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 1, precio: 1m) });

            Assert.Equal(1200m, VentaUnica().Total);
        }

        [Fact]
        public async Task AceptaElPrecioEnviadoCuandoEsEditableYLoRedondeaA2Decimales()
        {
            Preparar();
            var producto = SembrarProducto(precio: 1200m, precioEditable: true);

            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 1, precio: 999.567m) });

            Assert.Equal(999.57m, VentaUnica().Total);
        }

        // --- Cantidad ---

        [Fact]
        public async Task TruncaLaCantidadFraccionariaEnProductosSinPeso()
        {
            Preparar();
            var producto = SembrarProducto(precio: 100m, stock: 10m, esPorPeso: false);

            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 2.7m) });

            var venta = VentaUnica();
            using var contexto = NuevoContexto(_tenantId);
            var detalle = contexto.VentaDetalles.AsNoTracking().Single(d => d.VentaId == venta.Id);

            Assert.Equal(2m, detalle.Cantidad);
            Assert.Equal(200m, venta.Total);
            Assert.Equal(8m, contexto.Productos.AsNoTracking().Single(p => p.Id == producto.Id).Stock);
        }

        [Fact]
        public async Task ConservaLaCantidadFraccionariaEnProductosPorPeso()
        {
            Preparar();
            var producto = SembrarProducto(precio: 1000m, stock: 10m, esPorPeso: true);

            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 1.75m) });

            var venta = VentaUnica();
            using var contexto = NuevoContexto(_tenantId);
            var detalle = contexto.VentaDetalles.AsNoTracking().Single(d => d.VentaId == venta.Id);

            Assert.Equal(1.75m, detalle.Cantidad);
            Assert.Equal(1750m, venta.Total);
        }

        // --- Stock y movimientos ---

        [Fact]
        public async Task DescuentaElStockYRegistraElMovimiento()
        {
            Preparar();
            var producto = SembrarProducto(precio: 100m, stock: 10m);

            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 4) });

            using var contexto = NuevoContexto(_tenantId);
            Assert.Equal(6m, contexto.Productos.AsNoTracking().Single(p => p.Id == producto.Id).Stock);

            var movimiento = contexto.MovimientosStock.AsNoTracking().Single(m => m.ProductoId == producto.Id);
            Assert.Equal(4m, movimiento.Cantidad);
            Assert.Equal("VENTA", movimiento.Tipo);
        }

        [Fact]
        public async Task ConsumeLosLotesPorFechaDeVencimiento()
        {
            // FIFO: sale primero el lote que vence antes.
            Preparar();
            var producto = SembrarProducto(precio: 100m, stock: 100m);

            var hoy = DateTime.Today;
            _contexto.LotesProducto.AddRange(
                NuevoLote(producto.Id, 5m, hoy.AddDays(60)),
                NuevoLote(producto.Id, 5m, hoy.AddDays(10)),
                NuevoLote(producto.Id, 5m, hoy.AddDays(30)));
            _contexto.SaveChanges();

            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 7) });

            var lotes = NuevoContexto(_tenantId).LotesProducto
                .AsNoTracking()
                .Where(l => l.ProductoId == producto.Id)
                .OrderBy(l => l.FechaVencimiento)
                .ToList();

            // El que vence en 10 dias (5) se agota y el de 30 dias (5) queda en 3.
            Assert.Equal(0m, lotes[0].Cantidad);
            Assert.Equal(3m, lotes[1].Cantidad);
            Assert.Equal(5m, lotes[2].Cantidad);
        }

        [Fact]
        public async Task VenderMasQueElStockDisponiblePermiteStockNegativo()
        {
            // Comportamiento actual documentado: si los lotes no alcanzan no bloquea la
            // venta y el stock queda negativo (lineas 236-242). Es una decision del
            // negocio; el test lo fija para que un cambio futuro sea deliberado.
            Preparar();
            var producto = SembrarProducto(precio: 100m, stock: 3m);

            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 5) });

            using var contexto = NuevoContexto(_tenantId);
            Assert.Equal(-2m, contexto.Productos.AsNoTracking().Single(p => p.Id == producto.Id).Stock);
            Assert.Equal(500m, contexto.Ventas.AsNoTracking().Single().Total);
        }

        // --- Pago ---

        [Fact]
        public async Task RegistraElPagoEnEfectivoYCalculaElVuelto()
        {
            Preparar();
            var producto = SembrarProducto(precio: 250m);

            await CrearVenta(
                new List<VentaDetalle> { Linea(producto.Id, 2) },
                metodoPago: "efectivo", montoRecibido: 1000m);

            var pago = NuevoContexto(_tenantId).Pagos.AsNoTracking().Single();

            Assert.Equal("EFECTIVO", pago.Metodo);
            Assert.Equal(500m, pago.Monto);
            Assert.Equal(1000m, pago.MontoRecibido);
            Assert.Equal(500m, pago.Vuelto);
        }

        [Fact]
        public async Task SinMontoRecibidoElVueltoEsCero()
        {
            Preparar();
            var producto = SembrarProducto(precio: 250m);

            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 2) });

            var pago = NuevoContexto(_tenantId).Pagos.AsNoTracking().Single();

            Assert.Equal(500m, pago.MontoRecibido);
            Assert.Equal(0m, pago.Vuelto);
        }

        [Fact]
        public async Task EnPagoConTarjetaNoCalculaVuelto()
        {
            Preparar();
            var producto = SembrarProducto(precio: 800m);

            await CrearVenta(
                new List<VentaDetalle> { Linea(producto.Id, 1) },
                metodoPago: "Tarjeta", montoRecibido: 1000m);

            var pago = NuevoContexto(_tenantId).Pagos.AsNoTracking().Single();

            Assert.Equal("TARJETA", pago.Metodo);
            Assert.Null(pago.MontoRecibido);
            Assert.Null(pago.Vuelto);
        }

        [Fact]
        public async Task UnaVentaFiadaNoRegistraPago()
        {
            Preparar();
            var producto = SembrarProducto(precio: 500m);

            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 1) }, esFiado: true);

            using var contexto = NuevoContexto(_tenantId);
            Assert.Empty(contexto.Pagos.AsNoTracking().ToList());

            var venta = contexto.Ventas.AsNoTracking().Single();
            Assert.True(venta.EsFiado);
        }

        [Fact]
        public async Task ElMontoDelPagoCoincideConElTotalDeLaVenta()
        {
            Preparar();
            var producto = SembrarProducto(precio: 175.50m);

            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 3) });

            using var contexto = NuevoContexto(_tenantId);
            var venta = contexto.Ventas.AsNoTracking().Single();
            var pago = contexto.Pagos.AsNoTracking().Single();

            Assert.Equal(venta.Total, pago.Monto);
            Assert.Equal(526.50m, pago.Monto);
        }

        // --- Caja ---

        [Fact]
        public async Task SumaLaVentaATotalVentasDeLaCajaAbierta()
        {
            Preparar(usarCaja: true);

            var caja = new Caja
            {
                TenantId = _tenantId,
                UsuarioId = Usuario,
                FechaApertura = DateTime.Now,
                MontoInicial = 10000m,
                TotalVentas = 2000m,
                Estado = "ABIERTA"
            };
            _contexto.Cajas.Add(caja);
            _contexto.SaveChanges();

            var producto = SembrarProducto(precio: 150m);

            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 4) });

            using var contexto = NuevoContexto(_tenantId);
            var guardada = contexto.Cajas.AsNoTracking().Single(c => c.Id == caja.Id);

            Assert.Equal(2600m, guardada.TotalVentas);
            Assert.Equal(caja.Id, contexto.Ventas.AsNoTracking().Single().CajaId);
        }

        [Fact]
        public async Task SinCajaAbiertaLaVentaQuedaSinCajaAsignada()
        {
            Preparar(usarCaja: true);
            var producto = SembrarProducto(precio: 150m);

            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 1) });

            using var contexto = NuevoContexto(_tenantId);
            var venta = contexto.Ventas.AsNoTracking().Single();

            Assert.Null(venta.CajaId);
            Assert.Equal(150m, venta.Total);
            Assert.Empty(contexto.Cajas.AsNoTracking().ToList());
        }

        [Fact]
        public async Task EnModoAutomaticoReutilizaLaCajaDelDia()
        {
            // Modo simple: las ventas del usuario caen en la caja AUTOMATICA del dia, que
            // se crea sola la primera vez y se reutiliza en las siguientes.
            Preparar(usarCaja: false);
            var producto = SembrarProducto(precio: 100m);

            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 1) });
            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 2) });

            using var contexto = NuevoContexto(_tenantId);
            var caja = contexto.Cajas.AsNoTracking().Single();
            var ventas = contexto.Ventas.AsNoTracking().ToList();

            Assert.Equal(2, ventas.Count);
            Assert.Equal("AUTOMATICA", caja.Estado);
            Assert.Equal(300m, caja.TotalVentas);
            Assert.All(ventas, v => Assert.Equal(caja.Id, v.CajaId));
        }

        // --- Estado y trazabilidad ---

        [Fact]
        public async Task LaVentaRecienCreadaQuedaVigenteNoAnulada()
        {
            // Recordatorio de la convencion invertida: Activa = false significa vigente.
            // VentaAdminController anula poniendo Activa = true.
            Preparar();
            var producto = SembrarProducto(precio: 100m);

            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 1) });

            Assert.False(VentaUnica().Activa);
        }

        [Fact]
        public async Task LaVentaQuedaAuditadaConSuUsuario()
        {
            Preparar();
            var producto = SembrarProducto(precio: 100m);

            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 1) });

            var auditoria = NuevoContexto(_tenantId).Auditorias.AsNoTracking().Single();

            Assert.Equal("Crear", auditoria.Accion);
            Assert.Equal("Ventas", auditoria.Tabla);
            Assert.Equal(Usuario, auditoria.UsuarioId);
            Assert.Equal(_tenantId, auditoria.TenantId);
        }

        [Fact]
        public async Task LosDetallesQuedanEnElTenantCorrecto()
        {
            Preparar();
            var producto = SembrarProducto(precio: 100m);

            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 2) });

            var ventaId = VentaUnica().Id;
            var detalles = NuevoContexto(_tenantId).VentaDetalles
                .AsNoTracking().Where(d => d.VentaId == ventaId).ToList();

            Assert.NotEmpty(detalles);
            Assert.All(detalles, d => Assert.Equal(_tenantId, d.TenantId));
        }

        [Fact]
        public async Task LaVentaNoEsVisibleParaOtroTenant()
        {
            Preparar();
            var producto = SembrarProducto(precio: 100m);

            await CrearVenta(new List<VentaDetalle> { Linea(producto.Id, 1) });

            Assert.Single(NuevoContexto(_tenantId).Ventas.AsNoTracking().ToList());
            Assert.Empty(NuevoContexto(_tenantId + 500).Ventas.AsNoTracking().ToList());
        }
    }
}
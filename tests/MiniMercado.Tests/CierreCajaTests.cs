using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using MiniMercado.Controllers;
using MiniMercado.Data;
using MiniMercado.Models;
using Xunit;

namespace MiniMercado.Tests
{
    /// <summary>
    /// Flujo 2 de 3 de P-017: cierre de caja.
    ///
    /// La formula del saldo esperado es MontoInicial + TotalVentas - TotalEgresos, y
    /// estaba escrita en tres lugares (CajaController.cs:96, CerrarCaja.cshtml:34 y
    /// ResumenCierre.cshtml:37). Estos tests la fijan en un solo punto de verdad para que
    /// no vuelva a divergir en silencio: la vista que el usuario lee tiene que mostrar
    /// exactamente el mismo numero que quedo persistido en Diferencia.
    /// </summary>
    public class CierreCajaTests : BaseDePrueba
    {
        private const string Usuario = "cajero-1";

        private int _tenantId;
        private MiniMercadoContext _contexto;

        private CajaController CrearController()
        {
            _contexto = NuevoContexto(_tenantId);
            return new CajaController(_contexto, new TenantServiceStub(_tenantId)).ConClaims(Usuario);
        }

        private int AbrirCaja(decimal montoInicial, decimal totalVentas = 0m)
        {
            var caja = new Caja
            {
                TenantId = _tenantId,
                UsuarioId = Usuario,
                FechaApertura = DateTime.Now,
                MontoInicial = montoInicial,
                TotalVentas = totalVentas,
                Estado = "ABIERTA"
            };

            _contexto.Cajas.Add(caja);
            _contexto.SaveChanges();

            // Se devuelve el Id del entity framework, no un Single() sobre la base: un
            // test puede abrir mas de una caja para el mismo usuario.
            return caja.Id;
        }

        private void AgregarEgreso(int cajaId, decimal monto, bool anulado = false)
        {
            _contexto.EgresosCaja.Add(new EgresoCaja
            {
                TenantId = _tenantId,
                CajaId = cajaId,
                Monto = monto,
                Categoria = CategoriaEgreso.Otros,
                Descripcion = "Egreso de prueba",
                Anulado = anulado
            });
            _contexto.SaveChanges();
        }

        // --- La formula del saldo esperado ---

        [Fact]
        public async Task SaldoEsperadoEsInicialMasVentasMenosEgresos()
        {
            _tenantId = SembrarTenant();
            SembrarUsuario(Usuario);
            var controller = CrearController();
            var cajaId = AbrirCaja(montoInicial: 10000m, totalVentas: 25000m);
            AgregarEgreso(cajaId, 4000m);

            // 10000 + 25000 - 4000 = 31000, y se entregan 31000 justos.
            await controller.CerrarCaja(cajaId, montoReal: 31000m);

            var caja = NuevoContexto(_tenantId).Cajas.AsNoTracking().Single(c => c.Id == cajaId);
            Assert.Equal(0m, caja.Diferencia);
        }

        [Fact]
        public async Task SinEgresosElEsperadoEsInicialMasVentas()
        {
            _tenantId = SembrarTenant();
            SembrarUsuario(Usuario);
            var controller = CrearController();
            var cajaId = AbrirCaja(montoInicial: 5000m, totalVentas: 7500m);

            await controller.CerrarCaja(cajaId, montoReal: 12500m);

            var caja = NuevoContexto(_tenantId).Cajas.AsNoTracking().Single(c => c.Id == cajaId);
            Assert.Equal(0m, caja.Diferencia);
        }

        [Fact]
        public async Task LosEgresosAnuladosNoAfectanElEsperado()
        {
            // EgresoCaja tiene un query filter que excluye Anulado. Si el filtro no
            // estuviera, un egreso anulado restaria saldo que nunca salio de la caja.
            _tenantId = SembrarTenant();
            SembrarUsuario(Usuario);
            var controller = CrearController();
            var cajaId = AbrirCaja(montoInicial: 10000m, totalVentas: 0m);
            AgregarEgreso(cajaId, 3000m, anulado: true);

            await controller.CerrarCaja(cajaId, montoReal: 10000m);

            var caja = NuevoContexto(_tenantId).Cajas.AsNoTracking().Single(c => c.Id == cajaId);
            Assert.Equal(0m, caja.Diferencia);
        }

        [Fact]
        public async Task LosEgresosDeOtroCajaNoAfectanElEsperado()
        {
            _tenantId = SembrarTenant();
            SembrarUsuario(Usuario);
            var controller = CrearController();
            var cajaId = AbrirCaja(montoInicial: 10000m, totalVentas: 0m);

            var otroCajaId = AbrirCaja(montoInicial: 0m, totalVentas: 0m);
            _contexto.EgresosCaja.Add(new EgresoCaja
            {
                TenantId = _tenantId,
                CajaId = otroCajaId,
                Monto = 9000m,
                Categoria = CategoriaEgreso.Otros,
                Descripcion = "Egreso de otra caja"
            });
            _contexto.SaveChanges();

            await controller.CerrarCaja(cajaId, montoReal: 10000m);

            var caja = NuevoContexto(_tenantId).Cajas.AsNoTracking().Single(c => c.Id == cajaId);
            Assert.Equal(0m, caja.Diferencia);
        }

        // --- La diferencia ---

        [Fact]
        public async Task DiferenciaPositivaCuandoSobraDinero()
        {
            _tenantId = SembrarTenant();
            SembrarUsuario(Usuario);
            var controller = CrearController();
            var cajaId = AbrirCaja(montoInicial: 10000m, totalVentas: 0m);

            await controller.CerrarCaja(cajaId, montoReal: 10500m);

            var caja = NuevoContexto(_tenantId).Cajas.AsNoTracking().Single(c => c.Id == cajaId);
            Assert.Equal(500m, caja.Diferencia);
        }

        [Fact]
        public async Task DiferenciaNegativaCuandoFaltaDinero()
        {
            _tenantId = SembrarTenant();
            SembrarUsuario(Usuario);
            var controller = CrearController();
            var cajaId = AbrirCaja(montoInicial: 10000m, totalVentas: 0m);

            await controller.CerrarCaja(cajaId, montoReal: 9200m);

            var caja = NuevoContexto(_tenantId).Cajas.AsNoTracking().Single(c => c.Id == cajaId);
            Assert.Equal(-800m, caja.Diferencia);
        }

        [Fact]
        public async Task LaDiferenciaAdmiteCentavos()
        {
            _tenantId = SembrarTenant();
            SembrarUsuario(Usuario);
            var controller = CrearController();
            var cajaId = AbrirCaja(montoInicial: 10000m, totalVentas: 0m);

            await controller.CerrarCaja(cajaId, montoReal: 9876.54m);

            var caja = NuevoContexto(_tenantId).Cajas.AsNoTracking().Single(c => c.Id == cajaId);
            Assert.Equal(-123.46m, caja.Diferencia);
        }

        // --- Guardas ---

        [Fact]
        public async Task MarcaLaCajaComoCerradaYLePoneFecha()
        {
            _tenantId = SembrarTenant();
            SembrarUsuario(Usuario);
            var controller = CrearController();
            var cajaId = AbrirCaja(montoInicial: 10000m, totalVentas: 0m);

            await controller.CerrarCaja(cajaId, montoReal: 10000m);

            var caja = NuevoContexto(_tenantId).Cajas.AsNoTracking().Single(c => c.Id == cajaId);
            Assert.Equal("CERRADA", caja.Estado);
            Assert.NotNull(caja.FechaCierre);
            Assert.Equal(10000m, caja.MontoReal);
        }

        [Fact]
        public async Task NoCierraUnaCajaQueYaEstaCerrada()
        {
            _tenantId = SembrarTenant();
            SembrarUsuario(Usuario);
            var controller = CrearController();
            var cajaId = AbrirCaja(montoInicial: 10000m, totalVentas: 0m);

            await controller.CerrarCaja(cajaId, montoReal: 10000m);

            // Un segundo intento con otro monto tiene que rechazarse: si pasara, el
            //Difference del primer cierre se sobreescribiria.
            var segundo = await controller.CerrarCaja(cajaId, montoReal: 99999m);

            Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestResult>(segundo);
            var caja = NuevoContexto(_tenantId).Cajas.AsNoTracking().Single(c => c.Id == cajaId);
            Assert.Equal(10000m, caja.MontoReal);
            Assert.Equal(0m, caja.Diferencia);
        }

        [Fact]
        public async Task NoCierraUnaCajaDeOtroTenant()
        {
            _tenantId = SembrarTenant();
            SembrarUsuario(Usuario);
            var controller = CrearController();
            var cajaId = AbrirCaja(montoInicial: 10000m, totalVentas: 0m);

            // Mismo usuario, pero el controller se ejecuta con otro tenant en sesion.
            var otroContexto = NuevoContexto(_tenantId + 500);
            var controllerOtro = new CajaController(otroContexto, new TenantServiceStub(_tenantId + 500))
                .ConClaims(Usuario);

            var resultado = await controllerOtro.CerrarCaja(cajaId, montoReal: 10000m);

            Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestResult>(resultado);
            var caja = NuevoContexto(_tenantId).Cajas.AsNoTracking().Single(c => c.Id == cajaId);
            Assert.Equal("ABIERTA", caja.Estado);
            Assert.Null(caja.MontoReal);
        }

        [Fact]
        public async Task NoCierraUnaCajaDeOtroUsuario()
        {
            _tenantId = SembrarTenant();
            SembrarUsuario(Usuario);
            var controller = CrearController();
            var cajaId = AbrirCaja(montoInicial: 10000m, totalVentas: 0m);

            var controllerOtro = new CajaController(_contexto, new TenantServiceStub(_tenantId)).ConClaims("otro-cajero");

            var resultado = await controllerOtro.CerrarCaja(cajaId, montoReal: 10000m);

            Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestResult>(resultado);
            var caja = NuevoContexto(_tenantId).Cajas.AsNoTracking().Single(c => c.Id == cajaId);
            Assert.Equal("ABIERTA", caja.Estado);
        }

        [Fact]
        public async Task UnaCajaSinVentasCierraEnSuMontoInicial()
        {
            _tenantId = SembrarTenant();
            SembrarUsuario(Usuario);
            var controller = CrearController();
            var cajaId = AbrirCaja(montoInicial: 20000m, totalVentas: 0m);

            await controller.CerrarCaja(cajaId, montoReal: 20000m);

            var caja = NuevoContexto(_tenantId).Cajas.AsNoTracking().Single(c => c.Id == cajaId);
            Assert.Equal("CERRADA", caja.Estado);
            Assert.Equal(0m, caja.Diferencia);
        }
    }
}
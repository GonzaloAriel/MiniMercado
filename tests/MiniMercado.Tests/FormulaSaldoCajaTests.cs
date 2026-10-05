using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Controllers;
using MiniMercado.Models;
using Xunit;

namespace MiniMercado.Tests
{
    /// <summary>
    /// P-032: la formula del saldo esperado de caja estaba escrita en tres lugares
    /// (CajaController.CerrarCaja, CerrarCaja.cshtml y ResumenCierre.cshtml). Si una se
    /// actualizaba y las otras no, el usuario veia un "Esperado" distinto del "Diferencia"
    /// que quedo guardado, y nada lo detectaba porque las pruebas no renderizaban HTML.
    ///
    /// Ahora hay una sola definicion, <see cref="Caja.CalcularSaldoEsperado"/>, y el
    /// controller y las dos vistas la usan. Estos tests fijan la aritmetica y agregan un
    /// guardia que impide que la formula vuelva a aparecer escrita a mano en una vista.
    /// </summary>
    public class FormulaSaldoCajaTests
    {
        // --- La formula pura, sin base de datos ---

        [Fact]
        public void ElEsperadoEsInicialMasVentasMenosEgresos()
        {
            var caja = new Caja { MontoInicial = 10000m, TotalVentas = 25000m };

            Assert.Equal(31000m, caja.CalcularSaldoEsperado(totalEgresos: 4000m));
        }

        [Fact]
        public void SinEgresosElEsperadoEsInicialMasVentas()
        {
            var caja = new Caja { MontoInicial = 5000m, TotalVentas = 7500m };

            Assert.Equal(12500m, caja.CalcularSaldoEsperado(totalEgresos: 0m));
        }

        [Fact]
        public void UnaCajaVaciaDaCero()
        {
            Assert.Equal(0m, new Caja().CalcularSaldoEsperado(0m));
        }

        [Fact]
        public void ElEsperadoAdmiteCentavos()
        {
            var caja = new Caja { MontoInicial = 1000.55m, TotalVentas = 2000.44m };

            Assert.Equal(2999.99m, caja.CalcularSaldoEsperado(totalEgresos: 1.00m));
        }

        [Fact]
        public void ElEsperadoPuedeQuedarNegativo()
        {
            // Un dia con mas egresos que ventas: el arqueo tiene que poder dar negativo.
            var caja = new Caja { MontoInicial = 1000m, TotalVentas = 0m };

            Assert.Equal(-500m, caja.CalcularSaldoEsperado(totalEgresos: 1500m));
        }
    }

    /// <summary>
    /// Fija el enlace entre lo que el controller guarda y lo que la vista muestra.
    ///
    /// Si la formula se duplicara otra vez, estas pruebas seguirian pasando (el controller
    /// calcula bien); por eso el test de <see cref="LaFormulaNoVuelveALasVistas"/> es el que
    /// cierra el problema de verdad.
    /// </summary>
    public class CoherenciaCierreCajaTests : BaseDePrueba
    {
        private const string Usuario = "cajero-1";

        private int _tenantId;

        private CajaController CrearController()
        {
            return new CajaController(NuevoContexto(_tenantId), new TenantServiceStub(_tenantId))
                .ConClaims(Usuario);
        }

        private int AbrirCaja(decimal montoInicial, decimal totalVentas)
        {
            _tenantId = SembrarTenant();
            SembrarUsuario(Usuario);

            var contexto = NuevoContexto(_tenantId);
            var caja = new Caja
            {
                TenantId = _tenantId,
                UsuarioId = Usuario,
                FechaApertura = DateTime.Now,
                MontoInicial = montoInicial,
                TotalVentas = totalVentas,
                Estado = "ABIERTA"
            };

            contexto.Cajas.Add(caja);
            contexto.SaveChanges();

            return caja.Id;
        }

        private void AgregarEgresos(int cajaId, params decimal[] montos)
        {
            var contexto = NuevoContexto(_tenantId);

            foreach (var monto in montos)
            {
                contexto.EgresosCaja.Add(new EgresoCaja
                {
                    TenantId = _tenantId,
                    CajaId = cajaId,
                    Monto = monto,
                    Categoria = CategoriaEgreso.Otros,
                    Descripcion = "Egreso"
                });
            }

            contexto.SaveChanges();
        }

        [Fact]
        public async Task LaDiferenciaGuardadaCoincideConLaFormulaDelModelo()
        {
            // Lo que quedo en Diferencia tiene que ser exactamente lo que las vistas
            // muestran como "Esperado", menos el efectivo contado.
            var cajaId = AbrirCaja(10000m, 25000m);
            AgregarEgresos(cajaId, 4000m, 1500m);

            var totalEgresos = NuevoContexto(_tenantId).EgresosCaja
                .Where(e => e.CajaId == cajaId)
                .Sum(e => (decimal?)e.Monto) ?? 0m;

            await CrearController().CerrarCaja(cajaId, montoReal: 30000m);

            var caja = NuevoContexto(_tenantId).Cajas.AsNoTracking().Single(c => c.Id == cajaId);

            // 10000 + 25000 - 5500 = 29500 de esperado; se entregaron 30000, sobran 500.
            Assert.Equal(29500m, caja.CalcularSaldoEsperado(totalEgresos));
            Assert.Equal(500m, caja.Diferencia);
            Assert.Equal(
                caja.MontoReal - caja.CalcularSaldoEsperado(totalEgresos),
                caja.Diferencia);
        }

        [Fact]
        public async Task CerrarSinEgresosTambienCoincide()
        {
            var cajaId = AbrirCaja(5000m, 7500m);

            await CrearController().CerrarCaja(cajaId, montoReal: 12000m);

            var caja = NuevoContexto(_tenantId).Cajas.AsNoTracking().Single(c => c.Id == cajaId);

            Assert.Equal(12500m, caja.CalcularSaldoEsperado(0m));
            Assert.Equal(-500m, caja.Diferencia);
        }

        [Fact]
        public async Task ElMontoInicialQuedaEnLaCajaYNoSeCobra()
        {
            // El monto con el que se abrio el cajon es parte del saldo esperado, no una
            // venta: por eso entra con signo mas en la formula.
            var cajaId = AbrirCaja(20000m, 0m);

            await CrearController().CerrarCaja(cajaId, montoReal: 20000m);

            var caja = NuevoContexto(_tenantId).Cajas.AsNoTracking().Single(c => c.Id == cajaId);

            Assert.Equal(20000m, caja.CalcularSaldoEsperado(0m));
            Assert.Equal(0m, caja.Diferencia);
        }
    }

    /// <summary>
    /// Guardia contra la re-duplicacion de la formula.
    ///
    /// Es un test sobre el codigo fuente y no sobre el comportamiento, y esa es
    /// justamente la razon de existir: el bug de P-032 era invisible para cualquier prueba
    /// de comportamiento, porque el controller hacia bien las cuentas y el problema estaba
    /// en que las vistas tenian otra copia que se podia quedar vieja sin que nadie se
    /// enterara. Renderizar las vistas para compararlas seria desproporcionado; leer los
    /// archivos alcanza para evitar la reaparicion.
    /// </summary>
    public class LaFormulaNoVuelveALasVistas
    {
        [Fact]
        public void NingunaVistaVuelveAEscribirLaFormulaAMano()
        {
            foreach (var archivo in RutasDeVistas())
            {
                var contenido = File.ReadAllText(archivo);

                Assert.False(
                    contenido.Contains("MontoInicial + Model.TotalVentas")
                    || contenido.Contains("MontoInicial + Model.TotalVentas -"),
                    $"{Path.GetFileName(archivo)} vuelve a escribir la formula del saldo " +
                    "esperado a mano. Tiene que usar Caja.CalcularSaldoEsperado (P-032).");
            }
        }

        [Fact]
        public void ElControllerUsaLaFormulaDelModelo()
        {
            var controller = File.ReadAllText(Path.Combine(RutaFuente, "CajaController.cs"));

            Assert.Contains("CalcularSaldoEsperado", controller);
            Assert.False(
                controller.Contains("MontoInicial + caja.TotalVentas"),
                "CajaController volvio a escribir la formula a mano en vez de usar el modelo.");
        }

        [Fact]
        public void LasDosVistasDeCajaUsanLaFormulaDelModelo()
        {
            foreach (var nombre in new[] { "CerrarCaja.cshtml", "ResumenCierre.cshtml" })
            {
                var ruta = Path.Combine(RutaFuente, nombre);
                Assert.True(File.Exists(ruta), $"No se encontro {ruta}");

                Assert.Contains(
                    "CalcularSaldoEsperado",
                    File.ReadAllText(ruta));
            }
        }

        private static string[] RutasDeVistas()
        {
            return Directory.GetFiles(RutaFuente, "*.cshtml");
        }

        /// <summary>
        /// Carpeta con las fuentes que MiniMercado.Tests.csproj copia a la salida de la
        /// compilacion (ver el ItemGroup "fuente\"). Evita depender de donde se este
        /// ejecutando el test o de como este armado el repositorio.
        /// </summary>
        private static string RutaFuente => Path.Combine(AppContext.BaseDirectory, "fuente");
    }
}
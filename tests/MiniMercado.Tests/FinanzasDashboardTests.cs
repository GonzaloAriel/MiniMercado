using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Controllers;
using MiniMercado.Models;
using MiniMercado.Models.ViewModels;
using Xunit;

namespace MiniMercado.Tests
{
    /// <summary>
    /// El dashboard financiero de FinanzasController. No estaba en el alcance original de
    /// P-017 (venta, cobro, cierre) pero los tests de ventarii revelaron la convencion
    /// invertida de Venta.Activa, y hay dos consultas que la usan al reves.
    ///
    /// CONVENCION: Venta.Activa = false significa venta vigente; Activa = true significa
    /// anulada. La creo VentaController.Create con Activa = false y la respeta
    /// VentaAdminController, que anula poniendo Activa = true.
    /// </summary>
    public class FinanzasDashboardTests : BaseDePrueba
    {
        private int _tenantId;
        private FinanzasController _controller;

        private void Preparar()
        {
            _tenantId = SembrarTenant();
            var contexto = NuevoContexto(_tenantId);
            _controller = new FinanzasController(contexto, new TenantServiceStub(_tenantId))
                .ConClaims("admin-1", "Admin");
        }

        private void SembrarVenta(decimal total, bool anulada, DateTime fecha)
        {
            var contexto = NuevoContexto(_tenantId);
            contexto.Ventas.Add(new Venta
            {
                TenantId = _tenantId,
                Total = total,
                Fecha = fecha,
                // Invertido a proposito: false = vigente, true = anulada.
                Activa = anulada
            });
            contexto.SaveChanges();
        }

        private async Task<DashboardFinancieroViewModel> Dashboard()
        {
            var resultado = await _controller.Dashboard();
            return Assert.IsType<DashboardFinancieroViewModel>(
                Assert.IsType<Microsoft.AspNetCore.Mvc.ViewResult>(resultado).Model);
        }

        [Fact]
        public async Task VentasDeHoySumanSoloLasVentasVigentes()
        {
            // Regresion: la consulta usaba v.Activa, que con la convencion invertida son
            // las ANULADAS. El dashboard mostraba como ingresos del dia la suma de las
            // ventas canceladas, y un dia con ventas reales pero sin anulaciones aparecia
            // en cero.
            Preparar();
            var hoy = DateTime.Now;

            SembrarVenta(1000m, anulada: false, fecha: hoy);
            SembrarVenta(500m, anulada: true, fecha: hoy);

            var vm = await Dashboard();

            Assert.Equal(1000m, vm.VentasHoy);
        }

        [Fact]
        public async Task VentasDelMesSumanSoloLasVentasVigentes()
        {
            Preparar();
            var hoy = DateTime.Now;

            SembrarVenta(2000m, anulada: false, fecha: hoy);
            SembrarVenta(700m, anulada: true, fecha: hoy);
            // Del mes anterior: no cuenta en el mes.
            SembrarVenta(9999m, anulada: false, fecha: hoy.AddMonths(-1));

            var vm = await Dashboard();

            Assert.Equal(2000m, vm.VentasMes);
        }

        [Fact]
        public async Task ElResultadoRestaLosEgresosDeHoy()
        {
            Preparar();
            var hoy = DateTime.Now;

            SembrarVenta(10000m, anulada: false, fecha: hoy);

            var contexto = NuevoContexto(_tenantId);
            contexto.EgresosCaja.Add(new EgresoCaja
            {
                TenantId = _tenantId,
                Monto = 2500m,
                Categoria = CategoriaEgreso.Otros,
                Descripcion = "Pago de proveedor",
                Fecha = hoy
            });
            contexto.SaveChanges();

            // No hace falta volver a llamar Preparar(): el controller consulta contra la
            // base, no contra su change tracker.
            var vm = await Dashboard();

            Assert.Equal(10000m, vm.VentasHoy);
            Assert.Equal(2500m, vm.EgresosHoy);
            Assert.Equal(7500m, vm.ResultadoHoy);
        }

        [Fact]
        public async Task UnDiaSinVentasNiEgresosDaCero()
        {
            Preparar();

            var vm = await Dashboard();

            Assert.Equal(0m, vm.VentasHoy);
            Assert.Equal(0m, vm.EgresosHoy);
            Assert.Equal(0m, vm.ResultadoHoy);
        }

        [Fact]
        public async Task LasVentasDeOtroTenantNoEntranEnElDashboard()
        {
            Preparar();
            var otroTenant = SembrarTenant("Otro");

            SembrarVenta(1000m, anulada: false, fecha: DateTime.Now);

            using (var contextoOtro = NuevoContexto(otroTenant))
            {
                contextoOtro.Ventas.Add(new Venta
                {
                    TenantId = otroTenant,
                    Total = 5000m,
                    Fecha = DateTime.Now,
                    Activa = false
                });
                contextoOtro.SaveChanges();
            }

            var vm = await Dashboard();

            Assert.Equal(1000m, vm.VentasHoy);
        }
    }
}
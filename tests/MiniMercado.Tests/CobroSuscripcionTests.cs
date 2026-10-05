using System;
using System.Linq;
using System.Threading.Tasks;
using MiniMercado.Models;
using MiniMercado.Services;
using Xunit;

namespace MiniMercado.Tests
{
    /// <summary>
    /// Flujo 1 de 3 de P-017: cobro de suscripcion.
    ///
    /// Es el unico de los tres que ya tiene la logica en un service (SuscripcionService),
    /// sin HTTP, sin claims ni T-SQL, asi que se puede probar sinrializar controllers.
    ///
    /// El primer test es la regresion de P-022: el monto cobrado nunca se guardaba
    /// porque se usaba el precio teorico del plan en vez del monto realmente cobrado.
    /// </summary>
    public class CobroSuscripcionTests : BaseDePrueba
    {
        private int _tenantId;

        private SuscripcionService CrearServicio(out MiniMercado.Data.MiniMercadoContext contexto)
        {
            contexto = NuevoContexto(_tenantId);
            return new SuscripcionService(contexto);
        }

        // --- Regresion P-022: el monto real tiene que persistirse ---

        [Fact]
        public async Task GuardaElMontoRealCobrado()
        {
            _tenantId = SembrarTenant();

            var servicio = CrearServicio(out var contexto);
            await servicio.RegistrarPagoAsync(_tenantId, meses: 1, plan: "Mensual", monto: 8500m);

            using var verificacion = NuevoContexto(_tenantId);
            var cobro = verificacion.CobrosTenants.Single();

            Assert.Equal(8500m, cobro.Monto);
            Assert.NotEqual(0m, cobro.Monto);
        }

        [Fact]
        public async Task GuardaElMontoNegociadoYNoElPrecioDelPlan()
        {
            // El tenant pago 3000 aunque el plan figura a 5000 (descuento o monto
            // negociado). El precio del plan es una sugerencia: si se usara para
            // escribir el cobro, el historial mostraria 5000 y el balance no cerraria.
            _tenantId = SembrarTenant();

            var servicio = CrearServicio(out var contexto);
            await servicio.RegistrarPagoAsync(_tenantId, meses: 1, plan: "Mensual", monto: 3000m);

            using var verificacion = NuevoContexto(_tenantId);
            var cobro = verificacion.CobrosTenants.Single();
            var tenant = verificacion.Tenants.Single();

            Assert.Equal(3000m, cobro.Monto);
            Assert.Equal(5000m, tenant.PrecioMensual);
        }

        [Fact]
        public async Task AceptaDecimalesConCentavos()
        {
            // decimal(18,2): si el monto se trunca a entero, un cobro de 1234.56 queda
            // en 1234 y el balance no cuadra contra la caja.
            _tenantId = SembrarTenant();

            var servicio = CrearServicio(out var contexto);
            await servicio.RegistrarPagoAsync(_tenantId, meses: 1, plan: "Mensual", monto: 1234.56m);

            using var verificacion = NuevoContexto(_tenantId);
            Assert.Equal(1234.56m, verificacion.CobrosTenants.Single().Monto);
        }

        // --- Acumulacion de meses ---

        [Fact]
        public async Task AcumulaSobreLaFechaFutura()
        {
            // Si la suscripcion sigue vigente, el nuevo vencimiento se cuenta desde el
            // vencimiento actual, no desde hoy: asi un tenant que paga por adelantado
            // no pierde los meses que ya habia pagado.
            _tenantId = SembrarTenant();

            var vigente = DateTime.Now.AddDays(30);
            PrepararTenant(proximoPago: vigente);

            var servicio = CrearServicio(out var contexto);
            await servicio.RegistrarPagoAsync(_tenantId, meses: 3, plan: "Anual", monto: 10000m);

            using var verificacion = NuevoContexto(_tenantId);
            var tenant = verificacion.Tenants.Single();

            // Tolerancia de 5s: el servicio usa DateTime.Now y la base guarda con
            // precision de milisegundos, asi que la igualdad exacta seria fragil.
            Assert.NotNull(tenant.FechaProximoPago);
            var diferencia = (tenant.FechaProximoPago.Value - vigente.AddMonths(3)).Duration();
            Assert.True(
                diferencia < TimeSpan.FromSeconds(5),
                $"Esperado {vigente.AddMonths(3):yyyy-MM-dd HH:mm:ss}, " +
                $"obtenido {tenant.FechaProximoPago:yyyy-MM-dd HH:mm:ss}.");

            Assert.Equal(3, tenant.MesesPagados);
        }

        [Fact]
        public async Task SiEstaVencidaSumaDesdeHoyYNoDesdeElPasado()
        {
            // Al reengancharse un tenant vencido, los meses se cuentan desde hoy. Si se
            // sumaran desde el vencimiento viejo, el nuevo vencimiento quedaria en el
            // pasado y el tenant quedaria vencido al instante.
            _tenantId = SembrarTenant();

            PrepararTenant(proximoPago: DateTime.Now.AddDays(-45));

            var servicio = CrearServicio(out var contexto);
            await servicio.RegistrarPagoAsync(_tenantId, meses: 1, plan: "Mensual", monto: 5000m);

            using var verificacion = NuevoContexto(_tenantId);
            var tenant = verificacion.Tenants.Single();

            Assert.True(
                tenant.FechaProximoPago > DateTime.Now.AddDays(27),
                $"El vencimiento quedo en {tenant.FechaProximoPago:yyyy-MM-dd}, que sigue siendo pasado.");
        }

        [Fact]
        public async Task PrimeraSuscripcionFijaLaFechaDeInicio()
        {
            _tenantId = SembrarTenant();

            var servicio = CrearServicio(out var contexto);
            await servicio.RegistrarPagoAsync(_tenantId, meses: 1, plan: "Mensual", monto: 5000m);

            using var verificacion = NuevoContexto(_tenantId);
            var tenant = verificacion.Tenants.Single();

            Assert.True(tenant.FechaInicioSuscripcion.HasValue);
            Assert.True(tenant.UltimoPagoRegistrado.HasValue);
        }

        [Fact]
        public async Task SincronizaFechaVencimientoConProximoPago()
        {
            // FechaVencimiento es el campo legado; las dos columnas tienen que quedar
            // iguales porque distintas vistas leen una o la otra.
            _tenantId = SembrarTenant();

            var servicio = CrearServicio(out var contexto);
            await servicio.RegistrarPagoAsync(_tenantId, meses: 2, plan: "Mensual", monto: 10000m);

            using var verificacion = NuevoContexto(_tenantId);
            var tenant = verificacion.Tenants.Single();

            Assert.Equal(tenant.FechaProximoPago, tenant.FechaVencimiento);
        }

        [Fact]
        public async Task ElCobroGuardaElProximoPagoComoSuVencimiento()
        {
            _tenantId = SembrarTenant();

            var servicio = CrearServicio(out var contexto);
            await servicio.RegistrarPagoAsync(_tenantId, meses: 1, plan: "Mensual", monto: 5000m);

            using var verificacion = NuevoContexto(_tenantId);
            var cobro = verificacion.CobrosTenants.Single();
            var tenant = verificacion.Tenants.Single();

            Assert.Equal(tenant.FechaProximoPago, cobro.FechaVencimiento);
        }

        [Fact]
        public async Task ActivaLaSuscripcionYSumaLosMeses()
        {
            _tenantId = SembrarTenant();
            PrepararTenant(estado: EstadoSuscripcion.Vencida);

            var servicio = CrearServicio(out var contexto);
            await servicio.RegistrarPagoAsync(_tenantId, meses: 6, plan: "Anual", monto: 25000m);

            using var verificacion = NuevoContexto(_tenantId);
            var tenant = verificacion.Tenants.Single();

            Assert.Equal(EstadoSuscripcion.Activa, tenant.EstadoSuscripcion);
            Assert.Equal(6, tenant.MesesPagados);
            Assert.Equal("Anual", tenant.Plan);
        }

        [Fact]
        public async Task AcumulaMesesPagadosEntreCobros()
        {
            _tenantId = SembrarTenant();

            var servicio = CrearServicio(out var contexto);
            await servicio.RegistrarPagoAsync(_tenantId, meses: 1, plan: "Mensual", monto: 5000m);
            await servicio.RegistrarPagoAsync(_tenantId, meses: 2, plan: "Mensual", monto: 10000m);

            using var verificacion = NuevoContexto(_tenantId);
            var tenant = verificacion.Tenants.Single();

            Assert.Equal(3, tenant.MesesPagados);
            Assert.Equal(2, verificacion.CobrosTenants.Count());
            Assert.Equal(15000m, verificacion.CobrosTenants.Sum(c => c.Monto));
        }

        // --- Casos borde ---

        [Fact]
        public async Task TenantInexistenteNoEscribeNiExplota()
        {
            _tenantId = SembrarTenant();

            var servicio = CrearServicio(out var contexto);
            await servicio.RegistrarPagoAsync(tenantId: 999999, meses: 1, plan: "Mensual", monto: 5000m);

            using var verificacion = NuevoContexto(_tenantId);
            Assert.Empty(verificacion.CobrosTenants.ToList());
        }

        [Fact]
        public async Task UsaLaObservacionPorDefectoCuandoNoSeManda()
        {
            _tenantId = SembrarTenant();

            var servicio = CrearServicio(out var contexto);
            await servicio.RegistrarPagoAsync(_tenantId, meses: 3, plan: "Trimestral", monto: 12000m);

            using var verificacion = NuevoContexto(_tenantId);
            var observacion = verificacion.CobrosTenants.Single().Observacion;

            Assert.NotNull(observacion);
            Assert.Contains("3", observacion);
            Assert.Contains("Trimestral", observacion);
        }

        [Fact]
        public async Task RespetaLaObservacionIndicada()
        {
            _tenantId = SembrarTenant();

            var servicio = CrearServicio(out var contexto);
            await servicio.RegistrarPagoAsync(
                _tenantId, meses: 1, plan: "Mensual", monto: 5000m, observacion: "Transferencia banco X");

            using var verificacion = NuevoContexto(_tenantId);
            Assert.Equal("Transferencia banco X", verificacion.CobrosTenants.Single().Observacion);
        }

        [Fact]
        public async Task ElCobroQuedaAisladoEnSuTenant()
        {
            // El query filter de CobroTenant tiene que impedir que un tenant vea los
            // cobros de otro.
            var tenantA = SembrarTenant("A");
            _tenantId = tenantA;
            var tenantB = SembrarTenant("B");

            var servicioA = new SuscripcionService(NuevoContexto(tenantA));
            await servicioA.RegistrarPagoAsync(tenantA, meses: 1, plan: "Mensual", monto: 5000m);

            var servicioB = new SuscripcionService(NuevoContexto(tenantB));
            await servicioB.RegistrarPagoAsync(tenantB, meses: 1, plan: "Mensual", monto: 7000m);

            using var contextoA = NuevoContexto(tenantA);
            using var contextoB = NuevoContexto(tenantB);

            Assert.Equal(5000m, contextoA.CobrosTenants.Single().Monto);
            Assert.Equal(7000m, contextoB.CobrosTenants.Single().Monto);
            Assert.Equal(2, Context.CobrosTenants.Count());
        }

        [Fact]
        public async Task LeerLaSuscripcionDevuelveElTenantPedido()
        {
            // Tenants no tiene query filter, asi que el AdminGlobal puede leer cualquiera.
            _tenantId = SembrarTenant();

            var servicio = CrearServicio(out var contexto);
            await servicio.RegistrarPagoAsync(_tenantId, meses: 1, plan: "Mensual", monto: 5000m);

            var leido = await servicio.GetSuscripcionActualAsync(_tenantId);

            Assert.NotNull(leido);
            Assert.Equal(_tenantId, leido.Id);
        }

        private void PrepararTenant(
            DateTime? proximoPago = null,
            DateTime? inicio = null,
            EstadoSuscripcion estado = EstadoSuscripcion.PendientePago)
        {
            var tenant = Context.Tenants.Single(t => t.Id == _tenantId);
            tenant.FechaProximoPago = proximoPago;
            tenant.FechaInicioSuscripcion = inicio;
            tenant.EstadoSuscripcion = estado;
            Context.SaveChanges();
            Context.ChangeTracker.Clear();
        }
    }
}
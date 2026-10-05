using System;
using MiniMercado.Models;
using MiniMercado.Services;
using Xunit;

namespace MiniMercado.Tests
{
    /// <summary>
    /// CalcularEstado y CalcularDiasRestantes son funciones puras sobre un Tenant: no
    /// tocan la base, asi que no heredan de BaseDePrueba (que migraria 60 migraciones
    /// para nada).
    ///
    /// La precedencia importa: un tenant bloqueado esta suspendido aunque su fecha de
    /// pago sea futura. Por eso el orden de los casos es bloqueado, sin fecha,
    /// vencida y activa.
    /// </summary>
    public class CalculoEstadoSuscripcionTests
    {
        private readonly SuscripcionService _servicio;

        public CalculoEstadoSuscripcionTests()
        {
            _servicio = new SuscripcionService(null);
        }

        [Fact]
        public void BloqueadoGanaSiempre()
        {
            // Fecha de pago futura: si el tenant esta bloqueado sigue suspendido.
            var tenant = NuevoTenant(bloqueado: true, proximoPago: DateTime.Now.AddDays(30));

            Assert.Equal(EstadoSuscripcion.Suspendida, _servicio.CalcularEstado(tenant));
        }

        [Fact]
        public void SinFechaDeProximoPagoQuedaPendiente()
        {
            var tenant = NuevoTenant(bloqueado: false, proximoPago: null);

            Assert.Equal(EstadoSuscripcion.PendientePago, _servicio.CalcularEstado(tenant));
        }

        [Fact]
        public void FechaPasadaQuedaVencida()
        {
            var tenant = NuevoTenant(bloqueado: false, proximoPago: DateTime.Now.AddDays(-1));

            Assert.Equal(EstadoSuscripcion.Vencida, _servicio.CalcularEstado(tenant));
        }

        [Fact]
        public void SigueActivoTodoElDiaDeVencimiento()
        {
            // Regresion: la suscripcion se cuenta por dias calendario, no por instantes.
            // RegistrarPagoAsync guarda FechaProximoPago con la hora en que se cobro
            // (DateTime.Now.AddMonths(n)), asi que comparar con DateTime.Now hacia que
            // un tenant que pago el dia 5 a las 14:30 quedara VENCIDO a las 15:30 del
            // mismo dia 5, y con un dia menos de margen.
            var tenant = NuevoTenant(bloqueado: false, proximoPago: DateTime.Now);

            Assert.Equal(EstadoSuscripcion.Activa, _servicio.CalcularEstado(tenant));
        }

        [Fact]
        public void QuedaVencidaAlDiaSiguiente()
        {
            // El ultimo dia con suscripcion es el dia de vencimiento; al siguiente ya
            // no hay margen.
            var hoy = NuevoTenant(bloqueado: false, proximoPago: DateTime.Today);
            var ayer = NuevoTenant(bloqueado: false, proximoPago: DateTime.Today.AddDays(-1));

            Assert.Equal(EstadoSuscripcion.Activa, _servicio.CalcularEstado(hoy));
            Assert.Equal(EstadoSuscripcion.Vencida, _servicio.CalcularEstado(ayer));
        }

        [Fact]
        public void FechaFuturaQedaActiva()
        {
            var tenant = NuevoTenant(bloqueado: false, proximoPago: DateTime.Now.AddDays(30));

            Assert.Equal(EstadoSuscripcion.Activa, _servicio.CalcularEstado(tenant));
        }

        [Fact]
        public void DiasRestantesCuentanDiasCalendario()
        {
            // A midnight de dentro de 10 dias hay 10 dias calendario completos. Con la
            // comparacion por instante devolvia 9, porque la fraccion de hora ya habia
            // transcurrido.
            var tenant = NuevoTenant(proximoPago: DateTime.Now.Date.AddDays(10));

            Assert.Equal(10, _servicio.CalcularDiasRestantes(tenant));
        }

        [Fact]
        public void DiasRestantesMismoDiaEsCeroYNoUno()
        {
            var tenant = NuevoTenant(proximoPago: DateTime.Now.Date);

            Assert.Equal(0, _servicio.CalcularDiasRestantes(tenant));
        }

        [Fact]
        public void DiasRestantesNoBajanDeCero()
        {
            // Con un vencimiento pasado la diferencia es negativa. Si se devolviera tal
            // cual, la vista sumaria dias negativos y el saldo del tenant se veria raro.
            var tenant = NuevoTenant(proximoPago: DateTime.Now.AddDays(-10));

            Assert.Equal(0, _servicio.CalcularDiasRestantes(tenant));
        }

        [Fact]
        public void SinFechaDevuelveCeroDias()
        {
            Assert.Equal(0, _servicio.CalcularDiasRestantes(NuevoTenant(proximoPago: null)));
        }

        private static Tenant NuevoTenant(bool bloqueado = false, DateTime? proximoPago = null) => new()
        {
            NombreComercio = "Tenant",
            Bloqueado = bloqueado,
            FechaProximoPago = proximoPago
        };
    }
}
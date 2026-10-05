using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;

namespace MiniMercado.Services
{
    public interface ISuscripcionService
    {
        Task RegistrarPagoAsync(int tenantId, int meses, string plan, decimal monto, string? observacion = null);
        Task<Tenant?> GetSuscripcionActualAsync(int tenantId);
        EstadoSuscripcion CalcularEstado(Tenant tenant);
        int CalcularDiasRestantes(Tenant tenant);
    }

    public class SuscripcionService : ISuscripcionService
    {
        private readonly MiniMercadoContext _context;

        public SuscripcionService(MiniMercadoContext context)
        {
            _context = context;
        }

        public async Task RegistrarPagoAsync(int tenantId, int meses, string plan, decimal monto, string? observacion = null)
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) return;

            DateTime hoy = DateTime.Now;

            // 1. Actualizar Fecha de inicio si es primera vez o estaba vencida hace mucho
            if (!tenant.FechaInicioSuscripcion.HasValue)
            {
                tenant.FechaInicioSuscripcion = hoy;
            }

            // 2. Calcular Fecha de Próximo Pago
            // Si la fecha actual de próximo pago es futura, sumamos a esa fecha (Acumulación)
            // Si ya venció o no tiene, sumamos desde hoy.
            DateTime baseFecha = (tenant.FechaProximoPago.HasValue && tenant.FechaProximoPago > hoy) 
                                 ? tenant.FechaProximoPago.Value 
                                 : hoy;

            tenant.FechaProximoPago = baseFecha.AddMonths(meses);
            tenant.Plan = plan;
            tenant.MesesPagados += meses;
            tenant.UltimoPagoRegistrado = hoy;
            tenant.EstadoSuscripcion = EstadoSuscripcion.Activa;
            
            // Sincronizar con el campo anterior por compatibilidad
            tenant.FechaVencimiento = tenant.FechaProximoPago;

            // 3. Registrar en Historial (CobroTenant)
            // Se guarda el monto REAL cobrado (monto), no el precio teorico del plan:
            // el precio mensual es solo una sugerencia y no refleja descuentos ni
            // montos negociados, por lo que usarlo aqui dejaba los cobros en 0.
            var cobro = new CobroTenant
            {
                TenantId = tenantId,
                Monto = monto,
                FechaPago = hoy,
                FechaVencimiento = tenant.FechaProximoPago.Value,
                Observacion = observacion ?? $"Pago de {meses} meses - Plan {plan}"
            };

            _context.CobrosTenants.Add(cobro);
            await _context.SaveChangesAsync();
        }

        public async Task<Tenant?> GetSuscripcionActualAsync(int tenantId)
        {
            // Usamos IgnoreQueryFilters para asegurar que el AdminGlobal pueda verlo si es necesario, 
            // aunque usualmente el Tenant solo ve el suyo.
            return await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId);
        }

        public EstadoSuscripcion CalcularEstado(Tenant tenant)
        {
            if (tenant.Bloqueado) return EstadoSuscripcion.Suspendida;
            if (!tenant.FechaProximoPago.HasValue) return EstadoSuscripcion.PendientePago;

            // Se compara por DIA, no por instante. RegistrarPagoAsync guarda
            // FechaProximoPago con la hora en que se cobro (DateTime.Now.AddMonths(n)),
            // y comparar con DateTime.Now hacia que un tenant que pago el dia 5 a las
            // 14:30 quedara VENCIDO a las 15:30 del mismo dia 5. Una suscripcion pagada
            // hasta el dia 5 sigue vigente durante todo ese dia.
            if (DateTime.Today > tenant.FechaProximoPago.Value.Date)
            {
                return EstadoSuscripcion.Vencida;
            }

            return EstadoSuscripcion.Activa;
        }

        public int CalcularDiasRestantes(Tenant tenant)
        {
            if (!tenant.FechaProximoPago.HasValue) return 0;

            // Dias calendario completos, no fraccion de dia. Comparar instantes
            // devolvia un dia menos que el nominal en cuanto habia transcurrido un
            // poco de hora.
            var diff = tenant.FechaProximoPago.Value.Date - DateTime.Today;
            return diff.Days > 0 ? diff.Days : 0;
        }
    }
}

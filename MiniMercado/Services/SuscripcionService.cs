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

            if (DateTime.Now > tenant.FechaProximoPago.Value)
            {
                return EstadoSuscripcion.Vencida;
            }

            return EstadoSuscripcion.Activa;
        }

        public int CalcularDiasRestantes(Tenant tenant)
        {
            if (!tenant.FechaProximoPago.HasValue) return 0;
            
            var diff = tenant.FechaProximoPago.Value - DateTime.Now;
            return diff.Days > 0 ? diff.Days : 0;
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models.ViewModels;
using MiniMercado.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MiniMercado.Controllers
{
    [Authorize(Roles = "Admin,Caja")]
    public class FinanzasController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;

        public FinanzasController(MiniMercadoContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var tenantId = _tenantService.GetTenantId();
            if (!tenantId.HasValue) return RedirectToAction("Index", "Home");

            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var finHoy = hoy.AddDays(1).AddTicks(-1);

            var vm = new DashboardFinancieroViewModel();

            // Ventas del día (Solo vigentes)
            // OJO: la convencion de Venta.Activa esta invertida: Activa = false es venta
            // vigente y Activa = true es venta anulada (la creo VentaController.Create con
            // false y VentaAdminController anula poniendo true). Filtrar por v.Activa
            // suma las CANCELADAS como si fueran ingresos.
            // El filtro de TenantId se aplica globalmente, pero lo ponemos por claridad y seguridad
            vm.VentasHoy = await _context.Ventas
                .Where(v => !v.Activa && v.Fecha >= hoy && v.Fecha <= finHoy)
                .SumAsync(v => (decimal?)v.Total) ?? 0;

            // Egresos del día
            vm.EgresosHoy = await _context.EgresosCaja
                .Where(e => e.Fecha >= hoy && e.Fecha <= finHoy)
                .SumAsync(e => (decimal?)e.Monto) ?? 0;

            // Ventas del mes (Solo vigentes, ver nota de !Activa arriba)
            vm.VentasMes = await _context.Ventas
                .Where(v => !v.Activa && v.Fecha >= inicioMes)
                .SumAsync(v => (decimal?)v.Total) ?? 0;

            // Egresos del mes
            vm.EgresosMes = await _context.EgresosCaja
                .Where(e => e.Fecha >= inicioMes)
                .SumAsync(e => (decimal?)e.Monto) ?? 0;

            return View(vm);
        }
    }
}

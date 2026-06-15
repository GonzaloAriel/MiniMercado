using System;

namespace MiniMercado.Models.ViewModels
{
    public class DashboardFinancieroViewModel
    {
        // Métricas del Día
        public decimal VentasHoy { get; set; }
        public decimal EgresosHoy { get; set; }
        public decimal ResultadoHoy => VentasHoy - EgresosHoy;

        // Métricas del Mes
        public decimal VentasMes { get; set; }
        public decimal EgresosMes { get; set; }
        public decimal ResultadoMes => VentasMes - EgresosMes;

        public DateTime FechaConsulta { get; set; } = DateTime.Now;
    }
}

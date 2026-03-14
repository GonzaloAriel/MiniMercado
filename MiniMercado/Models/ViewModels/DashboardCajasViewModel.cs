using MiniMercado.Models;

namespace MiniMercado.ViewModels
{
    public class DashboardCajasViewModel
    {
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }

        public List<ResumenCajaDia> ResumenCajas { get; set; }

        public List<VentaHistorialItem> HistorialVentas { get; set; }
        public List<Auditoria> Auditorias { get; set; }
    }
}
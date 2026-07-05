using System.ComponentModel.DataAnnotations;

namespace MiniMercado.Models
{
    public class VentasPorRubroViewModel
    {
        public string RubroNombre { get; set; }
        public decimal FacturacionTotal { get; set; }
        public decimal UnidadesVendidas { get; set; }
        public int CantidadVentas { get; set; }
        public decimal ParticipacionPorcentual { get; set; }
    }
}

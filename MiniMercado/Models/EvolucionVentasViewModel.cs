using System;

namespace MiniMercado.Models
{
    public class EvolucionVentasViewModel
    {
        public string Periodo { get; set; }
        public decimal Facturacion { get; set; }
        public int CantidadVentas { get; set; }
        public decimal TicketPromedio { get; set; }
        public DateTime FechaOrden { get; set; }
    }
}

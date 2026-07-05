namespace MiniMercado.Models
{
    public class PagosViewModel
    {
        public string Metodo { get; set; }
        public int CantidadOperaciones { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal TicketPromedio { get; set; }
        public decimal ParticipacionPorcentual { get; set; }
    }
}

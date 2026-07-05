namespace MiniMercado.Models
{
    public class ProductosMasVendidosViewModel
    {
        public string Nombre { get; set; }
        public string Rubro { get; set; }
        public decimal CantidadTotalVendida { get; set; }
        public decimal FacturacionGenerada { get; set; }
        public decimal PrecioPromedio { get; set; }
        public int CantidadDeVentas { get; set; }
        public decimal ParticipacionPorcentual { get; set; }
    }
}

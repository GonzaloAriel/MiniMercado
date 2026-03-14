namespace MiniMercado.Models
{
    public class ResumenCajaDia
    {
        public string UsuarioId { get; set; }
        public string? Usuario { get; set; }
        public DateTime Fecha { get; set; }

        public int CantidadVentas { get; set; }

        public int CantidadAnuladas { get; set; }

        public decimal TotalVendido { get; set; }
    }
}

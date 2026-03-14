namespace MiniMercado.Models
{
    public class VentaHistorialItem
    {
        public int VentaId { get; set; }

        public DateTime Fecha { get; set; }

        public string UsuarioId { get; set; }
        public string? Usuario { get; set; }
        public decimal Total { get; set; }

        public bool Activa { get; set; }
    }
}
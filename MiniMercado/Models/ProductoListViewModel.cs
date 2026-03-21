namespace MiniMercado.Models
{
    public class ProductoListViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string CodigoBarra { get; set; }
        public decimal Precio { get; set; }
        public decimal Stock { get; set; }
        public bool Activo { get; set; }
    }
}
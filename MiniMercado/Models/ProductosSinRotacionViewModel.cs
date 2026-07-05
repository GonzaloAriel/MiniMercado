namespace MiniMercado.Models
{
    public class ProductosSinRotacionViewModel
    {
        public string Nombre { get; set; }
        public string Rubro { get; set; }
        public decimal Stock { get; set; }
        public DateTime? UltimaVenta { get; set; }
        public int DiasSinVender { get; set; } 
        public decimal Costo { get; set; }
        public decimal ValorStock { get; set; }
    }
}

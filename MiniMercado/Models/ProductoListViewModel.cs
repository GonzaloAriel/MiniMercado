using System.Collections.Generic;

namespace MiniMercado.Models
{
    public class ProductoIndexViewModel
    {
        public List<Producto> Productos { get; set; }
        public List<Rubro> Rubros { get; set; }
        public int? FiltroRubro { get; set; }
    }
}       
using System.Collections.Generic;

namespace MiniMercado.Models
{
    public class ProductoIndexViewModel
    {
        public List<Producto> Productos { get; set; }
        public List<Rubro> Rubros { get; set; }
        public int? FiltroRubro { get; set; }

        // Paginación
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public string? FiltroNombre { get; set; }
    }
}       
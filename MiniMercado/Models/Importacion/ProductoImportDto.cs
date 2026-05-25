using System.ComponentModel.DataAnnotations;

namespace MiniMercado.Models.Importacion
{
    public class ProductoImportDto
    {
        public string CodigoBarra { get; set; }

        public string Nombre { get; set; }

        public string Rubro { get; set; }

        public decimal Precio { get; set; }

        public decimal Stock { get; set; }
    }
}
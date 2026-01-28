using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace MiniMercado.Models
{
    public class VentaDetalle
    {
        public int Id { get; set; }

        [Required]
        public int VentaId { get; set; }

        [ValidateNever]
        public Venta Venta { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [ValidateNever]
        public Producto Producto { get; set; }

        [Required]
        public int Cantidad { get; set; }

        [Required]
        public decimal PrecioUnitario { get; set; }
    }
}

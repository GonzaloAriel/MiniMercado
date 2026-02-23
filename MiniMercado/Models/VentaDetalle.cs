using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniMercado.Models
{
    public class VentaDetalle
    {
        public int Id { get; set; }

        [Required]
        public int VentaId { get; set; }

        [ValidateNever]
        public Venta Venta { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un producto")]
        public int? ProductoId { get; set; }


        [ValidateNever]
        public Producto Producto { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal Cantidad { get; set; }

        [Required]
        public decimal PrecioUnitario { get; set; }

        [ValidateNever]
        public int TenantId { get; set; }
        [ValidateNever]
        public Tenant Tenant { get; set; }

    }
}

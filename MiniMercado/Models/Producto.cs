using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace MiniMercado.Models
{
    public class Producto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El código de barra es obligatorio")]
        [StringLength(50)]
        public string CodigoBarra { get; set; }


        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string Nombre { get; set; }


        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, 999999, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }


        [Required(ErrorMessage = "El stock es obligatorio")]
        [Range(0, 100000, ErrorMessage = "El stock no puede ser negativo")]
        public decimal Stock { get; set; }

        public bool EsPorPeso { get; set; } = false;

        public bool PrecioEditable { get; set; } = false;

        public bool Activo { get; set; } = true;

        [ValidateNever]
        public List<MovimientoStock> Movimientos { get; set; }

        [ValidateNever]
        public int TenantId { get; set; }

        [ValidateNever]
        public Tenant Tenant { get; set; }

    }
}

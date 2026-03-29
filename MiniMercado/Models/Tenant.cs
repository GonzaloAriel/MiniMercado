using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniMercado.Models
{
    public class Tenant
    {
        public int Id { get; set; }

        [Required]
        public string NombreComercio { get; set; }

        public string? Direccion { get; set; }

        public string? Telefono { get; set; }

        public string? Email { get; set; }

        public string? CUIT { get; set; }

        public string? MensajeTicket { get; set; } // ej: "Gracias por su compra!"

        public bool Activo { get; set; } = true;

        [Display(Name = "Fecha de Vencimiento")]
        public DateTime? FechaVencimiento { get; set; }

        [Required(ErrorMessage = "El precio mensual es obligatorio")]
        [Range(0, 9999999, ErrorMessage = "El precio no puede ser negativo")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioMensual { get; set; }

        public bool Bloqueado { get; set; } = false;

        [ValidateNever]
        public virtual ICollection<CobroTenant> Cobros { get; set; } = new List<CobroTenant>();
    }
}

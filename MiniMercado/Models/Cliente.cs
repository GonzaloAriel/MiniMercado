
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MiniMercado.Models
{
    public class Cliente : ITenantEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        [Range(0, 999999, ErrorMessage = "El límite debe ser mayor a 0")]
        public decimal LimiteCredito { get; set; } = 0;

        public bool Activo { get; set; } = true;

        [ValidateNever]
        public int TenantId { get; set; }

        [ValidateNever]
        public Tenant Tenant { get; set; }

        

        [ValidateNever]
        public List<PagoFiado> Pagos { get; set; } = new();

        [ValidateNever]
        public List<Venta> Ventas { get; set; } = new();
    }
}

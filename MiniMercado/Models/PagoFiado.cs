using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace MiniMercado.Models
{
    public class PagoFiado : ITenantEntity
    {
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [ValidateNever]
        public Cliente Cliente { get; set; }

        [Required]
        [Range(0.01, 999999, ErrorMessage = "El monto debe ser mayor a 0")]
        public decimal Monto { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [StringLength(200)]
        public string? Observacion { get; set; }

        [ValidateNever]
        public int TenantId { get; set; }

        [ValidateNever]
        public Tenant Tenant { get; set; }
    }
}
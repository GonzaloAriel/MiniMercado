using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniMercado.Models
{
    public class CobroTenant : ITenantEntity
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El monto es obligatorio")]
        [Range(0.01, 9999999, ErrorMessage = "El monto debe ser mayor a 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "La fecha de pago es obligatoria")]
        [Display(Name = "Fecha de Pago")]
        public DateTime FechaPago { get; set; }

        [Required(ErrorMessage = "La fecha de vencimiento es obligatoria")]
        [Display(Name = "Fecha de Vencimiento")]
        public DateTime FechaVencimiento { get; set; }

        [StringLength(500, ErrorMessage = "La observación no puede superar los 500 caracteres")]
        public string? Observacion { get; set; }

        // Multi-tenant configuration
        [ValidateNever]
        public int TenantId { get; set; }

        [ValidateNever]
        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }
    }
}

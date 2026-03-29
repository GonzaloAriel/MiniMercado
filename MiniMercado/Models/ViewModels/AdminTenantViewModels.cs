using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MiniMercado.Models.ViewModels
{
    public class TenantAdminViewModel
    {
        public int Id { get; set; }
        public string NombreComercio { get; set; }
        public DateTime? UltimoPago { get; set; }
        public decimal TotalPagado { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public bool Bloqueado { get; set; }
        public bool Activo { get; set; }
        public bool EstaVencido => FechaVencimiento.HasValue && FechaVencimiento.Value < DateTime.Now;
        public bool PorVencer => FechaVencimiento.HasValue && !EstaVencido && (FechaVencimiento.Value - DateTime.Now).TotalDays <= 5;
    }

    public class RegistrarPagoViewModel
    {
        [Required]
        public int TenantId { get; set; }

        public string? NombreComercio { get; set; }

        [Required(ErrorMessage = "El monto es obligatorio")]
        [Range(0.01, 9999999, ErrorMessage = "Monto inválido")]
        public decimal Monto { get; set; }

        [Display(Name = "Observación")]
        public string? Observacion { get; set; }

        [Display(Name = "Fecha de Vencimiento (Opcional)")]
        [DataType(DataType.Date)]
        public DateTime? FechaVencimiento { get; set; }

        public decimal PrecioSugerido { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public decimal TotalRecaudado { get; set; }
        public int TenantsActivos { get; set; }
        public int TenantsVencidos { get; set; }
        public int TenantsBloqueados { get; set; }
        public int TotalTenants { get; set; }
    }
}

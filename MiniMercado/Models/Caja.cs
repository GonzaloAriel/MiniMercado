using System;
using System.ComponentModel.DataAnnotations;

namespace MiniMercado.Models
{
    public class Caja : ITenantEntity
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;
        public ApplicationUser? Usuario { get; set; }

        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }

        public decimal MontoInicial { get; set; }
        public decimal TotalVentas { get; set; }
        public decimal? MontoReal { get; set; }
        public decimal? Diferencia { get; set; }

        [MaxLength(20)]
        public string Estado { get; set; } = "ABIERTA"; // ABIERTA / CERRADA / AUTOMATICA
    }
}

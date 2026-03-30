using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniMercado.Models
{
    public class Notificacion : ITenantEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; }

        [Required]
        [StringLength(100)]
        public string Titulo { get; set; }

        [Required]
        [StringLength(500)]
        public string Mensaje { get; set; }

        [Required]
        [StringLength(50)]
        public string Tipo { get; set; } // "Stock", "Pago", "Venta"

        public bool Leida { get; set; } = false;

        public DateTime Fecha { get; set; } = DateTime.Now;

        [StringLength(255)]
        public string? Url { get; set; }
    }
}

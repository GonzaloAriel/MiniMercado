using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniMercado.Models
{
    public class Pago : ITenantEntity
    {
        public int Id { get; set; }

        public int VentaId { get; set; }
        public Venta Venta { get; set; }

        public int TenantId { get; set; }
        public Tenant Tenant { get; set; }

        [Required]
        [StringLength(20)]
        public string Metodo { get; set; } // EFECTIVO, MP, TARJETA

        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; } // monto que cubre la venta

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MontoRecibido { get; set; } // solo efectivo

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Vuelto { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}

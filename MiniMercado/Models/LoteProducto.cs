using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniMercado.Models
{
    public class LoteProducto : ITenantEntity
    {
        public int Id { get; set; }

        [Required]
        public int ProductoId { get; set; }
        
        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Cantidad { get; set; }

        public DateTime? FechaVencimiento { get; set; }

        public DateTime FechaIngreso { get; set; } = DateTime.Now;
    }
}

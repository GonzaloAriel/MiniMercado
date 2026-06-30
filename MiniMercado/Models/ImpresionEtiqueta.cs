using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MiniMercado.Models;

namespace MiniMercado.Models
{
    public class ImpresionEtiqueta : ITenantEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        [Required]
        public int TenantId { get; set; }

        [ForeignKey("TenantId")]
        public Tenant Tenant { get; set; }

        [Required]
        public string UsuarioId { get; set; }

        public int Cantidad { get; set; }

        [Required]
        public string TipoEtiqueta { get; set; } // Chica, Mediana, Grande

        public bool IncluyePrecio { get; set; }

        public DateTime FechaImpresion { get; set; } = DateTime.Now;
    }
}

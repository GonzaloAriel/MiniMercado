using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniMercado.Models
{
    public class ProductoBase
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200)]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El código de barras es obligatorio")]
        [StringLength(50)]
        public string CodigoBarras { get; set; }

        [StringLength(100)]
        public string? Marca { get; set; }

        [StringLength(100)]
        public string? Categoria { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrecioReferencia { get; set; }
    }
}
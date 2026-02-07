
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniMercado.Models
{
    public class MovimientoStock
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un producto")]
        public int? ProductoId { get; set; }

        [ValidateNever]   // 🔑 CLAVE
        public Producto Producto { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Debe ingresar cantidad")]
        [Column(TypeName = "decimal(18,3)")]
        public decimal Cantidad { get; set; }

        [Required]
        [StringLength(20)]
        public string Tipo { get; set; }

        [StringLength(200)]
        public string? Observacion { get; set; }

    }
}

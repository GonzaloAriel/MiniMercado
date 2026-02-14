using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MiniMercado.Models
{
    public class Venta
    {
        public int Id { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        public decimal Total { get; set; }

        public bool Activa { get; set; } = true;

        public string? MotivoAnulacion { get; set; }

        [ValidateNever]
        public List<VentaDetalle> Detalles { get; set; } = new();
    }
}

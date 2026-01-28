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

        public List<VentaDetalle> Detalles { get; set; }
    }
}

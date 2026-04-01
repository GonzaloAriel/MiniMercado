using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MiniMercado.Models
{
    public class Venta : ITenantEntity
    {
        public int Id { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        public decimal Total { get; set; }

        public bool Activa { get; set; } = true;

        public string? MotivoAnulacion { get; set; }

        [ValidateNever]
        public List<VentaDetalle> Detalles { get; set; } = new();

        [ValidateNever]
        public int TenantId { get; set; }
        [ValidateNever]
        public Tenant Tenant { get; set; }

        public string? NombreVenta { get; set; }
        public string? UsuarioId { get; set; }
        public string? Usuario { get; set; }

        public bool EsFiado { get; set; } = false;

        public int? ClienteId { get; set; }

        public int? CajaId { get; set; }
        public Caja? Caja { get; set; }

        

    }
}

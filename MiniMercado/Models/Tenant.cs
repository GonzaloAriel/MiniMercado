using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace MiniMercado.Models
{
    public class Tenant
    {
        public int Id { get; set; }

        [Required]
        public string NombreComercio { get; set; }

        public string? Direccion { get; set; }

        public string? Telefono { get; set; }

        public string? Email { get; set; }

        public string? CUIT { get; set; }

        public string? MensajeTicket { get; set; } // ej: "Gracias por su compra!"

        public bool Activo { get; set; } = true;
    }
}

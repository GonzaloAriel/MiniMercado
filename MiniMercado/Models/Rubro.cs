using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MiniMercado.Models
{
    public class Rubro : ITenantEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del rubro es obligatorio")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string Nombre { get; set; }

        [ValidateNever]
        public List<Producto> Productos { get; set; }

        [ValidateNever] public int TenantId { get; set; }

        [ValidateNever] public Tenant Tenant { get; set; }
        public bool Activo { get; set; } = true;
    }
}

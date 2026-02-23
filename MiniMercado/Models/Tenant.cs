using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MiniMercado.Models
{
    public class Tenant
    {
        public int Id { get; set; }

        [Required]
        public string? NombreComercio { get; set; }

        public bool Activo { get; set; } = true;

    }
}

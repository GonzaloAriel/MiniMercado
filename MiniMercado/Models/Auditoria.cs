using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MiniMercado.Models
{
    public class Auditoria
    {
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required(ErrorMessage = "Usuario es obligatorio")]
        [StringLength(450, ErrorMessage = "Máximo 450 caracteres")]
        public string UsuarioId { get; set; }

        [Required(ErrorMessage = "Accion es obligatorio")]
        [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
        public string Accion { get; set; }

        [Required(ErrorMessage = "Tabla es obligatorio")]
        [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
        public string Tabla { get; set; }

        public int? RegistroId { get; set; }

        [Required(ErrorMessage = "Descripcion es obligatorio")]
        [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
        public string Descripcion { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}

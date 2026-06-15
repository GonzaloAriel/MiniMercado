using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniMercado.Models
{
    public class EgresoCaja : ITenantEntity
    {
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "El monto es obligatorio")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "La categoría es obligatoria")]
        public CategoriaEgreso Categoria { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(200, ErrorMessage = "La descripción no puede superar los 200 caracteres")]
        public string Descripcion { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "El beneficiario no puede superar los 100 caracteres")]
        public string? Beneficiario { get; set; }

        public string? UsuarioId { get; set; }
        public ApplicationUser? Usuario { get; set; }

        public int? CajaId { get; set; }
        public Caja? Caja { get; set; }

        public int? ProveedorId { get; set; }
        public Proveedor? Proveedor { get; set; }

        public string? Observaciones { get; set; }

        public bool Anulado { get; set; } = false;
    }
}

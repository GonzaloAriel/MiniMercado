using System;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace MiniMercado.Models
{
    public class Producto : ITenantEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El código de barra es obligatorio")]
        [StringLength(50)]
        public string CodigoBarra { get; set; }


        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string Nombre { get; set; }


        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, 999999, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }


        [Required(ErrorMessage = "El stock es obligatorio")]
        [Range(0, 100000, ErrorMessage = "El stock no puede ser negativo")]
        public decimal Stock { get; set; }

        public bool EsPorPeso { get; set; } = true;

        public bool PrecioEditable { get; set; } = false;

        public bool Activo { get; set; } = true;

        [Display(Name = "Fecha de Vencimiento")]
        [DataType(DataType.Date)]
        public DateTime? FechaVencimiento { get; set; }

        [ValidateNever]
        public List<MovimientoStock> Movimientos { get; set; }

        [ValidateNever]
        public int TenantId { get; set; }

        [ValidateNever]
        public Tenant Tenant { get; set; }

        // Relación con Proveedor (principal)
        [Display(Name = "Proveedor Principal")]
        public int? ProveedorId { get; set; }

        [ValidateNever]
        public Proveedor? Proveedor { get; set; }

        [ValidateNever]
        public List<ProductoProveedor> Proveedores { get; set; } = new List<ProductoProveedor>();

        // Relación con Rubro (categoria). Opcional: el FK es nullable y la
        // relación usa ON DELETE SET NULL, así que el modelo no puede declararla
        // obligatoria. Ver D-013.
        public int? RubroId { get; set; }

        [ValidateNever]
        public Rubro? Rubro { get; set; }

        // Vínculo opcional con el catálogo global (ProductoBase)
        public int? ProductoBaseId { get; set; }

        [ValidateNever]
        public ProductoBase? ProductoBase { get; set; }

        // Propiedades calculadas para UI
        [NotMapped]
        public string NombreDisplay => ProductoBase?.Nombre ?? Nombre;

        [NotMapped]
        public string CodigoDisplay => ProductoBase?.CodigoBarras ?? CodigoBarra;

        [NotMapped]
        public DateTime? FechaProximoVencimiento { get; set; }

    }
}

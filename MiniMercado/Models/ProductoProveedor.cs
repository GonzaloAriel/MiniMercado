using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniMercado.Models
{
    public class ProductoProveedor
    {
        public int ProductoId { get; set; }

        public int ProveedorId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrecioCosto { get; set; }

        public bool EsPrincipal { get; set; }

        public virtual Producto Producto { get; set; }

        public virtual Proveedor Proveedor { get; set; }
    }
}

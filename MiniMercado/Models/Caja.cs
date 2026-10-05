using System;
using System.ComponentModel.DataAnnotations;

namespace MiniMercado.Models
{
    public class Caja : ITenantEntity
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;
        public ApplicationUser? Usuario { get; set; }

        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }

        public decimal MontoInicial { get; set; }
        public decimal TotalVentas { get; set; }
        public decimal? MontoReal { get; set; }
        public decimal? Diferencia { get; set; }

        [MaxLength(20)]
        public string Estado { get; set; } = "ABIERTA"; // ABIERTA / CERRADA / AUTOMATICA

        /// <summary>
        /// Saldo que deberia haber en el cajon al cerrar: lo que se abrio, mas lo que se
        /// vendio, menos lo que salio por egresos.
        ///
        /// Esta es la UNICA definicion de la formula (P-032). Antes estaba escrita en tres
        /// lugares —este mismo calculo en <c>CajaController.CerrarCaja</c>, y en
        /// <c>Views/Caja/CerrarCaja.cshtml</c> y <c>Views/Caja/ResumenCierre.cshtml</c>— y las
        /// tres tenian que coincidir a mano. Si el controller cambiaba y las vistas no, el
        /// usuario veia un "Esperado" distinto del <see cref="Diferencia"/> que quedo
        /// guardado, y ningun test que no renderizara el HTML lo detectaba.
        ///
        /// Los egresos entran como parametro porque viven en otra tabla y se suman por
        /// consulta; el resto ya esta en la caja.
        /// </summary>
        public decimal CalcularSaldoEsperado(decimal totalEgresos) => MontoInicial + TotalVentas - totalEgresos;
    }
}

using Microsoft.AspNetCore.Identity;

namespace MiniMercado.Models
{
    public class ApplicationUser : IdentityUser
    {
        // En el futuro podés agregar:
        // Nombre
        // Apellido
        // ClienteId (si es multi-tenant)
        public int? TenantId { get; set; }

        public Tenant? Tenant { get; set; }

    }
}

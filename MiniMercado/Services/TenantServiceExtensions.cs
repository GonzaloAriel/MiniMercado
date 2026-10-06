namespace MiniMercado.Services
{
    /// <summary>
    /// Resolucion fail-closed del TenantId (P-007).
    ///
    /// Desenvolver el claim con <c>Value</c> a secas lanzaba excepcion cuando no
    /// existia (AdminGlobal, usuarios sin credencial) y el usuario veia un 500. Este helper
    /// devuelve <c>false</c> cuando no hay claim, y el caller deniega de forma
    /// explicita antes de tocar cualquier dato.
    ///
    /// Patron de denegacion de referencia: <c>EtiquetasController</c>
    /// (<c>if (!tenantId.HasValue) return Unauthorized();</c>).
    /// </summary>
    public static class TenantServiceExtensions
    {
        public static bool TryGetTenantId(this ITenantService servicio, out int tenantId)
        {
            var id = servicio.GetTenantId();
            tenantId = id ?? 0;
            return id.HasValue;
        }
    }
}

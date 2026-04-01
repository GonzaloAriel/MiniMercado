namespace MiniMercado.Models
{
    public class Configuracion : ITenantEntity
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public bool UsarCaja { get; set; } = false;
    }
}

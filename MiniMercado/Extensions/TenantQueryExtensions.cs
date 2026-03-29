using System.Linq;
using MiniMercado.Models;

namespace MiniMercado.Extensions
{
    public static class TenantQueryExtensions
    {
        /// <summary>
        /// Aplica automáticamente el filtro de TenantId a una consulta LINQ.
        /// </summary>
        /// <typeparam name="T">Entidad que implementa ITenantEntity</typeparam>
        /// <param name="query">Consulta original</param>
        /// <param name="tenantId">ID del tenant por el cual filtrar</param>
        /// <returns>Consulta filtrada</returns>
        public static IQueryable<T> ApplyTenantFilter<T>(this IQueryable<T> query, int tenantId) 
            where T : class, ITenantEntity
        {
            return query.Where(x => x.TenantId == tenantId);
        }
    }
}
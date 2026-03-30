using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;

namespace MiniMercado.Services
{
    public interface INotificacionService
    {
        Task CrearNotificacion(string titulo, string mensaje, string tipo, string? url = null);
        Task<List<Notificacion>> GetNotificacionesNoLeidas();
        Task MarcarComoLeida(int id);
    }

    public class NotificacionService : INotificacionService
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;

        public NotificacionService(MiniMercadoContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public async Task CrearNotificacion(string titulo, string mensaje, string tipo, string? url = null)
        {
            var tenantId = _tenantService.GetTenantId();
            if (tenantId == null) return;

            var notificacion = new Notificacion
            {
                TenantId = tenantId.Value,
                Titulo = titulo,
                Mensaje = mensaje,
                Tipo = tipo,
                Url = url,
                Fecha = DateTime.Now,
                Leida = false
            };

            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notificacion>> GetNotificacionesNoLeidas()
        {
            var tenantId = _tenantService.GetTenantId();
            if (tenantId == null) return new List<Notificacion>();

            return await _context.Notificaciones
                .Where(n => n.TenantId == tenantId.Value && !n.Leida)
                .OrderByDescending(n => n.Fecha)
                .Take(10)
                .ToListAsync();
        }

        public async Task MarcarComoLeida(int id)
        {
            var tenantId = _tenantService.GetTenantId();
            if (tenantId == null) return;

            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.Id == id && n.TenantId == tenantId.Value);

            if (notificacion != null)
            {
                notificacion.Leida = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}

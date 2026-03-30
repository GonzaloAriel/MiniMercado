using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;

namespace MiniMercado.Services
{
    public interface IVencimientoService
    {
        Task VerificarProductosVencidos();
    }

    public class VencimientoService : IVencimientoService
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;
        private readonly INotificacionService _notificacionService;

        public VencimientoService(MiniMercadoContext context, 
                                 ITenantService tenantService, 
                                 INotificacionService notificacionService)
        {
            _context = context;
            _tenantService = tenantService;
            _notificacionService = notificacionService;
        }

        public async Task VerificarProductosVencidos()
        {
            var tenantId = _tenantService.GetTenantId();
            if (tenantId == null) return;

            DateTime hoy = DateTime.Today;
            DateTime limite = hoy.AddDays(3);

            // Buscar productos por vencer o vencidos con stock
            var productos = await _context.Productos
                .Where(p => p.TenantId == tenantId.Value 
                            && p.Activo 
                            && p.Stock > 0 
                            && p.FechaVencimiento != null 
                            && p.FechaVencimiento <= limite)
                .ToListAsync();

            foreach (var p in productos)
            {
                bool vencido = p.FechaVencimiento < hoy;
                string tipoNotif = vencido ? "Vencido" : "PorVencer";
                string titulo = vencido ? "Producto Vencido 🔴" : "Producto por vencer 🟡";
                
                // Verificar si ya existe una notificación NO LEÍDA para este producto y tipo
                bool existe = await _context.Notificaciones
                    .AnyAsync(n => n.TenantId == tenantId.Value 
                                   && !n.Leida 
                                   && n.Tipo == tipoNotif 
                                   && n.Url.Contains($"/Productos/Edit/{p.Id}"));

                if (!existe)
                {
                    string mensaje = vencido 
                        ? $"El producto '{p.Nombre}' venció el {p.FechaVencimiento?.ToShortDateString()}. Favor de retirar de góndola."
                        : $"El producto '{p.Nombre}' vence el {p.FechaVencimiento?.ToShortDateString()}.";

                    await _notificacionService.CrearNotificacion(
                        titulo,
                        mensaje,
                        tipoNotif,
                        $"/Productos/Edit/{p.Id}"
                    );
                }
            }
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniMercado.Models;
using MiniMercado.Services;
using System.Linq;

namespace MiniMercado.Controllers
{
    [Authorize]
    public class AlertasController : Controller
    {
        private readonly ITenantService _tenantService;
        private readonly AlertasService _alertasService;

        public AlertasController(ITenantService tenantService, AlertasService alertasService)
        {
            _tenantService = tenantService;
            _alertasService = alertasService;
        }

        public async Task<IActionResult> Vencimientos(int page = 1)
        {
            if (page < 1) page = 1;
            var tenantId = _tenantService.GetTenantId().Value;
            int pageSize = 10;

            var result = await _alertasService.GetVencimientosPaginated(tenantId, page, pageSize);

            ViewBag.PaginaActual = page;
            ViewBag.TotalPaginas = (int)Math.Ceiling(result.Total / (double)pageSize);

            return View(result.Lista);
        }

        public async Task<IActionResult> StockBajo(int page = 1)
        {
            if (page < 1) page = 1;
            var tenantId = _tenantService.GetTenantId().Value;
            int pageSize = 10;

            var result = await _alertasService.GetStockBajoPaginated(tenantId, page, pageSize);

            ViewBag.PaginaActual = page;
            ViewBag.TotalPaginas = (int)Math.Ceiling(result.Total / (double)pageSize);

            return View(result.Lista);
        }

        public async Task<IActionResult> StockNegativo(int page = 1)
        {
            if (page < 1) page = 1;
            var tenantId = _tenantService.GetTenantId().Value;
            int pageSize = 10;

            var result = await _alertasService.GetStockNegativoPaginated(tenantId, page, pageSize);

            ViewBag.PaginaActual = page;
            ViewBag.TotalPaginas = (int)Math.Ceiling(result.Total / (double)pageSize);

            return View(result.Lista);
        }
    }
}

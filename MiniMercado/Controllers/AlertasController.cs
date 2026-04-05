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

        public IActionResult Vencimientos()
        {
            var tenantId = _tenantService.GetTenantId().Value;
            var vencimientos = _alertasService.GetVencimientos(tenantId);
            return View(vencimientos);
        }

        public IActionResult StockBajo()
        {
            var tenantId = _tenantService.GetTenantId().Value;
            var stockBajo = _alertasService.GetStockBajo(tenantId);
            return View(stockBajo);
        }

        public IActionResult StockNegativo()
        {
            var tenantId = _tenantService.GetTenantId().Value;
            var stockNegativo = _alertasService.GetStockNegativo(tenantId);
            return View(stockNegativo);
        }
    }
}

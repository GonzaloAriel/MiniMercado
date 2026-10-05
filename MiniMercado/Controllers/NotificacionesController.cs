using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniMercado.Services;

namespace MiniMercado.Controllers
{
    [Authorize(Roles = "Admin,Caja")]
    public class NotificacionesController : Controller
    {
        private readonly INotificacionService _notificacionService;

        public NotificacionesController(INotificacionService notificacionService)
        {
            _notificacionService = notificacionService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var notificaciones = await _notificacionService.GetNotificacionesNoLeidas();
            return Json(notificaciones);
        }

        [HttpPost]
        public async Task<IActionResult> MarcarLeida(int id)
        {
            await _notificacionService.MarcarComoLeida(id);
            return Ok();
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiniMercado.Controllers
{
    public class CuentaController : Controller
    {
        [AllowAnonymous]
        [Route("Cuenta/Bloqueado")]
        public IActionResult Bloqueado()
        {
            return View();
        }
    }
}

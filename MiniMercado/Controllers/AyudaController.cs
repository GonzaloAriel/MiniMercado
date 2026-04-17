using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiniMercado.Controllers
{
    [Authorize]
    public class AyudaController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

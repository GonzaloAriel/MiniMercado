using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace MiniMercado.Tests
{
    /// <summary>
    /// Arma el HttpContext minimo que necesitan los controllers para ejecutarse fuera de
    /// ASP.NET: un principal con los claims que leen y un UrlHelper para que
    /// RedirectToAction no reviente.
    ///
    /// Es mas barato que WebApplicationFactory para lo que se quiere probar aca: la
    /// formula del cierre y las guardas de ownership son logica del controller, no
    /// comportamiento de routing ni de middleware.
    /// </summary>
    public static class ControllerTestHelper
    {
        public static T ConClaims<T>(
            this T controller,
            string userId,
            string role = "Caja") where T : Controller
        {
            var identidad = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, userId),
                    new Claim(ClaimTypes.Role, role)
                },
                authenticationType: "TestAuth");

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identidad)
            };

            // ControllerContext exige un ControllerActionDescriptor: con un
            // ActionDescriptor generico tira ArgumentException en el constructor.
            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ControllerActionDescriptor());

            controller.ControllerContext = new ControllerContext(actionContext);
            controller.Url = new UrlHelper(actionContext);
            controller.TempData = new TempDataStub();

            return controller;
        }

        /// <summary>
        /// TempData minimo en memoria. VentaController escribe TempData["Mensaje"] al
        /// final del POST; sin esto el DefaultHttpContext no lo tiene y revienta con NRE.
        /// La implementacion real depende de la sesion, que en un test no existe.
        /// </summary>
        private class TempDataStub : Dictionary<string, object?>, ITempDataDictionary
        {
            // ITempDataDictionary.Remove no devuelve nada y Dictionary.Remove si, asi que
            // se oculta con 'new' para que las dos interfaces queden satisfechas.
            public new void Remove(string key)
            {
            }

            public void Keep()
            {
            }

            public void Keep(string key)
            {
            }

            public object? Peek(string key) => TryGetValue(key, out var valor) ? valor : null;

            public void Load()
            {
            }

            public void Save()
            {
            }
        }
    }
}
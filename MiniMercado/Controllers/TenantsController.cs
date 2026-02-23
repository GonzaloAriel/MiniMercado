using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiniMercado.Data;
using MiniMercado.Models;

namespace MiniMercado.Controllers
{
    [Authorize(Roles = "AdminGlobal")]
    public class TenantsController : Controller
    {
        private readonly MiniMercadoContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TenantsController(
            MiniMercadoContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var tenants = _context.Tenants.ToList();
            return View(tenants);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string nombreComercio, string adminEmail, string password)
        {
            if (string.IsNullOrEmpty(nombreComercio) ||
                string.IsNullOrEmpty(adminEmail) ||
                string.IsNullOrEmpty(password))
            {
                return View();
            }

            // 1️⃣ Crear Tenant
            var tenant = new Tenant
            {
                NombreComercio = nombreComercio,
                Activo = true
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            // 2️⃣ Crear usuario Admin del Tenant
            var user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                TenantId = tenant.Id
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Admin");
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Toggle(int id)
        {
            var tenant = _context.Tenants.Find(id);
            if (tenant == null) return NotFound();

            tenant.Activo = !tenant.Activo;
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }


        [AllowAnonymous]
        [Route("Tenant/Desactivado")]
        public IActionResult Desactivado()
        {
            return View();
        }

    }
}

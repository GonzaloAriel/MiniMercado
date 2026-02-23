using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiniMercado.Models;
using MiniMercado.Models.ViewModels;

[Authorize(Roles = "AdminGlobal, Admin")]
public class UsuariosController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITenantService _tenantService;


    public UsuariosController(
     UserManager<ApplicationUser> userManager,
     RoleManager<IdentityRole> roleManager,
     ITenantService tenantService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tenantService = tenantService;
    }


    // GET
    [Authorize(Roles = "AdminGlobal, Admin")]
    public IActionResult Create()
    {
        ViewBag.Roles = _roleManager.Roles
            .Where(r => r.Name != "AdminGlobal")
            .ToList();

        return View();
    }

    // POST
    [HttpPost]
    [Authorize(Roles = "AdminGlobal, Admin")]
    public async Task<IActionResult> Create(CrearUsuarioVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = _roleManager.Roles
                .Where(r => r.Name != "AdminGlobal")
                .ToList();

            return View(model);
        }

        var tenantId = _tenantService.GetTenantId();

        if (!tenantId.HasValue)
            return Unauthorized();

        // Verificar que el rol exista
        var roleExists = await _roleManager.RoleExistsAsync(model.Rol);
        if (!roleExists)
        {
            ModelState.AddModelError("", "Rol inválido.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            TenantId = tenantId.Value
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Rol);
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        ViewBag.Roles = _roleManager.Roles
            .Where(r => r.Name != "AdminGlobal")
            .ToList();

        return View(model);
    }

}

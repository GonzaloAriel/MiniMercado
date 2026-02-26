using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Models.ViewModels;

[Authorize(Roles = "AdminGlobal")]
public class AdminGlobalUsuariosController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MiniMercadoContext _context;

    public AdminGlobalUsuariosController(
        UserManager<ApplicationUser> userManager,
        MiniMercadoContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    // ===============================
    // LISTADO DE TENANTS
    // ===============================
    public IActionResult Tenants()
    {
        var tenants = _context.Tenants.ToList();
        return View(tenants);
    }

    // ===============================
    // USUARIOS POR TENANT
    // ===============================
    public async Task<IActionResult> UsuariosPorTenant(int tenantId)
    {
        var tenant = _context.Tenants.FirstOrDefault(t => t.Id == tenantId);

        if (tenant == null)
            return NotFound();

        var usuarios = _userManager.Users
            .Where(u => u.TenantId == tenantId)
            .ToList();

        var lista = new List<UsuarioListadoVM>();

        foreach (var user in usuarios)
        {
            var roles = await _userManager.GetRolesAsync(user);

            lista.Add(new UsuarioListadoVM
            {
                Id = user.Id,
                Email = user.Email,
                Rol = roles.FirstOrDefault()
            });
        }

        ViewBag.TenantNombre = tenant.NombreComercio;
        ViewBag.TenantId = tenant.Id;

        return View(lista);
    }

    // ===============================
    // RESET PASSWORD
    // ===============================
    public IActionResult ResetPassword(string id)
    {
        return View(new CambiarPasswordVM { Id = id });
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(CambiarPasswordVM model)
    {
        var user = await _userManager.FindByIdAsync(model.Id);

        if (user == null)
            return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var result = await _userManager.ResetPasswordAsync(user, token, model.NuevaPassword);

        if (result.Succeeded)
        {
            TempData["Success"] = "Contraseña actualizada correctamente.";
            return RedirectToAction(nameof(Tenants));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return View(model);
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Models;
using MiniMercado.Models.ViewModels;
using MiniMercado.Services;

[Authorize(Roles = "Admin")]
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

    // ============================
    // LISTADO
    // ============================
    public async Task<IActionResult> Index(int page = 1)
    {
        if (page < 1) page = 1;
        var tenantId = _tenantService.GetTenantId();

        if (!tenantId.HasValue)
            return Unauthorized();

        int pageSize = 10;
        var query = _userManager.Users
            .Where(u => u.TenantId == tenantId.Value);

        var totalRegistros = await query.CountAsync();
        var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

        var usuarios = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

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

        ViewBag.PaginaActual = page;
        ViewBag.TotalPaginas = totalPaginas;

        return View(lista);
    }

    // ============================
    // CREATE
    // ============================
    public IActionResult Create()
    {
        ViewBag.Roles = _roleManager.Roles
            .Where(r => r.Name != "AdminGlobal")
            .ToList();

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(CrearUsuarioVM model)
    {
        var tenantId = _tenantService.GetTenantId();

        if (!tenantId.HasValue)
            return Unauthorized();

        if (!ModelState.IsValid)
        {
            ViewBag.Roles = _roleManager.Roles
                .Where(r => r.Name != "AdminGlobal")
                .ToList();

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
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        ViewBag.Roles = _roleManager.Roles
            .Where(r => r.Name != "AdminGlobal")
            .ToList();

        return View(model);
    }

    // ============================
    // EDIT
    // ============================
    public async Task<IActionResult> Edit(string id)
    {
        var tenantId = _tenantService.GetTenantId();

        if (!tenantId.HasValue)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(id);

        if (user == null || user.TenantId != tenantId.Value)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        var model = new EditUsuarioVM
        {
            Id = user.Id,
            Email = user.Email,
            Rol = roles.FirstOrDefault()
        };

        ViewBag.Roles = _roleManager.Roles
            .Where(r => r.Name != "AdminGlobal")
            .ToList();

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditUsuarioVM model)
    {
        var tenantId = _tenantService.GetTenantId();

        if (!tenantId.HasValue)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(model.Id);

        if (user == null || user.TenantId != tenantId.Value)
            return NotFound();

        user.Email = model.Email;
        user.UserName = model.Email;

        await _userManager.UpdateAsync(user);

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, model.Rol);

        return RedirectToAction(nameof(Index));
    }

    // ============================
    // CAMBIAR PASSWORD
    // ============================
    public IActionResult CambiarPassword(string id)
    {
        return View(new CambiarPasswordVM { Id = id });
    }

    [HttpPost]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordVM model)
    {
        var tenantId = _tenantService.GetTenantId();

        if (!tenantId.HasValue)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(model.Id);

        if (user == null || user.TenantId != tenantId.Value)
            return NotFound();

        if (!ModelState.IsValid)
            return View(model);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var result = await _userManager.ResetPasswordAsync(user, token, model.NuevaPassword);

        if (result.Succeeded)
            return RedirectToAction(nameof(Index));

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return View(model);
    }

    // ============================
    // DELETE
    // ============================
    public async Task<IActionResult> Delete(string id)
    {
        var tenantId = _tenantService.GetTenantId();

        if (!tenantId.HasValue)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(id);

        if (user == null || user.TenantId != tenantId.Value)
            return NotFound();

        // 🔒 No permitir borrarse a sí mismo
        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "No puedes eliminar tu propio usuario.";
            return RedirectToAction(nameof(Index));
        }

        await _userManager.DeleteAsync(user);

        return RedirectToAction(nameof(Index));
    }
}
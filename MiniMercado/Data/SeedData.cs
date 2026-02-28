using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;

public static class SeedData
{
    public static async Task Inicializar(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = serviceProvider.GetRequiredService<MiniMercadoContext>();

        // 🔹 1. Crear Roles
        string[] roles = { "AdminGlobal", "Admin", "Caja" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // 🔹 2. Crear Tenant Demo si no existe
        Tenant tenantDemo;

        if (!db.Tenants.Any())
        {
            tenantDemo = new Tenant
            {
                NombreComercio = "MiniMercado Demo",
                Activo = true
            };

            db.Tenants.Add(tenantDemo);
            await db.SaveChangesAsync();
        }
        else
        {
            tenantDemo = await db.Tenants.FirstAsync();
        }

        // 🔹 3. Crear AdminGlobal
        var globalEmail = "global@admin.com";
        var globalUser = await userManager.FindByEmailAsync(globalEmail);

        if (globalUser == null)
        {
            globalUser = new ApplicationUser
            {
                UserName = globalEmail,
                Email = globalEmail,
                EmailConfirmed = true,
                TenantId = null // AdminGlobal no pertenece a tenant
            };

            var result = await userManager.CreateAsync(globalUser, "Admin123!");

            if (result.Succeeded)
                await userManager.AddToRoleAsync(globalUser, "AdminGlobal");
        }

        // 🔹 4. Crear Admin del Tenant Demo
        var adminEmail = "admin@demo.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                TenantId = tenantDemo.Id
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");

            if (result.Succeeded)
                await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // 🔹 5. Crear Usuario Caja Demo
        var cajaEmail = "caja@demo.com";
        var cajaUser = await userManager.FindByEmailAsync(cajaEmail);

        if (cajaUser == null)
        {
            cajaUser = new ApplicationUser
            {
                UserName = cajaEmail,
                Email = cajaEmail,
                EmailConfirmed = true,
                TenantId = tenantDemo.Id
            };

            var result = await userManager.CreateAsync(cajaUser, "Caja123!");

            if (result.Succeeded)
                await userManager.AddToRoleAsync(cajaUser, "Caja");
        }
    }
}
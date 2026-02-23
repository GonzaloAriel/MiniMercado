using Microsoft.AspNetCore.Identity;
using MiniMercado.Models;

public static class SeedData
{
    public static async Task CrearRoles(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("AdminGlobal"))
            await roleManager.CreateAsync(new IdentityRole("AdminGlobal"));
    }

    public static async Task CrearAdmin(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var adminEmail = "admin@minimercado.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            var user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                TenantId = null
            };

            var result = await userManager.CreateAsync(user, "Admin123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "AdminGlobal");
            }
        }
    }
}

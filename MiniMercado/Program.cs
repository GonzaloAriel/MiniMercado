using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Middleware;
using MiniMercado.Models;
using MiniMercado.Services;
using System.Globalization;


var builder = WebApplication.CreateBuilder(args);

// MVC + Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// DB
builder.Services.AddDbContext<MiniMercadoContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MiniMercadoDB")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<MiniMercadoContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Tenant/Desactivado";
}
);

builder.Services.AddScoped<
    IUserClaimsPrincipalFactory<ApplicationUser>,
    CustomClaimsPrincipalFactory>();


builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ITenantService, TenantService>();


var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

var culture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<TenantValidationMiddleware>();
app.UseAuthorization();

app.MapRazorPages();

// Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");



// Seed roles + admin
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Admin", "Caja", "AdminGlobal" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var email = "global@admin.com";
    var user = await userManager.FindByEmailAsync(email);

    if (user == null)
    {
        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, "Admin123!");
        await userManager.AddToRoleAsync(user, "AdminGlobal");
    }

    // Crear Admin para Tenant 1
    var tenantAdminEmail = "admin@demo.com";

    var tenantAdmin = userManager.FindByEmailAsync(tenantAdminEmail)
        .GetAwaiter().GetResult();

    if (tenantAdmin == null)
    {
        tenantAdmin = new ApplicationUser
        {
            UserName = tenantAdminEmail,
            Email = tenantAdminEmail,
            EmailConfirmed = true,
            TenantId = 1 // 👈 Tenant MiniMercado Demo
        };

        userManager.CreateAsync(tenantAdmin, "Admin123!")
            .GetAwaiter().GetResult();

        userManager.AddToRoleAsync(tenantAdmin, "Admin")
            .GetAwaiter().GetResult();
    }

}


app.Run();

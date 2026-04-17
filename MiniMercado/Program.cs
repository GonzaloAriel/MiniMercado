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
builder.Services.AddMemoryCache();

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
});

builder.Services.AddScoped<
    IUserClaimsPrincipalFactory<ApplicationUser>,
    CustomClaimsPrincipalFactory>();

builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ISuscripcionService, SuscripcionService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<ProductoService>();
builder.Services.AddScoped<ProveedorService>();
builder.Services.AddScoped<INotificacionService, NotificacionService>();
builder.Services.AddScoped<AlertasService>();
builder.Services.AddHttpContextAccessor();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MiniMercadoContext>();
    
    // ⚡ Parche de emergencia: Asegurar columnas de suscripción
    string sql = @"
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tenants') AND name = 'Plan')
        BEGIN
            ALTER TABLE Tenants ADD 
                [Plan] nvarchar(max) NOT NULL DEFAULT 'Mensual',
                FechaInicioSuscripcion datetime2 NULL,
                FechaProximoPago datetime2 NULL,
                EstadoSuscripcion int NOT NULL DEFAULT 1,
                MesesPagados int NOT NULL DEFAULT 0,
                SaldoAFavorMeses int NOT NULL DEFAULT 0,
                UltimoPagoRegistrado datetime2 NULL;
        END";
    db.Database.ExecuteSqlRaw(sql);

    db.Database.Migrate();
}

// 🔹 Seed automático (Roles + Tenant Demo + AdminGlobal)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedData.Inicializar(services);
}

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

app.Run();
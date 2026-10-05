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
builder.Services.AddScoped<ProductoImportService>();


var app = builder.Build();

// ============================================================================
// Arranque de la base: migraciones y seed.
//
// Son pasos de un solo escritor, no parte del arranque. EF Core 6 no tiene lock
// de migraciones (eso llego en EF Core 9): si dos replicas levantan a la vez,
// ambas intentan aplicar la misma migracion y una se traba o muere. El seed
// tiene el mismo problema, con guardas Any() / RoleExistsAsync() que son
// TOCTOU: dos réplicas pueden insertar los mismos roles o el mismo catalogo
// global, que tiene indice unico en CodigoBarras.
//
// Por eso en Development se mantienen activas (levantas y anda) y en cualquier
// otro entorno hay que pedirlas explicitamente. Ver P-026.
//
//   Bootstrap__ApplyMigrations=true   aplica las migraciones pendientes
//   Bootstrap__Seed=true              crea roles, tenant demo y catalogo
//
// Sin banderas, el comportamiento por defecto es: activas en Development,
// inactivas en los demas entornos.
// ============================================================================

var aplicarMigraciones = BanderaDeBootstrap(builder.Configuration, builder.Environment, "Bootstrap:ApplyMigrations");
var aplicarSeed = BanderaDeBootstrap(builder.Configuration, builder.Environment, "Bootstrap:Seed");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Bootstrap");
    var db = services.GetRequiredService<MiniMercadoContext>();

    if (aplicarMigraciones)
    {
        try
        {
            db.Database.Migrate();
            logger.LogInformation("Arranque de la base: migraciones aplicadas.");
        }
        catch (Exception ex)
        {
            // Se pide explicitamente, asi que si falla no tiene sentido seguir.
            logger.LogCritical(ex, "No se pudieron aplicar las migraciones. La base puede no existir, no ser alcanzable, o estar en un estado incompatible.");
            throw;
        }
    }
    else
    {
        // No se aplican, pero avisar si la base esta atrasada. Sin esto, un
        // deploy que se olvida de migrar se ve como "la app levanta y despues
        // da 500", que es el peor modo de falla.
        logger.LogInformation("Arranque de la base: migraciones desactivadas (Bootstrap:ApplyMigrations).");
        try
        {
            // Ojo: si la base no existe todavia, EF no tira excepcion, responde
            // con TODAS las migraciones pendientes. Sin recortar, el log es un
            // muro de texto ilegible en el caso mas comun.
            var pendientes = (await db.Database.GetPendingMigrationsAsync()).ToList();
            if (pendientes.Count > 0)
            {
                const int maxDetalle = 3;
                var detalle = string.Join(", ", pendientes.Take(maxDetalle));
                var resto = pendientes.Count - Math.Min(maxDetalle, pendientes.Count);
                logger.LogWarning(
                    "La base esta atrasada: {Cantidad} migracion(es) sin aplicar, y no se van a aplicar porque Bootstrap:ApplyMigrations esta desactivado. Primera: {Detalle}{Resto}. Las consultas que las usen van a fallar.",
                    pendientes.Count, detalle,
                    resto > 0 ? $" (y {resto} mas)" : "");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudieron consultar las migraciones pendientes: la base puede no ser alcanzable.");
        }
    }

    if (aplicarSeed)
    {
        try
        {
            // Roles + Tenant Demo + AdminGlobal + catalogo global de prueba.
            await SeedData.Inicializar(services);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Fallo el seed de datos iniciales.");
            throw;
        }
    }
    else
    {
        logger.LogInformation("Arranque de la base: seed desactivado (Bootstrap:Seed).");
    }
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

// Una bandera de arranque no definida usa el comportamiento por defecto del
// entorno: activo en Development para no romper el flujo local, inactivo en los
// demas, donde las migraciones tienen que ser un paso del deploy.
bool BanderaDeBootstrap(IConfiguration configuration, IWebHostEnvironment environment, string clave)
{
    if (bool.TryParse(configuration[clave], out var valor))
        return valor;

    return environment.IsDevelopment();
}
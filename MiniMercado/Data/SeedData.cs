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

            var result = await userManager.CreateAsync(globalUser, "Zubzero123!!!");

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

        // 🔹 6. Catálogo Global (ProductoBase) de prueba
        if (!db.ProductosBase.Any())
        {
            db.ProductosBase.AddRange(
                new ProductoBase { Nombre = "Coca Cola 1.5L", CodigoBarras = "7790200111452", Marca = "Coca Cola", Categoria = "Bebidas", PrecioReferencia = 1500 },
                new ProductoBase { Nombre = "Agua Villavicencio 500ml", CodigoBarras = "7790200110301", Marca = "Danone", Categoria = "Bebidas", PrecioReferencia = 800 },
                new ProductoBase { Nombre = "Alfajor Jorgito Chocolate", CodigoBarras = "7790530000103", Marca = "Jorgito", Categoria = "Golosinas", PrecioReferencia = 600 },
                new ProductoBase { Nombre = "Galletitas Oreo 118g", CodigoBarras = "7622300840251", Marca = "Mondelez", Categoria = "Galletitas", PrecioReferencia = 1200 },
                new ProductoBase { Nombre = "Leche La Serenisima 1L", CodigoBarras = "7790742330703", Marca = "La Serenisima", Categoria = "Lácteos", PrecioReferencia = 1100 },
                new ProductoBase { Nombre = "Yerba Mate Taragüi 500g", CodigoBarras = "7790387013624", Marca = "Las Marías", Categoria = "Almacén", PrecioReferencia = 2500 },
                new ProductoBase { Nombre = "Pan Lactal Blanco 400g", CodigoBarras = "7790060023155", Marca = "Bimbo", Categoria = "Almacén", PrecioReferencia = 1800 },
                new ProductoBase { Nombre = "Cerveza Quilmes Lata 473ml", CodigoBarras = "7792758000720", Marca = "Quilmes", Categoria = "Bebidas", PrecioReferencia = 1300 },
                new ProductoBase { Nombre = "Papas Lays Clásicas 150g", CodigoBarras = "7790040110202", Marca = "PepsiCo", Categoria = "Snacks", PrecioReferencia = 2200 },
                new ProductoBase { Nombre = "Fideos Lucchetti Tallarin 500g", CodigoBarras = "7790070012057", Marca = "Molinos", Categoria = "Almacén", PrecioReferencia = 950 }
            );
            await db.SaveChangesAsync();
        }
    }
}
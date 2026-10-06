
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniMercado.Data;
using MiniMercado.Models;
using MiniMercado.Services;
using System.Text.Json.Serialization;

namespace MiniMercado.Controllers
{
    [Authorize(Roles = "Admin,Caja")]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosApiController : ControllerBase
    {
        private readonly MiniMercadoContext _context;
        private readonly ITenantService _tenantService;

        public ProductosApiController(MiniMercadoContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        [HttpGet("buscar")]
        public async Task<IActionResult> Buscar(string term, bool soloLocales = false)
        {
            if (string.IsNullOrWhiteSpace(term)) return Ok(new List<object>());

            if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();
            term = term.Trim();
            var terms = term.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // 1. Buscar productos locales del tenant
            var queryLocales = _context.Productos
                .Where(p => p.TenantId == tenantId && p.Activo);

            foreach (var t in terms)
            {
                queryLocales = queryLocales.Where(p => p.Nombre.Contains(t) || p.CodigoBarra.Contains(t));
            }

            var productosLocalesRaw = await queryLocales
                .ToListAsync();

            var productosLocales = productosLocalesRaw
                .Select(p => new
                {
                    id = p.Id,
                    nombre = p.Nombre,
                    stock = p.Stock,
                    precio = p.Precio,
                    codigo = p.CodigoBarra,
                    esPorPeso = p.EsPorPeso,
                    tipo = "local"
                })
                .OrderByDescending(p => p.codigo == term)
                .ThenByDescending(p => p.nombre.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                .ThenBy(p => p.nombre)
                .Take(10)
                .ToList();

            // 2. Si hay menos de 5 resultados locales y NO se pidió solo locales, buscamos en el catálogo global
            if (!soloLocales && productosLocales.Count < 5)
            {
                // ... (resto de la lógica igual)
                var codigosLocales = productosLocales.Select(p => p.codigo).ToList();
                var queryBase = _context.ProductosBase.AsQueryable();

                foreach (var t in terms)
                {
                    queryBase = queryBase.Where(pb => pb.Nombre.Contains(t) || pb.CodigoBarras.Contains(t));
                }

                var productosBaseRaw = await queryBase
                    .Where(pb => !codigosLocales.Contains(pb.CodigoBarras))
                    .ToListAsync();

                var productosBase = productosBaseRaw
                    .Select(pb => new
                    {
                        id = pb.Id,
                        nombre = pb.Nombre,
                        stock = (decimal)0,
                        precio = pb.PrecioReferencia ?? 0,
                        codigo = pb.CodigoBarras,
                        esPorPeso = false,
                        tipo = "base"
                    })
                    .OrderByDescending(p => p.codigo == term)
                    .ThenByDescending(p => p.nombre.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                    .ThenBy(p => p.nombre)
                    .Take(10 - productosLocales.Count)
                    .ToList();

                productosLocales.AddRange(productosBase);
            }

            return Ok(productosLocales.Take(10));
        }

        [HttpPost("crear-desde-base")]
        public async Task<IActionResult> CrearDesdeBase([FromBody] CrearDesdeBaseRequest request)
        {
            try 
            {
                if (request == null || request.BaseId <= 0) return BadRequest("ID de producto base inválido");

                if (!_tenantService.TryGetTenantId(out var tenantId)) return Unauthorized();

                // 1. Buscar el ProductoBase
                var pb = await _context.ProductosBase.FindAsync(request.BaseId);
                
                if (pb == null) return NotFound("Producto base no encontrado");
                if (string.IsNullOrEmpty(pb.CodigoBarras)) return BadRequest("El producto base no tiene código de barras");

                // 2. Anti-duplicados: verificar si ya existe en el tenant (por código)
                var existente = await _context.Productos
                    .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.CodigoBarra == pb.CodigoBarras);

                if (existente != null) 
                {
                    return Ok(new 
                    {
                        id = existente.Id,
                        nombre = existente.Nombre,
                        stock = existente.Stock,
                        precio = existente.Precio,
                        codigo = existente.CodigoBarra,
                        esPorPeso = existente.EsPorPeso
                    });
                }

                // 2.1 Buscar o Crear Rubro "Sin Clasificar"
                var rubro = await _context.Rubros
                    .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Nombre == "Sin Clasificar");

                if (rubro == null)
                {
                    rubro = new Rubro
                    {
                        Nombre = "Sin Clasificar",
                        TenantId = tenantId,
                        Activo = true
                    };

                    _context.Rubros.Add(rubro);
                    await _context.SaveChangesAsync();
                }

                // 3. Crear el nuevo producto
                var nuevo = new Producto
                {
                    TenantId = tenantId,
                    Nombre = pb.Nombre ?? "Producto sin nombre",
                    CodigoBarra = pb.CodigoBarras,
                    Precio = pb.PrecioReferencia ?? 0,
                    Stock = 0,
                    Activo = true,
                    EsPorPeso = false,
                    PrecioEditable = true,
                    RubroId = rubro.Id, // ASIGNACIÓN SEGURA
                    ProductoBaseId = pb.Id
                };

                _context.Productos.Add(nuevo);
                await _context.SaveChangesAsync();

                return Ok(new 
                {
                    id = nuevo.Id,
                    nombre = nuevo.Nombre,
                    stock = nuevo.Stock,
                    precio = nuevo.Precio,
                    codigo = nuevo.CodigoBarra,
                    esPorPeso = nuevo.EsPorPeso
                });
            }
            catch (Exception ex)
            {
                // Devolvemos el error completo para debuggear exactamente qué falla
                return StatusCode(500, ex.ToString());
            }
        }
    }

    public class CrearDesdeBaseRequest
    {
        [JsonPropertyName("baseId")]
        public int BaseId { get; set; }
    }
}

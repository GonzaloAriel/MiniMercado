
using Microsoft.AspNetCore.Mvc;
using MiniMercado.Data;
using MiniMercado.Models;
using Microsoft.EntityFrameworkCore;

namespace MiniMercado.Controllers
{
    public class ProductosController : Controller
    {
        private readonly MiniMercadoContext _context;

        public ProductosController(MiniMercadoContext context)
        {
            _context = context;
        }

        // LISTAR
        public IActionResult Index()
        {
            var productos = _context.Productos
       .Where(p => p.Activo)
       .ToList();

            return View(productos);
        }

        // CREATE GET
        public IActionResult Create()
        {
            return View();
        }

        // CREATE POST
        [HttpPost]
        public IActionResult Create(Producto producto)
        {
            if (!ModelState.IsValid)
                return View(producto);

            _context.Productos.Add(producto);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // EDIT GET
        public IActionResult Edit(int id)
        {
            var producto = _context.Productos.Find(id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        // EDIT POST
        [HttpPost]
        public IActionResult Edit(Producto producto)
        {
            if (!ModelState.IsValid)
                return View(producto);

            _context.Productos.Update(producto);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // DELETE GET
        public IActionResult Delete(int id)
        {
            var producto = _context.Productos.FirstOrDefault(p => p.Id == id && p.Activo);
            if (producto == null)
                return NotFound();

            return View(producto);
        }

        // DELETE POST
        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            var producto = _context.Productos.Find(id);
            if (producto == null)
                return NotFound();

            producto.Activo = false;

            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        //INACTIVOS
        public IActionResult Inactivos(string filtro)
        {
            var productos = _context.Productos
                .Where(p => !p.Activo);

            if (!string.IsNullOrEmpty(filtro))
            {
                filtro = filtro.ToLower();
                productos = productos.Where(p =>
                    p.Nombre.ToLower().Contains(filtro) ||
                    p.CodigoBarra.Contains(filtro)
                );
            }

            return View(productos.ToList());
        }

        //REACTIVAR
        public IActionResult Reactivar(int id)
        {
            var producto = _context.Productos.Find(id);
            if (producto == null)
                return NotFound();

            producto.Activo = true;
            _context.SaveChanges();

            return RedirectToAction("Inactivos");
        }

    }
}

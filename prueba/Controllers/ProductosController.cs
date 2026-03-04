using MascotasApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prueba.Models;


namespace prueba.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductos()
        {
            if (_context.Productos == null)
            {
                return NotFound("La tabla Productos no está configurada en el contexto.");
            }

            return await _context.Productos
                .FromSqlRaw("EXEC sp_ObtenerProductos")
                .AsNoTracking()
                .ToListAsync();
        }
        
        [HttpPost("confirmar-compra")]
        [Authorize]
        public async Task<IActionResult> ConfirmarCompra([FromBody] List<ItemCarritoDTO> carrito)
        {
            foreach (var item in carrito)
            {
                var producto = await _context.Productos.FindAsync(item.ProductoId);
                if (producto != null)
                {
                    if (producto.Stock >= item.Cantidad)
                    {
                        producto.Stock -= item.Cantidad;
                    }
                    else
                    {
                        return BadRequest($"Stock insuficiente para {producto.Nombre}");
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Compra procesada y stock actualizado" });
        }
        public class ItemCarritoDTO
        {
            public int ProductoId { get; set; }
            public int Cantidad { get; set; }
        }
        public class ItemUpdateDTO
        {
            public int ProductoId { get; set; }
            public int Cantidad { get; set; }
            public decimal NuevoPrecio { get; set; }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("actualizar-inventario")]
        public async Task<IActionResult> ActualizarInventario([FromBody] List<ItemUpdateDTO> tablaNueva)
        {
            if (tablaNueva == null || tablaNueva.Count == 0)
                return BadRequest("No se enviaron datos para actualizar.");

            foreach (var item in tablaNueva)
            {
                var producto = await _context.Productos.FindAsync(item.ProductoId);
                if (producto != null)
                {
                    producto.Stock += item.Cantidad;
                    producto.Precio = item.NuevoPrecio;
                }
                else
                {
                    return BadRequest($"El producto con ID {item.ProductoId} no existe.");
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Inventario y precios actualizados correctamente" });
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Producto>> CrearProducto([FromBody] Producto nuevoProducto)
        {
            try
            {
                _context.Productos.Add(nuevoProducto);
                await _context.SaveChangesAsync();
                return Ok(nuevoProducto);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error al crear el producto", details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EliminarProducto(int id)
        {
            try
            {
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null)
                {
                    return NotFound(new { message = "Producto no encontrado" });
                }

                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Producto eliminado con éxito" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "No se puede eliminar porque está asociado a una compra u otro registro.", details = ex.Message });
            }
        }
    }
}

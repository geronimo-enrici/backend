using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
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
        [HttpPost("sincronizar-excel")]
        public async Task<IActionResult> SincronizarExcel(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest("Por favor, subí un archivo válido.");

            try
            {
                var productosExcel = new List<ProductoDTO>();

                // 1. Leemos el Excel
                using (var stream = new MemoryStream())
                {
                    await archivo.CopyToAsync(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                        foreach (var row in rows)
                        {
                            productosExcel.Add(new ProductoDTO
                            {
                                Nombre = row.Cell(1).GetValue<string>().Trim(),
                                Categoria = row.Cell(2).GetValue<string>().Trim(),
                                Precio = row.Cell(3).GetValue<decimal>(),
                                Icono = row.Cell(4).GetValue<string>() ?? "📦",
                                Stock = row.Cell(5).GetValue<int>()
                            });
                        }
                    }
                }

                var productosBD = await _context.Productos.ToListAsync();

                foreach (var prodExcel in productosExcel)
                {
                    var productoExistente = productosBD.FirstOrDefault(p => p.Nombre.ToLower() == prodExcel.Nombre.ToLower());

                    if (productoExistente != null)
                    {
                        productoExistente.Precio = prodExcel.Precio;
                        productoExistente.Stock = prodExcel.Stock;
                        productoExistente.Categoria = prodExcel.Categoria;
                        productoExistente.Icono = prodExcel.Icono;
                    }
                    else
                    {
                        var nuevoProducto = new Producto
                        {
                            Nombre = prodExcel.Nombre,
                            Categoria = prodExcel.Categoria,
                            Precio = prodExcel.Precio,
                            Icono = prodExcel.Icono,
                            Stock = prodExcel.Stock
                        };
                        _context.Productos.Add(nuevoProducto);
                    }
                }

                var nombresExcel = productosExcel.Select(e => e.Nombre.ToLower()).ToList();

                var productosABorrar = productosBD
                    .Where(p => !nombresExcel.Contains(p.Nombre.ToLower()))
                    .ToList();

                if (productosABorrar.Any())
                {
                    _context.Productos.RemoveRange(productosABorrar);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "Sincronización exitosa",
                    procesados = productosExcel.Count,
                    eliminados = productosABorrar.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al procesar el Excel: {ex.Message}");
            }
        }
        public class ProductoDTO

        {

            public string Nombre { get; set; }

            public string Categoria { get; set; }

            public decimal Precio { get; set; }

            public int Stock { get; set; }

            public string Icono { get; set; }

        }

        [HttpGet("descargar-excel")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DescargarExcel()
        {
            try
            {
                var productos = await _context.Productos.ToListAsync();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Productos");

                    worksheet.Cell(1, 1).Value = "Nombre";
                    worksheet.Cell(1, 2).Value = "Categoria";
                    worksheet.Cell(1, 3).Value = "Precio";
                    worksheet.Cell(1, 4).Value = "Icono";
                    worksheet.Cell(1, 5).Value = "Stock";

                    worksheet.Range("A1:E1").Style.Font.Bold = true;

                    int fila = 2;
                    foreach (var prod in productos)
                    {
                        worksheet.Cell(fila, 1).Value = prod.Nombre;
                        worksheet.Cell(fila, 2).Value = prod.Categoria;
                        worksheet.Cell(fila, 3).Value = prod.Precio;
                        worksheet.Cell(fila, 4).Value = prod.Icono;
                        worksheet.Cell(fila, 5).Value = prod.Stock;
                        
                        fila++;
                    }

                    worksheet.Columns().AdjustToContents();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();

                        return File(
                            content,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            "Inventario_Productos.xlsx"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al generar el Excel: {ex.Message}");
            }
        }
    }
}

using MascotasApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prueba.Controllers;
using prueba.Models;

namespace prueba.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DuenosController : ControllerBase
    {
        private readonly AppDbContext _context;
        public DuenosController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Dueno>>> GetDuenos()
        {
            return await _context.Dueno
                .FromSqlRaw("EXEC sp_ObtenerDueno")
                .AsNoTracking()
                .ToListAsync();
        }
        [HttpPost]
        public async Task<ActionResult<Dueno>> PostDueno(Dueno dueno)
        {
            var resultado = await _context.Dueno
                .FromSqlRaw("EXEC sp_InsertarDueno @Nombre={0}, @Apellido={1}, @Telefono={2}",
                dueno.Nombre, dueno.Apellido, dueno.Telefono)
                .ToListAsync();

            var duenoCreado = resultado.FirstOrDefault();

            if (duenoCreado == null)
            {
                return BadRequest("No se pudo crear el registro del dueño.");
            }
            return CreatedAtAction(nameof(GetDuenos), new { id = duenoCreado.Id }, duenoCreado);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<Dueno>> GetDuenoById(int id)
        {
            var dueno = await _context.Dueno
                .Include(d => d.Mascotas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (dueno == null) return NotFound();

            Console.WriteLine($"---> DEBUG: Dueño {dueno.Nombre} encontrado.");
            Console.WriteLine($"---> DEBUG: Cantidad de mascotas en DB: {dueno.Mascotas?.Count ?? 0}");

            return dueno;
        }
    }
}

using MascotasApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prueba.Models;

namespace prueba.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TurnosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TurnosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetTurnos()
        {
            try
            {
                List<Turno> turnos = await _context.Turnos.ToListAsync();
                return Ok(turnos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetTurnos: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");

                return StatusCode(500, new { mensaje = "Error en el servidor", detalle = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostTurno([FromBody] Turno turno)
        {
            if (turno == null) return BadRequest();

            _context.Turnos.Add(turno);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTurnos), new { id = turno.Id }, turno);
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTurno(int id)
        {
            var turno = await _context.Turnos.FindAsync(id);
            if (turno == null) return NotFound();

            _context.Turnos.Remove(turno);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
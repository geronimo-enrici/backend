using MascotasApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prueba.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TuProyecto.Models;

namespace TuProyecto.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HistorialClinicoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HistorialClinicoController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("mascota/{mascotaId}")]
        public async Task<ActionResult<IEnumerable<HistorialClinico>>> GetHistorial(int mascotaId)
        {
            return await _context.HistorialClinico
                .Where(h => h.MascotaId == mascotaId)
                .OrderByDescending(h => h.Fecha)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<HistorialClinico>> PostHistorial(HistorialClinico historial)
        {
            _context.HistorialClinico.Add(historial);

            var mascota = await _context.Mascotas.FindAsync(historial.MascotaId);
            if (mascota != null)
            {
                mascota.Peso = historial.Peso;
            }

            await _context.SaveChangesAsync();
            return Ok(historial);
        }
    }
}
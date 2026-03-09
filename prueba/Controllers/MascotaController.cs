using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prueba.Models;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using System.Data;


namespace MascotasApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MascotasController : ControllerBase
{
    private readonly AppDbContext _context;

    public MascotasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Mascota>>> GetMascotas()
    {
        var mascotas = await _context.Mascotas
            .FromSqlRaw("EXEC dbo.sp_ObtenerMascotas")
            .ToListAsync();

        foreach (var mascota in mascotas)
        {
            await _context.Entry(mascota).Reference(m => m.Dueno).LoadAsync();
        }

        return Ok(mascotas);
    }

    [HttpPost]
    public async Task<ActionResult<Mascota>> PostMascota(Mascota mascota)
    {
        var resultado = await _context.Mascotas
            .FromSqlRaw("EXEC sp_InsertarMascota @nombre={0}, @Raza={1}, @Edad={2}, @Peso={3}, @DuenoId={4}",
                mascota.nombre, mascota.Raza, mascota.Edad, mascota.Peso, mascota.DuenoId)
            .AsNoTracking()
            .Include(m => m.Dueno)
            .ToListAsync();

        var mascotaCreado = resultado.FirstOrDefault();

        if (mascotaCreado == null)
        {
            return BadRequest("No se pudo crear el registro de la mascota.");
        }
        return CreatedAtAction(nameof(GetMascotas), new { id = mascotaCreado.Id }, mascotaCreado);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetMascota(int id)
    {
        try
        {
            var mascota = await _context.Mascotas
                .Include(m => m.Dueno)
                .Include(m => m.Vacunas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mascota == null) return NotFound();

            var todasLasVacunas = await _context.Vacunas.ToListAsync();

            var planVacunacion = todasLasVacunas.Select(v => {
                var registro = mascota.Vacunas?.FirstOrDefault(mv => mv.VacunaId == v.Id);
                return new
                {
                    VacunaId = v.Id,
                    NombreVacuna = v.Nombre,
                    Aplicada = registro != null ? registro.Aplicada : false,
                    Fecha = registro?.Fecha
                };
            }).ToList();

            return Ok(new
            {
                mascota = new
                {
                    id = mascota.Id,
                    nombre = mascota.nombre,
                    raza = mascota.Raza,
                    edad = mascota.Edad,
                    peso = mascota.Peso,
                    dueno = mascota.Dueno != null ? new { id = mascota.Dueno.Id, nombre = mascota.Dueno.Nombre } : null
                },
                planVacunacion = planVacunacion
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
        }
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, [FromBody] MascotaDto mascota)
    {
        if (mascota == null) return BadRequest("No se recibieron datos.");
        if (id != mascota.Id) return BadRequest("El ID de la URL no coincide con la mascota.");

        try
        {
            int duenoIdFinal = mascota.Dueno?.Id ?? 0;

            var parameters = new[] {
            new SqlParameter("@Id", mascota.Id),
            new SqlParameter("@Nombre", mascota.Nombre),
            new SqlParameter("@Raza", (object)mascota.Raza ?? DBNull.Value),
            new SqlParameter("@Edad", mascota.Edad),
            new SqlParameter("@Peso", mascota.Peso),
            new SqlParameter("@DuenoId", duenoIdFinal)
        };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_ActualizarMascota @Id, @Nombre, @Raza, @Edad, @Peso, @DuenoId",
                parameters
            );

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error en SQL: {ex.Message}");
        }
    }

    [HttpPut("aplicar-vacuna/{id}")]
    public async Task<IActionResult> AplicarVacuna(int id)
    {
        var registro = await _context.MascotaVacunas.FindAsync(id);

        if (registro == null) return NotFound(new { mensaje = "El registro de vacuna no existe." });

        registro.Aplicada = true;
        registro.Fecha = DateTime.Now;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return BadRequest(new { mensaje = "Error al actualizar la base de datos." });
        }

        return NoContent();
    }

    [HttpGet("generar-descripcion")]
    public async Task<IActionResult> GetDescripcionIA(string nombre, string raza)
    {
        string apiKey = "AIzaSyAKjwGtR8U604cqnLYK5wv2M6MzHeZhzWA";
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

        var prompt = $"Escribe una descripción corta,(máximo 50 palabras) para un perro llamado {nombre} de raza {raza}, apuntalo a su raza y forma de ser";

        var payload = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } }
        };

        using (var client = new HttpClient())
        {
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            var jsonResponse = JsonSerializer.Deserialize<object>(responseString);
            return Ok(jsonResponse);
        }
    }

    [HttpPut("{id}/vacunas/{idVacuna}")]
    public async Task<IActionResult> ActualizarVacuna(int id, int idVacuna, [FromBody] ActualizarVacunaDto dto)
    {
        var mascotaVacuna = await _context.MascotaVacunas
            .FirstOrDefaultAsync(mv => mv.MascotaId == id && mv.VacunaId == idVacuna);

        if (mascotaVacuna == null)
        {
            mascotaVacuna = new MascotaVacuna
            {
                MascotaId = id,
                VacunaId = idVacuna,
                Aplicada = dto.Aplicada,
                Fecha = dto.Fecha
            };
            _context.MascotaVacunas.Add(mascotaVacuna);
        }
        else
        {
            mascotaVacuna.Aplicada = dto.Aplicada;
            mascotaVacuna.Fecha = dto.Fecha;
        }

        await _context.SaveChangesAsync();
        return Ok(mascotaVacuna);
    }

    public class ActualizarVacunaDto
    {
        public bool Aplicada { get; set; }
        public DateTime? Fecha { get; set; }
    }
    [HttpGet("{id}/historial")]
    public async Task<IActionResult> GetHistorial(int id)
    {
        var historial = await _context.HistorialesClinicos
            .Where(h => h.MascotaId == id)
            .OrderByDescending(h => h.Fecha)
            .ToListAsync();

        return Ok(historial);
    }

    [HttpPost("{id}/historial")]
    public async Task<IActionResult> AgregarHistorial(int id, [FromBody] HistorialClinico nuevoRegistro)
    {
        if (nuevoRegistro == null) return BadRequest("Datos inválidos.");

        nuevoRegistro.MascotaId = id;

        _context.HistorialesClinicos.Add(nuevoRegistro);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetHistorial), new { id = nuevoRegistro.Id }, nuevoRegistro);
    }
}
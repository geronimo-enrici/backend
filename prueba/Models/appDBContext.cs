using Microsoft.EntityFrameworkCore;
using prueba.Models;
using TuProyecto.Models;

namespace MascotasApi;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Mascota> Mascotas { get; set; }
    public DbSet<Dueno> Dueno { get; set; }
    public DbSet<Vacuna> Vacunas { get; set; }
    public DbSet<MascotaVacuna> MascotaVacunas { get; set; } = null!;
    public DbSet<Producto> Productos { get; set; }
    public DbSet<Usuarios> Usuarios { get; set; }
    public DbSet<Turno> Turnos { get; set; }
    public DbSet<HistorialClinico> HistorialClinico { get; set; }
}
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace prueba.Models
{
    [Table("MascotaVacuna")]
    public class MascotaVacuna
    {
        public int Id { get; set; }
        public int MascotaId { get; set; }
        public int VacunaId { get; set; }
        public bool Aplicada { get; set; }
        [JsonIgnore]
        public DateTime? Fecha { get; set; }
        public Vacuna Vacuna { get; set; } = null!;
    }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace prueba.Models
{
    [Table("Vacuna")]
    public class Vacuna
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
    }
}

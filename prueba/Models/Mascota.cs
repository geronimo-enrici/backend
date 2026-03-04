using prueba.Models;
using System.Text.Json.Serialization;

namespace MascotasApi
{
    public class Mascota
    {
        public int Id {  get; set; }
        public string nombre { get; set; } = string.Empty;
        public string Raza { get; set; } = string.Empty;
        public int Edad { get; set; }
        public decimal Peso { get; set; }
        public int DuenoId { get; set; }
        public Dueno? Dueno { get; set; }


        public List<MascotaVacuna> Vacunas { get; set; } = new();
    }

}

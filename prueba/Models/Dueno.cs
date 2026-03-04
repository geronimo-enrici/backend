using MascotasApi;
using System.Text.Json.Serialization;

namespace prueba.Models
{
    public class Dueno
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido {  get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        [JsonPropertyName("mascotas")]
      
        public List<Mascota> Mascotas { get; set; } = new();
    }
}

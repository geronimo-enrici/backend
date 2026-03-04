using System.Text.Json.Serialization;

namespace prueba.Models
{
    public class MascotaDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nombre")]
        public string? Nombre { get; set; }

        [JsonPropertyName("raza")]
        public string? Raza { get; set; }

        [JsonPropertyName("edad")]
        public int Edad { get; set; }

        [JsonPropertyName("peso")]
        public decimal Peso { get; set; } 

        [JsonPropertyName("dueno")]
        public DuenoDto? Dueno { get; set; }
    }

    public class DuenoDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nombre")]
        public string? Nombre { get; set; }
    }
}
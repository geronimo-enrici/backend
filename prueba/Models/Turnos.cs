namespace prueba.Models
{
    public class Turno
    {
        public int Id { get; set; }
        public string MascotaNombre { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public string Tipo { get; set; } = string.Empty; 
    }
}
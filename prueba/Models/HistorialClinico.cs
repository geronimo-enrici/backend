using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TuProyecto.Models
{
    public class HistorialClinico
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MascotaId { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        [StringLength(200)]
        public string Motivo { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal Peso { get; set; }

        [Required]
        public string Diagnostico { get; set; }

        public string Tratamiento { get; set; }

        public string Veterinario { get; set; } = "Personal Médico";
    }
}
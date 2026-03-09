using MascotasApi;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace prueba.Models
{
    public class HistorialClinico
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MascotaId { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        [MaxLength(100)]
        public string Veterinario { get; set; }

        [Required]
        [MaxLength(150)]
        public string MotivoConsulta { get; set; }

        public string Diagnostico { get; set; }
        public string Tratamiento { get; set; }
        public string Observaciones { get; set; }

        [ForeignKey("MascotaId")]
        public Mascota? Mascota { get; set; }
    }
}
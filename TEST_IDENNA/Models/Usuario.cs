using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TEST_IDENNA.Models
{
    public class Usuario
    {
        [Key]
        public int Id_Usuario { get; set; }

        [Required]
        [MaxLength(50)]
        public string Nombre_Usuario { get; set; }

        [Required]
        public string Password_Hash { get; set; }

        [Required]
        public string Rol { get; set; } // Ejemplo: "Psicólogo", "Educador"

        public string Especialidad { get; set; }

        // Propiedades de Navegación
        // Un usuario (profesional) puede registrar muchas actividades
        public virtual ICollection<Actividad> ActividadesRealizadas { get; set; }

        // Un usuario puede registrar muchas evoluciones en la línea de tiempo
        public virtual ICollection<Evolucion> EvolucionesRegistradas { get; set; }
    }
}

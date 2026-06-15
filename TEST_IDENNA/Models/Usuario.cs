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
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string NombreUsuario { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Rol { get; set; } // Ej: "Administrador", "TrabajadorSocial", "Psicologo"

        public string NombreCompleto { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        // Propiedades de Navegación
        // Un usuario (profesional) puede registrar muchas actividades
        public virtual ICollection<Actividad> ActividadesRealizadas { get; set; }

        // Un usuario puede registrar muchas evoluciones en la línea de tiempo
        public virtual ICollection<Evolucion> EvolucionesRegistradas { get; set; }
    }
}

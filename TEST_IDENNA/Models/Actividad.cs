using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TEST_IDENNA.Models
{
    public class Actividad
    {
        [Key]
        public int Id_Actividad { get; set; }

        /*[Required]
        public int Id_Usuario_Responsable { get; set; }*/

        [Required]
        public DateTime Fecha_Registro { get; set; }

        [Required]
        public string Tipo_Actividad { get; set; } // Ejemplo: "Taller de Pintura"

        [Required]
        public string Area { get; set; } // Ejemplo: "Desarrollo Cognitivo"

        //public string Objetivos_Alcanzados { get; set; }

        //public string Observaciones_Generales { get; set; }

        // Propiedades de Navegación
        /*[ForeignKey("Id_Usuario_Responsable")]
        public virtual Usuario UsuarioResponsable { get; set; }*/

        // Una actividad tiene muchos beneficiarios asociados mediante la tabla de asistencia
        public virtual ICollection<AsistenciaActividad> Asistentes { get; set; } = new List<AsistenciaActividad>();
    }
}

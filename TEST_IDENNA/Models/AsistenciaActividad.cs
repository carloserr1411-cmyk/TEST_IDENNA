using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TEST_IDENNA.Models
{
    public class AsistenciaActividad
    {
        [Key]
        public int Id_Asistencia { get; set; }

        [Required]
        public int Id_Actividad { get; set; }

        [Required]
        public int Id_Beneficiario { get; set; }

        // Aquí registras cómo le fue a ese niño específico en esa actividad
        [Required]
        public required string Desempeño_Individual { get; set; }

        [Required]
        public required string Area { get; set; } // Psicología, Legal, etc. (opcional, pero útil para la línea de tiempo)

        // Propiedades de Navegación
        [ForeignKey("Id_Actividad")]
        [Required]
        public required virtual Actividad ActividadAsociada { get; set; }

        [ForeignKey("Id_Beneficiario")]
        [Required]
        public required virtual Beneficiario BeneficiarioAsistente { get; set; }

        // PROPIEDAD DE NAVEGACIÓN: Esto permite que EF Core una las tablas
        [Required]
        public required virtual Actividad Actividad { get; set; }

        [Required]
        public required virtual Tutores Tutor { get; set; }
    }
}

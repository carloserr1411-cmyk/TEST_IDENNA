using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace TEST_IDENNA.Models
{
    public class Evolucion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id_Evolucion { get; set; }

        // Llave foránea
        public int Id_Actividad { get; set; }

        // Propiedad de Navegación (ESTO es lo que usa el Include)
        [ForeignKey("Id_Actividad")]
        public virtual Actividad Actividad { get; set; }

        public int Id_Beneficiario { get; set; }

        [ForeignKey("Id_Beneficiario")]
        public virtual Beneficiario Beneficiario { get; set; }

        public string Detalle { get; set; }
        public DateTime Fecha_Registro { get; set; }
        public string Especialista { get; set; }
    }
}

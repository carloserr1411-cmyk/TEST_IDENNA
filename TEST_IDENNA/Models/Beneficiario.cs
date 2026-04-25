using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TEST_IDENNA.Models
{
    public class Beneficiario
    {
        [Key]
        public int Id_Beneficiario { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public DateTime Fecha_Nacimiento { get; set; }
        public string Cedula { get; set; }
        public int Edad {
            get
            {
                var hoy = DateTime.Today;
                var edad = hoy.Year - Fecha_Nacimiento.Year;
                // Ajuste por si aún no ha cumplido años en el año actual
                if (Fecha_Nacimiento.Date > hoy.AddYears(-edad)) edad--;

                return edad;
            }
        }
        public string Estatus_Legal { get; set; }
        public DateTime Fecha_Ingreso { get; set; }
        public string Observaciones { get; set; }

        // Relación: Un beneficiario tiene muchas evoluciones
        public ICollection<Evolucion>? Evoluciones { get; set; }
    }

    public class Evolucion
    {
        [Key]
        public int Id_Evolucion { get; set; }
        public int Id_Beneficiario { get; set; }
        public string Area { get; set; }
        public string Descripcion { get; set; }
        public DateTime Fecha_Registro { get; set; }

        [ForeignKey("Id_Beneficiario")]
        public Beneficiario Beneficiario { get; set; }
    }
}

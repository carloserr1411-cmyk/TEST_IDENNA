using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TEST_IDENNA.Models
{
    public class Nino
    {
        public int Id { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public string SituacionActual { get; set; } // Ejemplo: "Estable", "En Seguimiento"
        public string UbicacionArchivoFisico { get; set; } // Ej: "Caja 12, Carpeta D"
    }
}

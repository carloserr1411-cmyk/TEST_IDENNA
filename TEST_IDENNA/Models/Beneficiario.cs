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
        public byte[]? Foto { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }

        public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
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
        public string Estatus_Color { get; set; } // 👈 Aquí guardaremos el Hexadecimal (ej: "#E74C3C")
        public DateTime Fecha_Ingreso { get; set; }
        public string Observaciones { get; set; }

        public Beneficiario Clonar()
        {
            // MemberwiseClone crea una copia de los valores (Cédula, Nombres, Foto, etc.)
            return (Beneficiario)this.MemberwiseClone();
        }

        public string Estatus { get; set; } = "Activo"; // Por defecto todos entran activos

        public DateTime? FechaEliminacion { get; set; } // Fecha en que se eliminó el beneficiario (si es que se eliminó)}

        public string? NombreContacto { get; set; }
        public string? ParentescoContacto { get; set; }
        public string? TelefonoContacto { get; set; }

        public DateTime? FechaModificacion { get; set; } // Para rastrear cuándo se modificó por última vez el registro

        public string? UbicacionFisica { get; set; } // Campo para guardar la ubicación física del expediente (estante, caja, etc.)
    }
}

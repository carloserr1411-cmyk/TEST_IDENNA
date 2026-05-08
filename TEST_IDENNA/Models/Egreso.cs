using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TEST_IDENNA.Models
{
    public class Egreso
    {
        [Key]
        public int Id_Egreso { get; set; }

        // Clave Foránea
        public int Id_Beneficiario { get; set; }

        [Required]
        public DateTime Fecha_Salida { get; set; }

        [Required]
        public string Motivo_Egreso { get; set; }

        public string? Observaciones_Salida { get; set; }

        // Propiedad de Navegación: permite acceder a los datos del niño desde el egreso
        [ForeignKey("Id_Beneficiario")]
        public virtual Beneficiario Beneficiario { get; set; }
    }
}
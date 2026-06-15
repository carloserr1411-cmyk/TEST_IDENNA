using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TEST_IDENNA.Models
{
    public class Egreso
    {
        [Key]
        public int Id_Egreso { get; set; }

        // Clave Foránea
        // Propiedad de Navegación: permite acceder a los datos del niño desde el egreso
        [ForeignKey("Id_Beneficiario")]
        public int Id_Beneficiario { get; set; }

        public string CodigoExpediente { get; set; } = string.Empty;

        [Required]
        public DateTime Fecha_Salida { get; set; }

        [Required]
        public string Motivo_Egreso { get; set; } = string.Empty;

        public string? Observaciones_Salida { get; set; }

        [Required]
        public virtual Beneficiario Beneficiario { get; set; } = null!;
    }
}
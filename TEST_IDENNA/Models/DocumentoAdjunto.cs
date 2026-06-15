using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace TEST_IDENNA.Models
{
    public class DocumentoAdjunto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id_Documento { get; set; }

        // Relación con el Beneficiario
        public int Id_Beneficiario { get; set; }

        [ForeignKey("Id_Beneficiario")]
        public virtual Beneficiario Beneficiario { get; set; } = null!;

        // Datos informativos del documento
        public string NombreArchivo { get; set; } = string.Empty; // Ej: "Informe_Medico.jpg"
        public string TextoOcr { get; set; } = string.Empty;       // Aquí guardaremos todo el texto que lea Tesseract
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        // El archivo físico guardado en bytes (ideal para imágenes comprimidas)
        public byte[] ArchivoBytes { get; set; } = Array.Empty<byte>();

        public string RutaImagen { get; set; } = string.Empty;
        public string RutaWord { get; set; } = string.Empty;

        // Datos del archivo
        public string NombreDocumento { get; set; } = string.Empty; // Ej: "Informe_Psicologico_2026.pdf"
        public string TipoDocumento { get; set; } = string.Empty; // Ej: "Médico", "Legal", "Escolar"

        // Almacenamiento: Se recomienda guardar la ruta local o los bytes si son imágenes ligeras
        public byte[] ArchiveData { get; set; } = Array.Empty<byte>();

        // El texto que el OCR logró extraer (sirve para buscar palabras clave en el futuro)
        public string TextoExtraidoOcr { get; set; } = string.Empty;

        public DateTime Fecha_Registro { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;
        public DateTime? FechaEliminacion { get; set; }
    }
}

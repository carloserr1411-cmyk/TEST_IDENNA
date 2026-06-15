using System;
using System.Collections.Generic;
using System.Text;

namespace TEST_IDENNA.Models
{
    public class AuditoriaMovimiento
    {
        public int Id { get; set; }

        public string Computadora { get; set; } = string.Empty;

        // Quién lo hizo (Temporalmente será texto, luego se puede asociar al ID del Usuario)
        public string Usuario { get; set; } = string.Empty;

        // Qué hizo: "CREAR", "MODIFICAR", "ELIMINAR", "CONSULTAR"
        public string Accion { get; set; } = string.Empty;

        // En qué pantalla/módulo ocurrió: "Expedientes", "Casos Cerrados", "Localizador"
        public string Modulo { get; set; } = string.Empty;

        // Descripción de lo que cambió: "Se modificó la ubicación física a: Estante B-4"
        public string Detalles { get; set; } = string.Empty;

        // Cuándo ocurrió
        public DateTime Fecha { get; set; } = DateTime.Now;

        public string Rol { get; set; } = string.Empty;
    }
}

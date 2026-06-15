using System;
using System.Collections.Generic;
using System.Text;

namespace TEST_IDENNA.Models
{
    public class ExpedienteFisico
    {
        public string CodigoExpediente { get; set; } = string.Empty;
        public Beneficiario Beneficiario { get; set; } = new();
        public string UbicacionFisica { get; set; } = string.Empty;
        public DateTime FechaActualizacionUbicacion { get; set; }
    }
}

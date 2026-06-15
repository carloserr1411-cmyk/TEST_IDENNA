using System;
using System.Collections.Generic;
using System.Text;

namespace TEST_IDENNA.Messages
{
    // Mensaje para cuando el beneficiario YA existe
    public class EnrutarExpedienteExistenteMessage
    {
        public string Cedula { get; }
        public EnrutarExpedienteExistenteMessage(string cedula) => Cedula = cedula;
    }

    // Mensaje para cuando el beneficiario es NUEVO
    public class EnrutarNuevoRegistroMessage
    {
        public string Cedula { get; }
        public EnrutarNuevoRegistroMessage(string cedula) => Cedula = cedula;
    }
}

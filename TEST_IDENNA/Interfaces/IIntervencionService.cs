using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TEST_IDENNA.Models;
using TEST_IDENNA.Services;

namespace TEST_IDENNA.Interfaces
{
    public interface IIntervencionService
    {
        Task<bool> RegistrarNuevoIngreso(Beneficiario nuevo);
        Task<IEnumerable<Evolucion>> ObtenerIntervencionesPorBeneficiario(int idBeneficiario);
        Task<IEnumerable<Actividad>> ObtenerTodasLasActividades();
        Task<IEnumerable<Evolucion>> ObtenerHistorialGlobal();
        Task CrearNuevaActividad(string nombreNuevaActividad, string areaSeleccionada);

        // Registro de una nueva intervención
        Task RegistrarIntervencion(Evolucion nuevaEvolucion);
    }
}

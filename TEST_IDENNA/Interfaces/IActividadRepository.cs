using TEST_IDENNA.Models;

namespace TEST_IDENNA.Interfaces
{
    public interface IActividadRepository
    {
        Task<int> GuardarActividad(Actividad actividad);
        Task<IEnumerable<Evolucion>> ObtenerEvolucionesPorBeneficiario(int beneficiarioId);
        Task VincularNiñoAActividad(int nuevaActividadId, int niñoId, string notaInicial);
    }
}
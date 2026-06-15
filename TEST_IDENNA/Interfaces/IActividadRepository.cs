using TEST_IDENNA.Models;

namespace TEST_IDENNA.Interfaces
{
    public interface IActividadRepository
    {
        Task<int> GuardarActividad(Actividad actividad);
        Task<IEnumerable<Evolucion>> ObtenerEvolucionesPorBeneficiario(int beneficiarioId);
        Task VincularNiñoAActividad(int nuevaActividadId, int niñoId, string notaInicial);
/*
        // Obtiene las que están en la papelera (IsDeleted = 1)
        Task<IEnumerable<Evolucion>> GetPapeleraEvolucionesPorBeneficiarioAsync(int beneficiarioId);

        // 1. Mover a la papelera (Soft Delete)
        Task MoverAPapeleraAsync(int id);

        // 2. Restaurar de la papelera
        Task RestaurarAsync(int id);

        // 3. Eliminar para siempre (Hard Delete)
        Task EliminarPermanenteAsync(int id);*/
    }
}
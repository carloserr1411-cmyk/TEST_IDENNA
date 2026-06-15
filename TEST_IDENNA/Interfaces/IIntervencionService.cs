using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TEST_IDENNA.Models;

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
        Task<IEnumerable<Tutores>> ObtenerTodosLosTutores();
        Task UpdateEvolucionAsync(Evolucion evolucionEnEdicion);

        Task GuardarDocumentoAdjuntoAsync(DocumentoAdjunto documento);
        Task<List<DocumentoAdjunto>> ObtenerDocumentosPorBeneficiarioAsync(int beneficiarioId);

        // =========================================================================
        // 🔥 NUEVOS MÉTODOS: MÉTODOS DE LA PAPELERA (ALINEADOS CON EL SERVICIO)
        // =========================================================================

        #region PAPELERA DE EVOLUCIONES

        // Obtener las evoluciones que están en la papelera (IsDeleted = true)
        Task<IEnumerable<Evolucion>> ObtenerEvolucionesPapeleraAsync();

        // Mover una evolución a la papelera (Soft Delete)
        Task<bool> MoverEvolucionAPapeleraAsync(int id);

        // Restaurar una evolución de la papelera
        Task<bool> RestaurarEvolucionAsync(int id);

        // Eliminar definitivamente una evolución de la base de datos (Hard Delete)
        Task<bool> EliminarEvolucionPermanenteAsync(int id);

        #endregion

        #region PAPELERA DE DOCUMENTOS ADJUNTOS (ARCHIVOS DIGITALES)

        // Obtener los documentos adjuntos que están en la papelera (IsDeleted = true)
        Task<IEnumerable<DocumentoAdjunto>> ObtenerDocumentosPapeleraAsync();

        // Mover un documento a la papelera (Soft Delete)
        Task<bool> MoverDocumentoAPapeleraAsync(int id);

        // Restaurar un documento de la papelera
        Task<bool> RestaurarDocumentoAsync(int id);

        // Eliminar definitivamente un documento de la base de datos (Hard Delete)
        Task<bool> EliminarDocumentoPermanenteAsync(int id);

        #endregion

        #region PAPELERA DE BENEFICIARIOS

        Task<IEnumerable<Beneficiario>> ObtenerBeneficiariosPapeleraAsync();
        Task<bool> MoverBeneficiarioAPapeleraAsync(int id);
        Task<bool> RestaurarBeneficiarioAsync(int id);
        Task<bool> EliminarBeneficiarioPermanenteAsync(int id);

        #endregion
    }
}
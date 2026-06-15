using System;
using System.Collections.Generic;
using System.Text;
using TEST_IDENNA.Models;

namespace TEST_IDENNA.Interfaces
{
    public interface IArchivoService
    {
        Task<IEnumerable<ExpedienteFisico>> ObtenerExpedientesFisicosAsync();
        Task<IEnumerable<Egreso>> ObtenerCasosCerradosAsync();
        Task<IEnumerable<AuditoriaMovimiento>> ObtenerAuditoriasAsync();
    }
}

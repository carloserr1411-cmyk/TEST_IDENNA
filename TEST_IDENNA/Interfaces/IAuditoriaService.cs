using System;
using System.Collections.Generic;
using System.Text;
using TEST_IDENNA.Models;

namespace TEST_IDENNA.Interfaces
{
    public interface IAuditoriaService
    {
        Task RegistrarAccionAsync(string accion, string modulo, string detalles);
        Task<IEnumerable<AuditoriaMovimiento>> ObtenerTodoAsync();
    }
}

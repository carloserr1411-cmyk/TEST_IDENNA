using TEST_IDENNA.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TEST_IDENNA.Interfaces
{
    public interface IEgresoRepository
    {
        // El método que te faltaba
        Task<List<Egreso>> ObtenerTodos();

        Task Registrar(Egreso egreso);
        // Puedes agregar otros métodos que necesite tu compañera
    }
}
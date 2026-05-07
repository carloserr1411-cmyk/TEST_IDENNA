using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TEST_IDENNA.Models;

namespace TEST_IDENNA
{
    public interface IBeneficiarioRepository
    {
        Task Registrar(Beneficiario beneficiario);
        Task<List<Beneficiario>> ObtenerTodos();
        Task<List<Beneficiario>> ObtenerPorNombre(string searchText);
        Task Actualizar(Beneficiario beneficiario);
        //Task<IEnumerable<Evolucion>> ObtenerEvolucionesPorBeneficiario(int id_Beneficiario);
    }
}

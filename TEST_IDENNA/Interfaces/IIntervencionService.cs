using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TEST_IDENNA.Models;

namespace TEST_IDENNA
{
    public interface IIntervencionService
    {
        Task<bool> RegistrarNuevoIngreso(Beneficiario nuevo);
    }
}

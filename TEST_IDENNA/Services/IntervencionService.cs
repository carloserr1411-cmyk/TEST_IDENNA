using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TEST_IDENNA.Models;
using TEST_IDENNA.Repositories;

namespace TEST_IDENNA.Services
{
    public class IntervencionService : IIntervencionService
    {
        private readonly IBeneficiarioRepository _repoBeneficiario;
        private readonly IActividadRepository _repoActividad;
        public IntervencionService(IBeneficiarioRepository repoBeneficiario, IActividadRepository repoActividad)
        {
            _repoBeneficiario = repoBeneficiario;
            _repoActividad = repoActividad;
        }

        public async Task<bool> RegistrarNuevoIngreso(Beneficiario beneficiario)
        {
            // REGLA DE NEGOCIO: No permitir registros sin apellidos
            if (string.IsNullOrWhiteSpace(beneficiario.Apellidos)) return false;

            // Si todo está bien, pasamos al repositorio
            await _repoBeneficiario.Registrar(beneficiario);
            return true;
        }
        // Aquí coordinas: Guardar la actividad Y vincularla a los niños
        public async Task RegistrarActividadGrupal(Actividad actividad, List<int> idsNiños)
        {
            // 1. Validaciones (¿El personal tiene permiso?, ¿La fecha es correcta?)
            // 2. Guardar actividad principal
            // 3. Crear registros en la tabla de Asistencia para cada niño
        }

        public async Task RegistrarBeneficiario(Beneficiario beneficiario)
        {
            // Validaciones (¿El nombre es único?, ¿La edad es válida?
            await _repoBeneficiario.Registrar(beneficiario);
        }
    }
}

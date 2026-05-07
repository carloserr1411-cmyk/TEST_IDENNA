using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TEST_IDENNA.Data;
using TEST_IDENNA.Models;

namespace TEST_IDENNA.Interfaces
{

    // 2. LA IMPLEMENTACIÓN (La Clase que EF Core usará)
    public class ActividadRepository : IActividadRepository
    {
        private readonly AppDbContext _context;

        // Inyectamos el contexto para no tener que crear "new AppDbContext" manualmente
        public ActividadRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Evolucion>> ObtenerEvolucionesPorBeneficiario(int beneficiarioId)
        {
            // Usamos el contexto inyectado para la "Bitácora Cruzada"
            return await _context.Asistencia_Actividades
                .Where(asist => asist.Id_Beneficiario == beneficiarioId)
                .OrderByDescending(asist => asist.Actividad.Fecha_Registro)
                .Select(asist => new Evolucion
                {
                    Fecha_Registro = asist.Actividad.Fecha_Registro,
                    Actividad = asist.Actividad,
                    //Especialista = asist.Actividad.UsuarioResponsable.Nombre_Usuario,
                    Detalle = asist.Desempeño_Individual,
                    Beneficiario = asist.BeneficiarioAsistente
                })
                .ToListAsync();
        }

        public async Task<int> GuardarActividad(Actividad actividad)
        {
            _context.Actividades.Add(actividad);
            await _context.SaveChangesAsync();
            return actividad.Id_Actividad;
        }

        public async Task VincularNiñoAActividad(int actividadId, int beneficiarioId, string evolucionEspecifica)
        {
            var asistencia = new AsistenciaActividad
            {
                Id_Actividad = actividadId,
                Id_Beneficiario = beneficiarioId,
                Desempeño_Individual = evolucionEspecifica
            };

            _context.Asistencia_Actividades.Add(asistencia);
            await _context.SaveChangesAsync();
        }
    }
}
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TEST_IDENNA.Data;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Models;

namespace TEST_IDENNA.Repositories
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
                    Especialista = asist.Tutor,
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
                Desempeño_Individual = evolucionEspecifica,
                Area = "Área por Definir", // Puedes ajustar esto según tu lógica de negocio
                ActividadAsociada = await _context.Actividades.FindAsync(actividadId),
                BeneficiarioAsistente = await _context.Beneficiarios.FindAsync(beneficiarioId),
                Tutor = await _context.Tutores.FirstOrDefaultAsync(), // Asigna el tutor adecuado según tu lógica
                Actividad = await _context.Actividades.FindAsync(actividadId) // Relación directa con la actividad
            };

            _context.Asistencia_Actividades.Add(asistencia);
            await _context.SaveChangesAsync();
        }
    }
}
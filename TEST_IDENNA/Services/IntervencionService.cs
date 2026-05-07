using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TEST_IDENNA.Data;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Models;
using TEST_IDENNA.Repositories;

namespace TEST_IDENNA.Services
{
    public class IntervencionService : IIntervencionService
    {
        private readonly IBeneficiarioRepository _repoBeneficiario;
        private readonly IActividadRepository _repoActividad;
        private readonly AppDbContext _context;
        public IntervencionService(IBeneficiarioRepository repoBeneficiario, IActividadRepository repoActividad, AppDbContext context)
        {
            _repoBeneficiario = repoBeneficiario;
            _repoActividad = repoActividad;
            _context = context;
        }

        /*public async Task<IEnumerable<Evolucion>> ObtenerIntervencionesPorBeneficiario(int idBeneficiario)
        {
            // El servicio simplemente le pide al repositorio los datos ya filtrados
            return await _repoActividad.ObtenerEvolucionesPorBeneficiario(idBeneficiario);
        }*/

        public async Task<bool> RegistrarNuevoIngreso(Beneficiario beneficiario)
        {
            // REGLA DE NEGOCIO: No permitir registros sin apellidos, nombres o fecha de nacimiento
            if (string.IsNullOrWhiteSpace(beneficiario.Apellidos) || string.IsNullOrWhiteSpace(beneficiario.Nombres) || beneficiario.Estatus_Legal == null) return false;

            // Si todo está bien, pasamos al repositorio
            await _repoBeneficiario.Registrar(beneficiario);
            return true;
        }
        // Aquí coordinas: Guardar la actividad Y vincularla a los niños
        public async Task RegistrarActividadGrupal(Actividad actividad, List<int> idsNiños)
        {
            // 1. Guardamos la actividad principal (Ej: "Taller de Fútbol")
            // Obtenemos el ID que la base de datos le asigne
            int nuevaActividadId = await _repoActividad.GuardarActividad(actividad);

            // 2. Por cada niño seleccionado, creamos el vínculo en la tabla de asistencia
            foreach (var niñoId in idsNiños)
            {
                // Aquí podrías pasar una nota genérica o individual
                string notaInicial = "Asistencia confirmada en actividad grupal.";

                await _repoActividad.VincularNiñoAActividad(nuevaActividadId, niñoId, notaInicial);
            }
        }

        public async Task RegistrarBeneficiario(Beneficiario beneficiario)
        {
            // Validaciones (¿El nombre es único?, ¿La edad es válida?
            await _repoBeneficiario.Registrar(beneficiario);
        }

        public async Task<IEnumerable<Actividad>> ObtenerTodasLasActividades()
        {
            return await _context.Actividades
                .OrderBy(a => a.Tipo_Actividad)
                .ToListAsync();
        }

        public async Task CrearNuevaActividad(string nombre, string area)
        {
            var actividad = new Actividad { Tipo_Actividad = nombre, Area = area };
            _context.Actividades.Add(actividad);
            await _context.SaveChangesAsync();
        }

        // --- GESTIÓN DE BITÁCORA / EVOLUCIÓN ---

        public async Task<IEnumerable<Evolucion>> ObtenerHistorialGlobal()
        {
            // Corregido: Solo incluimos las entidades relacionadas
            return await _context.Evoluciones
                .Include(e => e.Beneficiario)
                .Include(e => e.Actividad) // Asegúrate que esta propiedad exista en tu modelo Evolucion
                .OrderByDescending(e => e.Fecha_Registro)
                .ToListAsync();
        }

        public async Task<IEnumerable<Evolucion>> ObtenerIntervencionesPorBeneficiario(int beneficiarioId)
        {
            return await _context.Evoluciones
                .Include(e => e.Actividad)
                .Include(e => e.Beneficiario)
                .Where(e => e.Beneficiario.Id_Beneficiario == beneficiarioId)
                .OrderByDescending(e => e.Fecha_Registro)
                .ToListAsync();
        }

        public async Task RegistrarIntervencion(Evolucion nuevaEvolucion)
        {
            nuevaEvolucion.Fecha_Registro = DateTime.Now;
            nuevaEvolucion.Especialista = "Especialista Ejemplo"; // Aquí podrías obtener el nombre del usuario actual si tienes autenticación
            _context.Evoluciones.Add(nuevaEvolucion);
            await _context.SaveChangesAsync();
        }
    }
}

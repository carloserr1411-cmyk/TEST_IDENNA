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

        // 1. Implementación para guardar el documento físico y el texto OCR
        public async Task GuardarDocumentoAdjuntoAsync(DocumentoAdjunto documento)
        {
            _context.DocumentosAdjuntos.Add(documento);
            await _context.SaveChangesAsync();
        }

        // 2. MODIFICADO: Recuperar SOLO los documentos ACTIVOS (que no están en papelera)
        public async Task<List<DocumentoAdjunto>> ObtenerDocumentosPorBeneficiarioAsync(int beneficiarioId)
        {
            return await _context.DocumentosAdjuntos
                .Where(d => d.Id_Beneficiario == beneficiarioId && !d.IsDeleted) // 🔥 Filtro activo
                .OrderByDescending(d => d.FechaRegistro)
                .ToListAsync();
        }

        public async Task<bool> RegistrarNuevoIngreso(Beneficiario beneficiario)
        {
            if (string.IsNullOrWhiteSpace(beneficiario.Apellidos) || string.IsNullOrWhiteSpace(beneficiario.Nombres) || beneficiario.Estatus_Legal == null) return false;

            await _repoBeneficiario.Registrar(beneficiario);
            return true;
        }

        public async Task RegistrarActividadGrupal(Actividad actividad, List<int> idsNiños)
        {
            int nuevaActividadId = await _repoActividad.GuardarActividad(actividad);

            foreach (var niñoId in idsNiños)
            {
                string notaInicial = "Asistencia confirmada en actividad grupal.";
                await _repoActividad.VincularNiñoAActividad(nuevaActividadId, niñoId, notaInicial);
            }
        }

        public async Task RegistrarBeneficiario(Beneficiario beneficiario)
        {
            await _repoBeneficiario.Registrar(beneficiario);
        }

        public async Task<IEnumerable<Actividad>> ObtenerTodasLasActividades()
        {
            return await _context.Actividades
                .OrderBy(a => a.Tipo_Actividad)
                .ToListAsync();
        }

        public async Task<IEnumerable<Tutores>> ObtenerTodosLosTutores()
        {
            return await _context.Tutores
                .OrderBy(a => a.NombreCompleto)
                .ToListAsync();
        }

        public async Task CrearNuevaActividad(string nombre, string area)
        {
            var actividad = new Actividad { Tipo_Actividad = nombre, Area = area };
            _context.Actividades.Add(actividad);
            await _context.SaveChangesAsync();
        }

        // --- GESTIÓN DE BITÁCORA / EVOLUCIÓN ---

        // MODIFICADO: El historial global tampoco debería mostrar lo que está en la papelera
        public async Task<IEnumerable<Evolucion>> ObtenerHistorialGlobal()
        {
            return await _context.Evoluciones
                .Include(e => e.Beneficiario)
                .Include(e => e.Actividad)
                .Include(e => e.Especialista)
                .Where(e => !e.IsDeleted) // 🔥 Filtro activo
                .OrderByDescending(e => e.Fecha_Registro)
                .ToListAsync();
        }

        // MODIFICADO: Recuperar SOLO las evoluciones ACTIVAS del menor
        public async Task<IEnumerable<Evolucion>> ObtenerIntervencionesPorBeneficiario(int beneficiarioId)
        {
            return await _context.Evoluciones
                .Include(e => e.Actividad)
                .Include(e => e.Beneficiario)
                .Where(e => e.Beneficiario.Id_Beneficiario == beneficiarioId && !e.IsDeleted) // 🔥 Filtro activo
                .OrderByDescending(e => e.Fecha_Registro)
                .ToListAsync();
        }

        public async Task RegistrarIntervencion(Evolucion nuevaEvolucion)
        {
            _context.Evoluciones.Add(nuevaEvolucion);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateEvolucionAsync(Evolucion evolucion)
        {
            _context.Evoluciones.Update(evolucion);
            await _context.SaveChangesAsync();
        }


        // =========================================================================
        // 🔥 NUEVOS MÉTODOS: SISTEMA DE PAPELERA (SOFT DELETE)
        // =========================================================================

        #region PAPELERA DE EVOLUCIONES

        public async Task<IEnumerable<Evolucion>> ObtenerEvolucionesPapeleraAsync()
        {
            return await _context.Evoluciones
                .Include(e => e.Actividad)
                .Include(e => e.Especialista)
                .Where(e => e.IsDeleted)
                .OrderByDescending(e => e.FechaEliminacion)
                .ToListAsync();
        }

        public async Task<bool> MoverEvolucionAPapeleraAsync(int id)
        {
            var evolucion = await _context.Evoluciones.FindAsync(id); // Si tu PK se llama distinto (ej: Id_Evolucion), cámbialo aquí
            if (evolucion == null) return false;

            evolucion.IsDeleted = true;
            evolucion.FechaEliminacion = DateTime.Now;

            _context.Evoluciones.Update(evolucion);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> RestaurarEvolucionAsync(int id)
        {
            var evolucion = await _context.Evoluciones.FindAsync(id);
            if (evolucion == null) return false;

            evolucion.IsDeleted = false;
            evolucion.FechaEliminacion = null;

            _context.Evoluciones.Update(evolucion);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> EliminarEvolucionPermanenteAsync(int id)
        {
            var evolucion = await _context.Evoluciones.FindAsync(id);
            if (evolucion == null) return false;

            _context.Evoluciones.Remove(evolucion); // Borrado físico definitivo
            return await _context.SaveChangesAsync() > 0;
        }

        #endregion

        #region PAPELERA DE DOCUMENTOS ADJUNTOS (ARCHIVOS DIGITALES)

        public async Task<IEnumerable<DocumentoAdjunto>> ObtenerDocumentosPapeleraAsync()
        {
            return await _context.DocumentosAdjuntos
                .Include(d => d.Beneficiario)
                .Where(d => d.IsDeleted)
                .OrderByDescending(d => d.FechaEliminacion)
                .ToListAsync();
        }

        public async Task<bool> MoverDocumentoAPapeleraAsync(int id)
        {
            var documento = await _context.DocumentosAdjuntos.FindAsync(id); // Si tu PK se llama distinto, cámbialo aquí
            if (documento == null) return false;

            documento.IsDeleted = true;
            documento.FechaEliminacion = DateTime.Now;

            _context.DocumentosAdjuntos.Update(documento);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> RestaurarDocumentoAsync(int id)
        {
            var documento = await _context.DocumentosAdjuntos.FindAsync(id);
            if (documento == null) return false;

            documento.IsDeleted = false;
            documento.FechaEliminacion = null;

            _context.DocumentosAdjuntos.Update(documento);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> EliminarDocumentoPermanenteAsync(int id)
        {
            var documento = await _context.DocumentosAdjuntos.FindAsync(id);
            if (documento == null) return false;

            _context.DocumentosAdjuntos.Remove(documento); // Borrado físico definitivo
            return await _context.SaveChangesAsync() > 0;
        }

        #endregion

        #region PAPELERA DE BENEFICIARIOS (SOFT DELETE)
        
        public async Task<IEnumerable<Beneficiario>> ObtenerBeneficiariosPapeleraAsync()
        {
            return await _context.Beneficiarios
                .Where(b => b.Estatus == "Eliminado")
                .OrderByDescending(b => b.FechaEliminacion)
                .ToListAsync();
        }

        public async Task<bool> MoverBeneficiarioAPapeleraAsync(int id)
        {
            var beneficiario = await _context.Beneficiarios.FindAsync(id);
            if (beneficiario == null) return false;
            beneficiario.Estatus = "Eliminado";
            beneficiario.FechaEliminacion = DateTime.Now;
            _context.Beneficiarios.Update(beneficiario);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> RestaurarBeneficiarioAsync(int id)
        {
            var beneficiario = await _context.Beneficiarios.FindAsync(id);
            if (beneficiario == null) return false;
            beneficiario.Estatus = "Activo";
            beneficiario.FechaEliminacion = null;
            _context.Beneficiarios.Update(beneficiario);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> EliminarBeneficiarioPermanenteAsync(int id)
        {
            var beneficiario = await _context.Beneficiarios.FindAsync(id);
            if (beneficiario == null) return false;
            _context.Beneficiarios.Remove(beneficiario); // Borrado físico definitivo
            return await _context.SaveChangesAsync() > 0;
        }

        #endregion
    }
}
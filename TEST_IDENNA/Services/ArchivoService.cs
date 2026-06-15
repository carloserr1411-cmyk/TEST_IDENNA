using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TEST_IDENNA.Data;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Models;
using TEST_IDENNA.Repositories;

namespace TEST_IDENNA.Services
{
    public class ArchivoService : IArchivoService
    {
        private readonly IBeneficiarioRepository _repository;

        public ArchivoService(IBeneficiarioRepository beneficiarioService)
        {
            _repository = beneficiarioService;
        }

        public async Task<IEnumerable<ExpedienteFisico>> ObtenerExpedientesFisicosAsync()
        {
            // 1. Llamamos al servicio que ya tienes hecho
            var beneficiariosReales = await _repository.ObtenerTodos();

            // 2. Filtramos solo los activos y mapeamos al modelo de la UI
            // NOTA: Ajusta "e.Estado", "e.CedulaEscolar", etc., a los nombres reales de tu clase Beneficiario
            return beneficiariosReales
                .Where(b => b.Estatus_Legal != "Egresado")
                .Select(b => new ExpedienteFisico
                {
                    CodigoExpediente = b.Cedula, // o b.CedulaEscolar / b.Id
                    Beneficiario = b,
                    UbicacionFisica = b.UbicacionFisica ?? "No asignada", // Campo de tu BD donde guardas el estante/caja
                    FechaActualizacionUbicacion = b.FechaModificacion ?? DateTime.Now
                });
        }

        public async Task<IEnumerable<Egreso>> ObtenerCasosCerradosAsync()
        {
            try
            {
                // Asumiendo que tu DbContext se llama _context y tu DbSet es Egresos
                using (var context = new AppDbContext())
                {
                    return await context.Egresos
                                     .Include(e => e.Beneficiario)
                                     .AsNoTracking() // Optimiza la velocidad de lectura
                                     .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                // Es buena práctica registrar el error o relanzarlo para que el ViewModel lo capture
                throw new Exception("Error al consultar la tabla de egresados en SQLite", ex);
            }
        }
        public async Task<IEnumerable<AuditoriaMovimiento>> ObtenerAuditoriasAsync() => Enumerable.Empty<AuditoriaMovimiento>();
    }
}
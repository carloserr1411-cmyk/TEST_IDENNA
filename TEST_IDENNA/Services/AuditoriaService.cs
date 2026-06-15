using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TEST_IDENNA.Data;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Models;

namespace TEST_IDENNA.Services
{
    public class AuditoriaService : IAuditoriaService
    {
        private readonly AppDbContext _context;

        public AuditoriaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task RegistrarAccionAsync(string accion, string modulo, string detalles)
        {
            try
            {
                var registro = new AuditoriaMovimiento
                {
                    // TRUCO: Si no hay usuario logueado, toma el nombre de la PC (ej: "Carlos-PC")
                    Computadora = Environment.MachineName,
                    Usuario = SesionSistema.IsLoggedIn ? SesionSistema.UsuarioActual!.NombreUsuario : Environment.UserName,
                    Rol = SesionSistema.IsLoggedIn ? SesionSistema.UsuarioActual!.Rol : "NO IDENTIFICADO",
                    Accion = accion.ToUpper().Trim(),
                    Modulo = modulo,
                    Detalles = detalles,
                    Fecha = DateTime.Now
                };

                _context.Auditorias.Add(registro);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // En auditoría es vital que si el log falla, NO se detenga la app principal.
                // Puedes registrarlo en un archivo de texto local (.log) si lo deseas.
            }
        }

        public async Task<IEnumerable<AuditoriaMovimiento>> ObtenerTodoAsync()
        {
            try
            {
                // Trae todos los registros de la tabla de auditoría en SQLite
                return await _context.Auditorias
                                     .AsNoTracking()
                                     .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error al consultar el historial de auditoría en la base de datos.", ex);
            }
        }
    }
}

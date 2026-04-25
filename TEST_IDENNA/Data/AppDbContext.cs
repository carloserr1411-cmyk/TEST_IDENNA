using System;
using Microsoft.EntityFrameworkCore;
using TEST_IDENNA.Models;

namespace TEST_IDENNA.Data
{
    public class AppDbContext : DbContext, IDisposable
    {
        public DbSet<Beneficiario> Beneficiarios { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Actividad> Actividades { get; set; }
        public DbSet<Evolucion> Evoluciones { get; set; }
        public DbSet<AsistenciaActividad> Asistencia_Actividades { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Se define el nombre del archivo de la base de datos
            optionsBuilder.UseSqlite("Data Source=idenna_sistema.db");
        }

        // Implementación explícita de IDisposable para asegurar compatibilidad con 'using'
        public new void Dispose()
        {
            // Llama al Dispose de la clase base si está disponible
            try
            {
                base.Dispose();
            }
            catch
            {
                // Si la clase base no expone Dispose públicamente por alguna razón,
                // capturamos la excepción para evitar introducir errores de compilación adicionales.
                throw;
            }
            GC.SuppressFinalize(this);
        }
    }
}

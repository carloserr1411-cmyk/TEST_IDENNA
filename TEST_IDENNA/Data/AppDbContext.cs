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
        public DbSet<Egreso> Egresos { get; set; }

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. Dile a EF que ignore la lista de evoluciones en el Usuario
            modelBuilder.Entity<Usuario>().Ignore(u => u.EvolucionesRegistradas);

            modelBuilder.Entity<Evolucion>()
                .HasKey(e => e.Id_Evolucion);
            // Configuramos la relación entre Actividad y Asistencia
            modelBuilder.Entity<AsistenciaActividad>()
                .HasOne(a => a.Actividad)
                .WithMany(act => act.Asistentes) // O el nombre que tengas en tu modelo Actividad
                .HasForeignKey(a => a.Id_Actividad);

            // Configuramos la relación entre Beneficiario (Niño) y Asistencia
            modelBuilder.Entity<AsistenciaActividad>()
                .HasOne(a => a.BeneficiarioAsistente)
                .WithMany() // Si el niño no tiene una lista de asistencias en su modelo, déjalo vacío
                .HasForeignKey(a => a.Id_Beneficiario);

            base.OnModelCreating(modelBuilder);
        }
    }
}

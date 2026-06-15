using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
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
        public DbSet<Tutores> Tutores { get; set; }
        public DbSet<DocumentoAdjunto> DocumentosAdjuntos { get; set; }
        public DbSet<AuditoriaMovimiento> Auditorias { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Crea o busca la carpeta AppData local de la máquina cliente
            string rutaAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string carpetaSistema = Path.Combine(rutaAppData, "SistemaIdenna");

            // Asegura que la carpeta exista
            Directory.CreateDirectory(carpetaSistema);

            string rutaBD = Path.Combine(carpetaSistema, "idenna_sistema.db");
            optionsBuilder.UseSqlite($"Data Source={rutaBD}");
        }

        /// <summary>
        /// Intercepta también los guardados síncronos tradicionales, por si acaso
        /// se usan en alguna parte del sistema.
        /// </summary>
        public override int SaveChanges()
        {
            var entradas = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entrada in entradas)
            {
                if (entrada.Entity is Beneficiario beneficiario)
                {
                    beneficiario.FechaModificacion = DateTime.Now;
                }
            }

            return base.SaveChanges();
        }

        /// <summary>
        /// Intercepta de forma automática todos los guardados asíncronos del sistema
        /// para estampar la fecha de modificación en la entidad Beneficiario.
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Buscamos cualquier registro que se esté insertando (Added) o editando (Modified)
            var entradas = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entrada in entradas)
            {
                // Si lo que se está guardando es un Beneficiario, actualizamos su fecha
                if (entrada.Entity is Beneficiario beneficiario)
                {
                    beneficiario.FechaModificacion = DateTime.Now;
                }
            }

            // Continuamos con el flujo normal de guardado de EF Core
            return base.SaveChangesAsync(cancellationToken);
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

            // Configurar la Cédula del Beneficiario como única
            modelBuilder.Entity<Beneficiario>()
                .HasIndex(b => b.Cedula)
                .IsUnique();
        }
    }
}

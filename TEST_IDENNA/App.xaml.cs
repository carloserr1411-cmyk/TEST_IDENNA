using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using System;
using System.Windows;
using TEST_IDENNA.Data;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Models;
using TEST_IDENNA.Repositories;
using TEST_IDENNA.Services;
using TEST_IDENNA.ViewModels;
using TEST_IDENNA.Views; // <--- Asegúrate de incluir tus vistas

namespace TEST_IDENNA
{
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // 1. Base de Datos
            services.AddDbContext<AppDbContext>();

            // 2. Servicios de Utilidad generales
            services.AddSingleton<OcrService>();

            // 3. Repositorios y Servicios de Negocio
            services.AddScoped<IEgresoRepository, EgresoRepository>();
            services.AddScoped<IBeneficiarioRepository, BeneficiarioRepository>();
            services.AddScoped<IActividadRepository, ActividadRepository>();
            services.AddScoped<ITutorRepository, TutorRepository>();
            services.AddScoped<IIntervencionService, IntervencionService>();
            services.AddScoped<IArchivoService, ArchivoService>();
            services.AddScoped<IAuditoriaService, AuditoriaService>();

            // 4. ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<RegistroBeneficiarioViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<BitacoraViewModel>();
            services.AddTransient<AuditoriasViewModel>();
            services.AddTransient<ReportesViewModel>();
            services.AddTransient<DigitalizarDocumentosViewModel>();
            services.AddTransient<ExpedientesViewModel>();
            services.AddTransient<LoginViewModel>();

            // 5. Ventanas (¡AGREGAMOS LOGINWINDOW AQUÍ!)
            services.AddSingleton<MainWindow>();
            services.AddTransient<LoginWindow>(); // <--- CLAVE: Registrada en el contenedor
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            base.OnStartup(e);

            using (var scope = ServiceProvider!.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();

                // Verificamos si la tabla de usuarios está vacía
                if (!db.Usuarios.Any())
                {
                    var admin = new Usuario
                    {
                        NombreUsuario = "admin",
                        PasswordHash = "hash123", // Cambia esto por tu contraseña de prueba
                        NombreCompleto = "Administrador del Sistema",
                        Rol = "Administrador",
                        Activo = true
                    };

                    db.Usuarios.Add(admin);
                    db.SaveChanges();
                }
            }

            // =========================================================================
            // CAMBIO AQUÍ: En lugar de pedir MainWindow, pedimos el LoginWindow
            // =========================================================================
            var loginWindow = ServiceProvider!.GetRequiredService<LoginWindow>();
            loginWindow.Show();
            // =========================================================================
        }
    }
}
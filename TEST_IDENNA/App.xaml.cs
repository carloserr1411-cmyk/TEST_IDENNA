using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TEST_IDENNA.Data;
using TEST_IDENNA.Repositories;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Services;
using TEST_IDENNA.ViewModels;

namespace TEST_IDENNA
{
    /// <summary>
    /// Lógica de interacción para App.xaml
    /// </summary>
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

            // 2. Repositorios y Servicios
            // ✅ CORRECCIÓN: Interfaz vinculada a su Clase Real
            services.AddScoped<IBeneficiarioRepository, BeneficiarioRepository>();
            services.AddScoped<IActividadRepository, ActividadRepository>();

            services.AddScoped<IIntervencionService, IntervencionService>();

            // Ventana Principal
            services.AddSingleton<MainWindow>();

            // 3. ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<RegistroBeneficiarioViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<BitacoraViewModel>();
            services.AddTransient<ArchivosViewModel>();
            services.AddTransient<ReportesViewModel>();

            // ✅ IMPORTANTE: Añade este, es el que controla la vista de la imagen
            services.AddTransient<ExpedientesViewModel>();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            //DatabaseConfig.InitializeDatabase(); // Crea la DB al iniciar
            /*var registroVm = new RegistroViewModel();
            registroVm.CargarDatos();*/

            using (var db = new AppDbContext())
            {
                // Esto crea la base de datos y las tablas si no existen
                db.Database.EnsureCreated();
            }

            // 2. PEDIR la ventana al ServiceProvider
            // Esto resuelve: MainWindow -> MainWindowViewModel -> IntervencionService -> Repositorios
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();

            // 3. Mostrar la ventana
            mainWindow.Show();
        }
    }
}

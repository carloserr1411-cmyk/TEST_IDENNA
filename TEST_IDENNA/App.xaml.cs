using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TEST_IDENNA.Data;
using TEST_IDENNA.ViewModels;

namespace TEST_IDENNA
{
    /// <summary>
    /// Lógica de interacción para App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DatabaseConfig.InitializeDatabase(); // Crea la DB al iniciar
            /*var registroVm = new RegistroViewModel();
            registroVm.CargarDatos();*/
        }
    }
}

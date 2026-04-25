using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TEST_IDENNA.Services;
using TEST_IDENNA.ViewModels;

namespace TEST_IDENNA
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel; // Aquí se hace la magia

            //this.DataContext = App.ServiceProvider.GetRequiredService<MainWindowViewModel>();
            // Pedimos al contenedor que nos dé el MainViewModel con todo incluido
            //this.DataContext = App.ServiceProvider.GetRequiredService<MainWindow>();
            /*var registroVm = new RegistroViewModel();
            registroVm.CargarDatos();*/
        }
    }
}

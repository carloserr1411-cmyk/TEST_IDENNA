using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TEST_IDENNA.ViewModels;

namespace TEST_IDENNA.Views
{
    /// <summary>
    /// Lógica de interacción para SeleccionarBeneficiariosView.xaml
    /// </summary>
    public partial class SeleccionarBeneficiariosView : Window
    {
        public SeleccionarBeneficiariosView(SeleccionarBeneficiariosViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
        private void BotonAceptar_Click(object sender, RoutedEventArgs e)
        {
            // Devuelve True al ShowDialog() cerrando la ventana y confirmando la selección
            this.DialogResult = true;
        }

        private void BotonCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

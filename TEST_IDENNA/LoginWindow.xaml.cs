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
    /// Lógica de interacción para LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        // El contenedor de dependencias pasará automáticamente el LoginViewModel aquí
        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }

        private void BtnOjo_Checked(object sender, RoutedEventArgs e)
        {
            // Al activar el ojo, pasamos el texto oculto al TextBox visible
            TxtPasswordVisible.Text = TxtPassword.Password;

            TxtPassword.Visibility = Visibility.Collapsed;
            TxtPasswordVisible.Visibility = Visibility.Visible;
        }

        private void BtnOjo_Unchecked(object sender, RoutedEventArgs e)
        {
            // Al cerrar el ojo, el PasswordBox vuelve a mandar
            TxtPassword.Visibility = Visibility.Visible;
            TxtPasswordVisible.Visibility = Visibility.Collapsed;
        }

        private void TxtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Si el usuario escribe o edita la contraseña mientras es visible,
            // actualizamos el PasswordBox en tiempo real.
            if (TxtPasswordVisible.Visibility == Visibility.Visible)
            {
                TxtPassword.Password = TxtPasswordVisible.Text;
            }
        }
    }
}

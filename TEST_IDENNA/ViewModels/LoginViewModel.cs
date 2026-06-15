using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TEST_IDENNA.Data;
using TEST_IDENNA.Views;

namespace TEST_IDENNA.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty] private string username = string.Empty;
        [ObservableProperty] private string mensajeError = string.Empty;
        [ObservableProperty] private string plainPassword = string.Empty;

        public LoginViewModel(AppDbContext context)
        {
            _context = context;
        }

        [RelayCommand]
        private async Task IniciarSesion(object passwordBoxObj)
        {
            var passwordBox = passwordBoxObj as PasswordBox;
            if (passwordBox == null || string.IsNullOrWhiteSpace(Username))
            {
                MensajeError = "Por favor, llene todos los campos.";
                return;
            }

            string contrasenaIngresada = passwordBox.Password;
            MensajeError = "Verificando...";

            // Buscar usuario en SQLite
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.NombreUsuario.ToLower() == Username.ToLower().Trim());

            // TODO: En producción, encripta contrasenaIngresada y compárala con usuario.PasswordHash
            if (usuario != null && usuario.PasswordHash == contrasenaIngresada)
            {
                if(usuario.Activo == false)
                {
                    MensajeError = "El usuario está inactivo. Contacte al administrador.";
                    return;
                }
            
                // 1. Guardamos el usuario en la sesión global
                SesionSistema.UsuarioActual = usuario;
                PlainPassword = string.Empty; // Limpiamos la contraseña en texto plano por seguridad
                // 2. Abrimos la MainWindow y cerramos el Login
                var mainWindow = App.ServiceProvider!.GetRequiredService<MainWindow>();
                mainWindow.Show();

                // Cerramos la ventana de Login de forma segura buscando la instancia activa
                Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is LoginWindow)?.Close();
            }
            else
            {
                MensajeError = "Usuario o contraseña incorrectos.";
                passwordBox.Clear();
            }
        }

        [RelayCommand]
        private void Salir() => Application.Current.Shutdown();
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using TEST_IDENNA.Data;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Models;

namespace TEST_IDENNA.ViewModels
{
    public partial class UsuariosViewModel : ObservableObject
    {
        private readonly IAuditoriaService _auditoriaService;

        [ObservableProperty] private ObservableCollection<Usuario> personalSistema = new();

        public UsuariosViewModel(IAuditoriaService auditoriaService)
        {
            _auditoriaService = auditoriaService;
        }

        [RelayCommand]
        private async Task CargarUsuarios()
        {
            using (var context = new AppDbContext())
            {
                var usuariosBd = await context.Usuarios
                    .AsNoTracking()
                    .Where(x => x.NombreUsuario.ToLower() != "admin")
                    .OrderBy(x => x.NombreCompleto)
                    .ToListAsync();
                PersonalSistema = new ObservableCollection<Usuario>(usuariosBd);
            }
        }

        [RelayCommand]
        private async Task AlternarEstadoUsuario(Usuario usuario)
        {
            if (usuario == null) return;
            if (usuario.Id == SesionSistema.UsuarioActual?.Id)
            {
                System.Windows.MessageBox.Show("No puedes desactivar tu propia cuenta de usuario.", "Acción denegada", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new AppDbContext())
                {
                    // Buscamos el registro real de seguimiento en BD para mutarlo
                    var usuarioBd = await context.Usuarios.FindAsync(usuario.Id);
                    if (usuarioBd != null)
                    {
                        usuarioBd.Activo = !usuarioBd.Activo;
                        await context.SaveChangesAsync();

                        // Registramos de forma obligatoria en la Auditoría
                        string estadoTexto = usuarioBd.Activo ? "ACTIVADO" : "DESACTIVADO";
                        await _auditoriaService.RegistrarAccionAsync(
                            accion: "MODIFICAR",
                            modulo: "Gestión de Usuarios",
                            detalles: $"Se cambió el estado del usuario '{usuarioBd.NombreUsuario}' a: {estadoTexto}"
                        );

                        await CargarUsuarios(); // Recargamos la grilla
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al cambiar estado: {ex.Message}");
            }
        }

        #region Comandos de Navegación para Crear/Editar

        // Propiedades para el control del Diálogo
        [ObservableProperty] private bool isDialogOpen;
        [ObservableProperty] private string tituloDialogo = "REGISTRAR NUEVO USUARIO";
        [ObservableProperty] private bool esModoEdicion;

        // Propiedades del Formulario
        [ObservableProperty] private string editNombreCompleto = string.Empty;
        [ObservableProperty] private string editNombreUsuario = string.Empty;
        [ObservableProperty] private string editPassword = string.Empty; // En edición puede quedar vacío para no cambiarla
        [ObservableProperty] private string? editRol;
        [ObservableProperty] private bool editActivo = true;

        private Usuario? _usuarioEnEdicion;
        // Lista de roles para el ComboBox del modal
        public List<string> RolesDisponibles { get; } = new() { "Administrador", "UsuarioComun" };

        [RelayCommand]
        private void AbrirFormularioCrear()
        {
            _usuarioEnEdicion = null;
            EsModoEdicion = false;
            TituloDialogo = "REGISTRAR NUEVO USUARIO";

            // Limpiar campos
            EditNombreCompleto = string.Empty;
            EditNombreUsuario = string.Empty;
            EditPassword = string.Empty;
            EditRol = RolesDisponibles[1]; // Usuario Comun por defecto
            EditActivo = true;

            IsDialogOpen = true;
        }

        [RelayCommand]
        private void AbrirFormularioEditar(Usuario usuario)
        {
            if (usuario == null) return;
            _usuarioEnEdicion = usuario;
            EsModoEdicion = true;
            TituloDialogo = "EDITAR PERFIL DE USUARIO";

            // Poblar campos
            EditNombreCompleto = usuario.NombreCompleto;
            EditNombreUsuario = usuario.NombreUsuario;
            EditPassword = string.Empty; // No mostramos el hash por seguridad
            EditRol = usuario.Rol;
            EditActivo = usuario.Activo;

            IsDialogOpen = true;
        }

        [RelayCommand]
        private async Task GuardarUsuario(object? parameter)
        {
            // 1. Extraemos el texto directamente del PasswordBox visual
            string contrasenaIngresada = string.Empty;
            System.Windows.Controls.PasswordBox? passwordBox = parameter as System.Windows.Controls.PasswordBox;

            if (passwordBox != null)
            {
                contrasenaIngresada = passwordBox.Password;
            }

            // 2. Validaciones estrictas de campos de texto comunes
            if (string.IsNullOrWhiteSpace(EditNombreCompleto) || string.IsNullOrWhiteSpace(EditNombreUsuario))
            {
                System.Windows.MessageBox.Show("El nombre completo y el ID de usuario son campos obligatorios.",
                    "Validación de Datos", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // 3. Validación ESTRICTA de contraseña: Si es un usuario NUEVO y no escribió nada -> ERROR
            if (!EsModoEdicion && string.IsNullOrWhiteSpace(contrasenaIngresada))
            {
                System.Windows.MessageBox.Show("Debe asignar una contraseña para poder registrar al nuevo usuario.",
                    "Contraseña Requerida", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new AppDbContext())
                {
                    if (EsModoEdicion && _usuarioEnEdicion != null)
                    {
                        // --- LÓGICA DE EDICIÓN ---
                        var u = await context.Usuarios.FindAsync(_usuarioEnEdicion.Id);
                        if (u != null)
                        {
                            u.NombreCompleto = EditNombreCompleto;
                            u.NombreUsuario = EditNombreUsuario;
                            u.Rol = EditRol ?? "UsuarioComun";
                            u.Activo = EditActivo;

                            // En edición, solo cambia la clave si el admin escribió una nueva
                            if (!string.IsNullOrWhiteSpace(contrasenaIngresada))
                                u.PasswordHash = contrasenaIngresada;

                            await context.SaveChangesAsync();
                            await _auditoriaService.RegistrarAccionAsync("MODIFICAR", "Usuarios", $"Se editó al usuario: {u.NombreUsuario}");
                        }
                    }
                    else
                    {
                        // --- LÓGICA DE CREACIÓN (SIN PREDETERMINADOS) ---
                        var nuevo = new Usuario
                        {
                            NombreCompleto = EditNombreCompleto,
                            NombreUsuario = EditNombreUsuario,
                            PasswordHash = contrasenaIngresada, // Se guarda exactamente lo que se tipeó
                            Rol = EditRol ?? "UsuarioComun",
                            Activo = EditActivo
                        };

                        context.Usuarios.Add(nuevo);
                        await context.SaveChangesAsync();
                        await _auditoriaService.RegistrarAccionAsync("CREAR", "Usuarios", $"Se registró nuevo usuario: {nuevo.NombreUsuario}");
                    }
                }

                // 4. Limpieza de seguridad: Borramos el texto del PasswordBox visual para que no se quede guardado en memoria
                passwordBox?.Clear();

                // 5. Cerrar modal y refrescar la tabla
                IsDialogOpen = false;
                await CargarUsuarios();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error crítico al guardar en la base de datos: {ex.Message}",
                    "Error de Sistema", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Cancelar() => IsDialogOpen = false;
        #endregion
    }
}
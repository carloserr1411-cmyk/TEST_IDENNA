using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Models;
using TEST_IDENNA.Services;

namespace TEST_IDENNA.ViewModels
{
    public partial class RegistroBeneficiarioViewModel(IIntervencionService service, IBeneficiarioRepository repository) : ObservableObject
    {
        private readonly IIntervencionService _service = service;
        private readonly IBeneficiarioRepository _repository = repository;

        [ObservableProperty]
        private string _tituloVista = "Registrar Nuevo Beneficiario";

        [ObservableProperty]
        private string _botonGuardarTexto = "REGISTRAR BENEFICIARIO";

        [ObservableProperty]
        private Beneficiario _modeloFormulario = GenerarNuevoBeneficiario();

        [ObservableProperty]
        private byte[]? _fotoSeleccionada;

        private static Beneficiario GenerarNuevoBeneficiario() => new()
        {
            Fecha_Nacimiento = DateTime.Today.AddYears(-12),
            Fecha_Ingreso = DateTime.Now,
            Estatus_Legal = "En Proceso de Investigación",
            Observaciones = string.Empty
        };

        public void SetBeneficiarioParaEdicion(Beneficiario beneficiario)
        {
            ModeloFormulario = beneficiario;
            FotoSeleccionada = beneficiario.Foto; // Cargamos la foto actual
            _esEdicion = true;
            TituloVista = "Editar Datos del Beneficiario";
            BotonGuardarTexto = "GUARDAR CAMBIOS";
        }

        [ObservableProperty]
        private Beneficiario _nuevoBeneficiario = new()
        {
            Fecha_Nacimiento = DateTime.Today.AddYears(-12), // Fecha promedio
            Fecha_Ingreso = DateTime.Now,
            Estatus_Legal = "En Proceso de Investigación",
            Observaciones = string.Empty
        };

        private bool _esEdicion = false;

        // Método que recibe los datos cuando navegas hacia aquí
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("BeneficiarioParaEditar"))
            {
                ModeloFormulario = (Beneficiario)query["BeneficiarioParaEditar"];
                TituloVista = "Editar Datos del Beneficiario";
                BotonGuardarTexto = "GUARDAR CAMBIOS";
                _esEdicion = true;
            }
            else
            {
                ModeloFormulario = new Beneficiario(); // Modo creación
                _esEdicion = false;
            }
        }

        /*[RelayCommand]
        public void SeleccionarFoto()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Imágenes (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                Title = "Seleccionar foto del beneficiario"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Convertimos el archivo de la ruta a un arreglo de bytes
                FotoSeleccionada = System.IO.File.ReadAllBytes(openFileDialog.FileName);
            }
        }*/

        [RelayCommand]
        public void SeleccionarFoto()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Imágenes (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                Title = "Seleccionar foto del beneficiario"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Cargamos la imagen original de forma segura
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Importante para no bloquear el archivo
                    bitmap.EndInit();

                    // Abrimos la ventana de recorte pasándole la imagen
                    // Nota: Asegúrate de tener el using de tu carpeta de Views
                    var ventanaRecorte = new TEST_IDENNA.Views.RecortarImagenView(bitmap);

                    // Mostramos la ventana de forma modal
                    if (ventanaRecorte.ShowDialog() == true)
                    {
                        // Si el usuario aceptó, obtenemos los bytes del recorte
                        FotoSeleccionada = ventanaRecorte.ImagenRecortadaBytes;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar la imagen: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task Guardar()
        {
            // Aseguramos que la foto seleccionada (o recortada) se asigne al modelo
            ModeloFormulario.Foto = FotoSeleccionada;

            if (_esEdicion)
            {
                await _repository.Actualizar(ModeloFormulario);
                //MessageBox.Show("Beneficiario actualizado con éxito"); en la siguiente linea colocare el mismo mensaje pero con una forma mas elegante, como un dialogo o algo asi, para no usar MessageBox
                var dialog = new Views.Dialogs.ExitoDialog("Beneficiario actualizado con éxito");
                dialog.ShowDialog();
            }
            else
            {
                //await _repository.Registrar(ModeloFormulario
                await Registrar(); // Llamamos al método que ya tiene la lógica de registro
                //MessageBox.Show("Beneficiario registrado con éxito");
                var dialog = new Views.Dialogs.ExitoDialog("Beneficiario registrado con éxito");
                dialog.ShowDialog();
            }

            Volver(); // Regresamos a la lista
        }

        [RelayCommand]
        private async Task Registrar()
        {
            // Llamamos al servicio de negocio
            NuevoBeneficiario.Foto = FotoSeleccionada;
            bool exito = await _service.RegistrarNuevoIngreso(NuevoBeneficiario);

            if (exito)
            {
                // Limpiar formulario o mostrar mensaje
                //NuevoBeneficiario = new Beneficiario();
                NuevoBeneficiario = new Beneficiario
                {
                    Fecha_Nacimiento = DateTime.Today.AddYears(-12), // Fecha promedio
                    Fecha_Ingreso = DateTime.Now,
                    Estatus_Legal = "En Proceso de Investigación",
                    Observaciones = string.Empty
                };
                FotoSeleccionada = null;
            }
        }

        [RelayCommand]
        private async Task Cancelar()
        {
                // Lógica para cancelar el registro, por ejemplo, limpiar el formulario
                NuevoBeneficiario = new Beneficiario
                {
                    Fecha_Nacimiento = DateTime.Today.AddYears(-12), // Fecha promedio
                    Fecha_Ingreso = DateTime.Now,
                    Estatus_Legal = "En Proceso de Investigación",
                    Observaciones = string.Empty
                };

            FotoSeleccionada = null;
        }

        [RelayCommand]
        public void Volver()
        {
            // Enviamos el mensaje para cambiar la vista de regreso a Expedientes
            WeakReferenceMessenger.Default.Send(new NavegarMensaje(new ExpedientesViewModel(_repository, _service)));
        }
    }
}

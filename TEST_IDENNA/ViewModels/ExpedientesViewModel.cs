using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Models;
using TEST_IDENNA.Services;

namespace TEST_IDENNA.ViewModels
{
    public partial class ExpedientesViewModel : ObservableObject
    {
        private readonly IBeneficiarioRepository _repository;
        private readonly IIntervencionService _service;

        [ObservableProperty]
        private ObservableCollection<Actividad> _actividadesDisponibles = new();

        [ObservableProperty]
        private Actividad? _actividadSeleccionada;

        [ObservableProperty]
        private string _nuevaEvolucionTexto = string.Empty;

        [ObservableProperty]
        private string? _searchText;

        [ObservableProperty]
        private bool _isEvolucionDialogOpen;

        [ObservableProperty]
        private ObservableCollection<Beneficiario> _resultadosBusqueda = new();

        // Lo inicializamos como null para que el Converter de XAML oculte la vista hasta seleccionar uno
        [ObservableProperty]
        private Beneficiario? _beneficiarioActual;

        [ObservableProperty]
        private ObservableCollection<Evolucion> _listaEvoluciones = new();

        [ObservableProperty]
        private byte[]? _fotoSeleccionada;

        public bool HayResultados => ResultadosBusqueda.Count > 0;

        public ExpedientesViewModel(IBeneficiarioRepository repository, IIntervencionService service)
        {
            _repository = repository;
            _service = service;

            // Llamamos a la carga inicial de actividades
            _ = CargarActividadesAsync();
        }

        [RelayCommand]
        private async Task NavegarAEditar()
        {
            if (BeneficiarioActual == null) return;
            var vm = new RegistroBeneficiarioViewModel(_service, _repository);
            vm.SetBeneficiarioParaEdicion(BeneficiarioActual.Clonar());
            WeakReferenceMessenger.Default.Send(new NavegarMensaje(vm));

            // Aquí usamos el servicio de navegación para ir a RegistroBeneficiario
            // Pasamos el objeto actual como parámetro
            //WeakReferenceMessenger.Default.Send(new NavegarMensaje(new RegistroBeneficiarioViewModel(_service, _repository)));
            //await _navigationService.NavigateToAsync<RegistroBeneficiarioViewModel>(BeneficiarioActual);
        }

        private async Task CargarActividadesAsync()
        {
            try
            {
                // 1. Obtener la lista del servicio
                var lista = await _service.ObtenerTodasLasActividades();

                // 2. Asignarla a la propiedad observable
                ActividadesDisponibles = new ObservableCollection<Actividad>(lista);
            }
            catch (Exception ex)
            {
                // Aquí podrías manejar errores de conexión a la DB
                System.Diagnostics.Debug.WriteLine($"Error cargando actividades: {ex.Message}");
            }
        }

        // Lógica de búsqueda automática
        partial void OnSearchTextChanged(string? value)
        {
            _ = BuscarBeneficiario();
        }

        [RelayCommand]
        public async Task BuscarBeneficiario()
        {
            if (string.IsNullOrWhiteSpace(SearchText) || SearchText.Length < 1)
            {
                ResultadosBusqueda.Clear();
                OnPropertyChanged(nameof(HayResultados));
                return;
            }

            var lista = await _repository.ObtenerPorNombre(SearchText);
            ResultadosBusqueda = new ObservableCollection<Beneficiario>(lista);
            OnPropertyChanged(nameof(HayResultados));
        }

        [RelayCommand]
        public async Task SeleccionarBeneficiario(Beneficiario seleccionado)
        {
            if (seleccionado == null) return;

            BeneficiarioActual = seleccionado;

            // Limpiamos búsqueda
            SearchText = string.Empty;
            ResultadosBusqueda.Clear();
            OnPropertyChanged(nameof(HayResultados));

            // Cargamos la historia clínica/evolutiva de la DB
            await CargarHistorialEvolucion(seleccionado.Id_Beneficiario);
        }

        [RelayCommand]
        public async Task CargarHistorialEvolucion(int beneficiarioId)
        {
            // Obtenemos los datos reales del servicio
            var historial = await _service.ObtenerIntervencionesPorBeneficiario(beneficiarioId);

            // Actualizamos la lista vinculada al ItemsControl
            ListaEvoluciones = new ObservableCollection<Evolucion>(historial);
        }

        [RelayCommand]
        public async Task GuardarEvolucion()
        {
            if (BeneficiarioActual == null || ActividadSeleccionada == null || string.IsNullOrWhiteSpace(NuevaEvolucionTexto))
            {
                // Aquí podrías disparar una notificación de error
                return;
            }

            var nuevaEvo = new Evolucion
            {
                Id_Beneficiario = BeneficiarioActual.Id_Beneficiario,
                Id_Actividad = ActividadSeleccionada.Id_Actividad, // Ajusta según el nombre en tu modelo
                Detalle = NuevaEvolucionTexto,
                Fecha_Registro = DateTime.Now
            };

            await _service.RegistrarIntervencion(nuevaEvo);

            // Limpiar formulario y refrescar la línea de tiempo
            NuevaEvolucionTexto = string.Empty;
            ActividadSeleccionada = null;
            await CargarHistorialEvolucion(BeneficiarioActual.Id_Beneficiario);

            IsEvolucionDialogOpen = false;
        }

        [RelayCommand]
        public void NavegarARegistro()
        {
            WeakReferenceMessenger.Default.Send(new NavegarMensaje(new RegistroBeneficiarioViewModel(_service, _repository)));
        }

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
                    var ventanaRecorte = new Views.RecortarImagenView(bitmap);

                    // Mostramos la ventana de forma modal
                    if (ventanaRecorte.ShowDialog() == true)
                    {
                        // Si el usuario aceptó, obtenemos los bytes del recorte
                        FotoSeleccionada = ventanaRecorte.ImagenRecortadaBytes;

                        if (BeneficiarioActual != null)
                        {
                            BeneficiarioActual.Foto = FotoSeleccionada;

                            // Forzamos la notificación para que la UI se refresque
                            OnPropertyChanged(nameof(BeneficiarioActual));
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar la imagen: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
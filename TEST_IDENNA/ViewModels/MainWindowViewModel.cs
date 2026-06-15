using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Messages;
using TEST_IDENNA.Services;
using TEST_IDENNA.ViewModels;


namespace TEST_IDENNA.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject,
        IRecipient<EnrutarExpedienteExistenteMessage>,
        IRecipient<EnrutarNuevoRegistroMessage>
    {
        /*public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));*/

        private readonly IIntervencionService _service;
        private readonly IBeneficiarioRepository _repository;
        private readonly IEgresoRepository _egreso;
        private readonly ITutorRepository _tutorRepository;
        private readonly IArchivoService _archivoService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly OcrService _ocrService;

        [ObservableProperty]
        private object? _currentView;

        [ObservableProperty]
        private bool _isBusy; // Esta es la propiedad que bindeamos al LoadingOverlay

        [ObservableProperty]
        private string? _nombreUsuario;

        [ObservableProperty]
        private string? _usuarioLogueadoNombre = SesionSistema.UsuarioActual?.NombreUsuario;

        [ObservableProperty]
        private string? _usuarioLogueadoRol = SesionSistema.UsuarioActual?.Rol;

        [ObservableProperty]
        private bool _isDarkMode;

        public bool PermisoAdmin => SesionSistema.EsAdmin;

        // Crea instancias de tus otros ViewModels (asegúrate de tenerlos creados)
        //private readonly DashboardViewModel _dashboardVM = new();
        public MainWindowViewModel(IIntervencionService service, IBeneficiarioRepository repository, IEgresoRepository egresoRepository, ITutorRepository tutorRepository, OcrService ocrService, IArchivoService archivoService, IAuditoriaService auditoriaService)
        {
            _service = service;
            _repository = repository;
            _egreso = egresoRepository;
            _tutorRepository = tutorRepository;
            _ocrService = ocrService;
            _archivoService = archivoService;
            _auditoriaService = auditoriaService;
            CurrentView = new DashboardViewModel(_service);

            WeakReferenceMessenger.Default.Register<NavegarMensaje>(this, (r, m) =>
            {
                CurrentView = m.Value; // El valor es el ViewModel que queremos mostrar
            });
        }

        // Este método se ejecutará automáticamente cada vez que el switch cambie de estado
        partial void OnIsDarkModeChanged(bool value)
        {
            var appDictionaries = App.Current.Resources.MergedDictionaries;

            // 1. Buscamos si ya hay un diccionario de tema cargado para removerlo
            var temaActual = appDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Tema"));
            if (temaActual != null)
            {
                appDictionaries.Remove(temaActual);
            }

            // 2. Definimos la ruta del archivo XAML según el estado del Switch
            string rutaTema = value ? "Themes/TemaOscuro.xaml" : "Themes/TemaClaro.xaml";

            // 3. Instanciamos el diccionario y lo inyectamos en caliente
            var nuevoTema = new ResourceDictionary { Source = new Uri(rutaTema, UriKind.Relative) };
            appDictionaries.Add(nuevoTema);

            WeakReferenceMessenger.Default.Send(new TemaCambiadoMensaje(value));
        }

        // CASO A: El receptor escucha que el expediente YA existe
        public async void Receive(EnrutarExpedienteExistenteMessage message)
        {
            // Pasamos las dependencias correspondientes al constructor (_repository y _service deben estar inyectados en este MainWindowViewModel)
            var expedienteVm = new ExpedientesViewModel(_repository, _service, _auditoriaService);

            // Reutilizamos tu lógica nativa de búsqueda automática usando la cédula extraída
            expedienteVm.SearchText = message.Cedula;
            await expedienteVm.BuscarBeneficiario();

            // Si la búsqueda arrojó al beneficiario, lo seleccionamos de una vez para cargar su historial
            if(expedienteVm.ResultadosBusqueda != null)
            {
                if (expedienteVm.ResultadosBusqueda.Count > 0)
                {
                    await expedienteVm.SeleccionarBeneficiario(expedienteVm.ResultadosBusqueda[0]);
                }
            }

            CurrentView = expedienteVm;
        }

        // CASO B: El receptor escucha que es un beneficiario NUEVO
        public void Receive(EnrutarNuevoRegistroMessage message)
        {
            // ¡Aquí estaba el cruce de cables! Se estaba instanciando ExpedientesViewModel por error.
            // Debes instanciar el ViewModel del formulario de inscripción.
            var registroVm = new RegistroBeneficiarioViewModel(_service, _repository, _auditoriaService);

            // Si tu RegistroBeneficiario tiene una propiedad pública para precargar datos, puedes usarla:
            // registroVm.CedulaPrevia = message.Cedula;

            CurrentView = registroVm;
        }

        // --- MÉTODO CENTRAL DE NAVEGACIÓN CON CARGA ---
        private async Task NavegarAsync(Func<object> factory)
        {
            if (IsBusy) return; // Evita clics dobles mientras carga

            IsBusy = true;

            // Un pequeño retraso de 50-100ms permite que el hilo de la UI 
            // dibuje el Spinner antes de que el procesador se concentre en la BD
            await Task.Yield();// NUEVO
            await Task.Delay(50);

            try
            {
                // Aquí podrías incluso meter un Task.Run si el constructor del VM es muy pesado
                //CurrentView = nuevoViewModel;
                // Ejecutamos la creación del ViewModel en un hilo de fondo si es posible,
                // o al menos dejamos que la UI sepa que estamos ocupados.
                var nuevoVM = await Task.Run(() => factory());
                CurrentView = nuevoVM;
            }
            finally
            {
                await Task.Delay(100);
                IsBusy = false;
            }
        }

        // --- COMANDOS ASÍNCRONOS (RelayCommand de CommunityToolkit) ---

        [RelayCommand]
        private async Task ShowDashboard() => await NavegarAsync(() => new DashboardViewModel(_service));

        [RelayCommand]
        private async Task ShowExpedientes() => await NavegarAsync(() => new ExpedientesViewModel(_repository, _service, _auditoriaService));
        [RelayCommand]
        private async Task ShowBitacora() => await NavegarAsync(() => new BitacoraViewModel(_service, _auditoriaService));

        [RelayCommand]
        private async Task ShowReportes() => await NavegarAsync(() => new ReportesViewModel(_repository, _egreso, _auditoriaService));

        [RelayCommand]
        private async Task ShowAuditorias() => await NavegarAsync(() => new AuditoriasViewModel(_auditoriaService));

        [RelayCommand]
        private async Task ShowGestionTutores() => await NavegarAsync(() => new GestionTutoresViewModel(_tutorRepository, _service, _auditoriaService));

        [RelayCommand]
        private async Task ShowDigitalizarDocumentos() => await NavegarAsync(() => new DigitalizarDocumentosViewModel(_repository, _service, _ocrService, _auditoriaService));

        [RelayCommand]
        private async Task ShowPapelera() => await NavegarAsync(() => new PapeleraViewModel(_service, _auditoriaService));

        [RelayCommand]
        private async Task ShowUsuarios() => await NavegarAsync(() => new UsuariosViewModel(_auditoriaService));
    }

    // Simple RelayCommand implementation used by the ViewModel
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value!;
            remove => CommandManager.RequerySuggested -= value!;
        }
    }
}
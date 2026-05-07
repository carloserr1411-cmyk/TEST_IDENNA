using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Services;
using TEST_IDENNA.ViewModels;
using TEST_IDENNA.Views;


namespace TEST_IDENNA.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private readonly IIntervencionService _service;
        private readonly IBeneficiarioRepository _repository;

        // Crea instancias de tus otros ViewModels (asegúrate de tenerlos creados)
        //private readonly DashboardViewModel _dashboardVM = new();
        public MainWindowViewModel(IIntervencionService service, IBeneficiarioRepository repository)
        {
            _service = service;
            _repository = repository;

            CurrentView = new DashboardViewModel(_service);

            WeakReferenceMessenger.Default.Register<NavegarMensaje>(this, (r, m) =>
            {
                CurrentView = m.Value; // El valor es el ViewModel que queremos mostrar
            });

            ShowDashboard = new RelayCommand(_ =>
            {
                CurrentView = new DashboardViewModel(_service);
            });
            ShowExpedientes = new RelayCommand(_ =>
            {
                //CurrentView = new RegistroBeneficiarioViewModel(_service);
                CurrentView = new ExpedientesViewModel(_repository, _service);
            });
            ShowBitacora = new RelayCommand(_ => 
            {
                CurrentView = new BitacoraViewModel(_service);
            });
            ShowReportes = new RelayCommand(_ => 
            { 
                CurrentView = new ReportesViewModel(_service);
            });
            ShowArchivoHistorico = new RelayCommand(_ => 
            { 
                CurrentView = new ArchivosViewModel(_service); 
            });
        }

        private object? _currentView;
        public object? CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(nameof(CurrentView)); }
        }

        public ICommand ShowDashboard { get; }
        public ICommand ShowExpedientes { get; }
        public ICommand ShowBitacora { get; }
        public ICommand ShowReportes { get; }
        public ICommand ShowArchivoHistorico { get; }

        private string? _nombreUsuario;
        public string? NombreUsuario
        {
            get => _nombreUsuario;
            set { _nombreUsuario = value; OnPropertyChanged(nameof(NombreUsuario)); }
        }
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
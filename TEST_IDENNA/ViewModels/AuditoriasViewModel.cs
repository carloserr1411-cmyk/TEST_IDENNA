using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TEST_IDENNA.Interfaces; // Asegúrate de tener tu interface de auditoría
using TEST_IDENNA.Models;

namespace TEST_IDENNA.ViewModels
{
    public partial class AuditoriasViewModel : ObservableObject
    {
        private readonly IAuditoriaService _auditoriaService;
        private List<AuditoriaMovimiento> _maestraAuditorias = new();

        [ObservableProperty] private ObservableCollection<AuditoriaMovimiento> listaAuditorias = new();

        // Propiedades de Filtro
        [ObservableProperty] private string busquedaTexto = string.Empty;
        [ObservableProperty] private string? moduloSeleccionado;
        [ObservableProperty] private List<string> modulosDisponibles = new();
        [ObservableProperty] private DateTime? fechaInicio = DateTime.Now.AddDays(-7);
        [ObservableProperty] private DateTime? fechaFin = DateTime.Now;

        public AuditoriasViewModel(IAuditoriaService auditoriaService)
        {
            _auditoriaService = auditoriaService;
        }

        [RelayCommand]
        private async Task CargarDatos()
        {
            try
            {
                // 1. Descomentamos y ejecutamos la llamada al servicio
                var datos = await _auditoriaService.ObtenerTodoAsync();

                // 2. Guardamos en la lista maestra ordenando por fecha descendente (lo más nuevo primero)
                _maestraAuditorias = datos?.OrderByDescending(x => x.Fecha).ToList() ?? new List<AuditoriaMovimiento>();

                // 3. Extraemos los módulos únicos que realmente existan en los registros
                var modulosDesdeBd = _maestraAuditorias
                    .Where(x => !string.IsNullOrWhiteSpace(x.Modulo))
                    .Select(x => x.Modulo)
                    .Distinct()
                    .OrderBy(m => m)
                    .ToList();

                // 4. Insertamos la opción por defecto para limpiar el filtro
                modulosDesdeBd.Insert(0, "Todos los módulos");
                ModulosDisponibles = modulosDesdeBd;

                // 5. Dejamos seleccionado "Todos los módulos" por defecto en la primera carga
                if (string.IsNullOrEmpty(ModuloSeleccionado))
                {
                    ModuloSeleccionado = "Todos los módulos";
                }

                // 6. Ejecutamos el método Filtrar para que pinte el DataGrid con la información real
                Filtrar();
            }
            catch (Exception ex)
            {
                // Muestra un mensaje limpio en la interfaz si algo falla con el SQLite
                System.Windows.MessageBox.Show($"Error al cargar el panel de auditoría: {ex.Message}",
                    "Error de Sistema", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        partial void OnBusquedaTextoChanged(string value) => Filtrar();
        partial void OnModuloSeleccionadoChanged(string? value) => Filtrar();
        partial void OnFechaInicioChanged(DateTime? value) => Filtrar();
        partial void OnFechaFinChanged(DateTime? value) => Filtrar();

        private void Filtrar()
        {
            var query = BusquedaTexto.ToLower().Trim();

            var filtrados = _maestraAuditorias.Where(x =>
                (string.IsNullOrEmpty(query) || x.Usuario.ToLower().Contains(query) || x.Detalles.ToLower().Contains(query)) &&
                (ModuloSeleccionado == null || ModuloSeleccionado == "Todos los módulos" || x.Modulo == ModuloSeleccionado) &&
                (!FechaInicio.HasValue || x.Fecha.Date >= FechaInicio.Value.Date) &&
                (!FechaFin.HasValue || x.Fecha.Date <= FechaFin.Value.Date)
            );

            ListaAuditorias = new ObservableCollection<AuditoriaMovimiento>(filtrados);
        }
    }
}
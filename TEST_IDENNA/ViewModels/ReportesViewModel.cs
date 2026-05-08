using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.ObjectModel;
using TEST_IDENNA.Models;
using TEST_IDENNA.Interfaces;

namespace TEST_IDENNA.ViewModels
{
    public partial class ReportesViewModel : ObservableObject
    {
        private readonly IBeneficiarioRepository _repository;
        // 1. Faltaba declarar el repositorio de egresos
        private readonly IEgresoRepository _egresoRepository;

        // Propiedades para los Gráficos (LiveCharts2)
        [ObservableProperty] private ISeries[] _seriesEstatus;
        [ObservableProperty] private ISeries[] _seriesMensual;
        [ObservableProperty] private Axis[] _xAxes;

        // Propiedades para los KPI (Tarjetas de totales)
        [ObservableProperty] private int _totalActivos;
        [ObservableProperty] private int _totalEgresados;
        [ObservableProperty] private double _tasaReunificacion;

        // Lista para el Archivo Histórico
        [ObservableProperty] private ObservableCollection<Beneficiario> _archivoHistorico;

        [ObservableProperty]
        private ObservableCollection<object> _archivoHistoricoCompuesto;

        public ReportesViewModel(IBeneficiarioRepository repository, IEgresoRepository egresoRepository)
        {
            _repository = repository;
            _egresoRepository = egresoRepository;
            CargarDatosAsync();
        }

        [RelayCommand]
        private async Task CargarDatosAsync()
        {
            // 1. Obtenemos todos los beneficiarios y todos los egresos
            var todosLosBeneficiarios = await _repository.ObtenerTodos();
            var todosLosEgresos = await _egresoRepository.ObtenerTodos(); // Nuevo repositorio

            // 2. KPIs Reales
            TotalActivos = todosLosBeneficiarios.Count(b => b.Estatus == "Activo");
            TotalEgresados = todosLosEgresos.Count(); // El conteo histórico real

            // 3. Gráfico de Torta: Motivos de Egreso (Viene de la tabla Egresos)
            SeriesEstatus = todosLosEgresos
                .GroupBy(e => e.Motivo_Egreso)
                .Select(g => new PieSeries<int>
                {
                    Values = new[] { g.Count() },
                    Name = g.Key
                }).ToArray();

            // 4. Archivo Histórico: Cruzamos datos (JOIN)
            // Queremos ver el nombre del niño (de Beneficiarios) y por qué salió (de Egresos)
            var historicoCompuesto = from egreso in todosLosEgresos
                                     join bene in todosLosBeneficiarios on egreso.Id_Beneficiario equals bene.Id_Beneficiario
                                     select new
                                     {
                                         Nombre = $"{bene.Nombres} {bene.Apellidos}",
                                         bene.Cedula,
                                         egreso.Fecha_Salida,
                                         egreso.Motivo_Egreso
                                     };

            // Actualizamos la colección para la tabla de la interfaz
            ArchivoHistoricoCompuesto = new ObservableCollection<object>(historicoCompuesto);
        }

        [RelayCommand]
        private void ExportarPDF()
        {
            // Aquí irá la lógica de QuestPDF
            // var reporte = new MiReporteTecnico(ArchivoHistorico);
            // reporte.GeneratePdf("Reporte_Idenna.pdf");
        }
    }
}
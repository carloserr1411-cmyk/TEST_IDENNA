using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Messages;
using TEST_IDENNA.Models;
using TEST_IDENNA.Services;

//QuestPDF.Settings.License = LicenseType.Evaluation;

namespace TEST_IDENNA.ViewModels
{
    // DTO Estructurado para el DataGrid del Histórico (Evita problemas de binding con tipos anónimos en WPF)
    public class RegistroHistoricoDTO
    {
        public string Nombre { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public DateTime Fecha_Egreso { get; set; }
        public string Motivo_Egreso { get; set; } = string.Empty;
    }

    public partial class ReportesViewModel : ObservableObject
    {
        private readonly IBeneficiarioRepository _repository;
        private readonly IEgresoRepository _egresoRepository;
        private readonly IAuditoriaService _auditoriaService;

        private bool _esModoOscuroActual;

        // Propiedades para los Gráficos (LiveCharts2)
        [ObservableProperty] private ISeries[] _seriesEstatus;
        [ObservableProperty] private ISeries[] _seriesMensual;
        [ObservableProperty] private Axis[] _xAxes;

        // Propiedades para los KPI (Tarjetas de totales)
        [ObservableProperty] private int _totalActivos;
        [ObservableProperty] private int _totalEgresados;
        [ObservableProperty] private int _totalPendientes;
        [ObservableProperty] private double _tasaReunificacion;

        [ObservableProperty]
        private bool _graficosExpandidos = false;

        // Propiedades para almacenar las fechas del filtro
        [ObservableProperty] private DateTime? _fechaDesde;
        [ObservableProperty] private DateTime? _fechaHasta;

        // Listas para el Archivo Histórico
        [ObservableProperty] private ObservableCollection<Beneficiario> _archivoHistorico;
        [ObservableProperty] private ObservableCollection<object> _archivoHistoricoCompuesto;

        public ReportesViewModel(IBeneficiarioRepository repository, IEgresoRepository egresoRepository, IAuditoriaService auditoriaService)
        {
            _repository = repository;
            _egresoRepository = egresoRepository;
            _auditoriaService = auditoriaService;

            WeakReferenceMessenger.Default.Register<TemaCambiadoMensaje>(this, (r, m) =>
            {
                _esModoOscuroActual = m.Value;
                ActualizarColoresGraficas(m.Value);
            });
        }

        public void ActualizarColoresGraficas(bool esModoOscuro)
        {
            var colorTexto = esModoOscuro ? SKColor.Parse("#F4F6F9") : SKColor.Parse("#1E293B");
            var colorLineasSeparadoras = esModoOscuro ? SKColor.Parse("#3A3A44") : SKColor.Parse("#E2E8F0");

            // Configurar el eje X con los meses correspondientes
            XAxes = new Axis[]
            {
                new() {
                    Name = $"Flujo de Casos ({DateTime.Now.Year})",
                    Labels = new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" },
                    NamePaint = new SolidColorPaint(colorTexto),
                    LabelsPaint = new SolidColorPaint(colorTexto),
                    SeparatorsPaint = new SolidColorPaint(colorLineasSeparadoras) { StrokeThickness = 1 }
                }
            };

            // Aplicar colores reactivos al gráfico de torta (Motivos de Egreso)
            if (SeriesEstatus != null)
            {
                foreach (var serie in SeriesEstatus)
                {
                    serie.DataLabelsPaint = new SolidColorPaint(colorTexto);
                }
            }

            // Aplicar colores reactivos al gráfico de líneas/barras mensual
            if (SeriesMensual != null)
            {
                foreach (var serie in SeriesMensual)
                {
                    serie.DataLabelsPaint = new SolidColorPaint(colorTexto);
                }
            }
        }

        [RelayCommand]
        private async Task CargarDatosAsync()
        {
            // 1. Obtener los universos de datos desde los repositorios SQLite
            var todosLosBeneficiarios = await _repository.ObtenerTodos();
            var todosLosEgresos = await _egresoRepository.ObtenerTodos();

            var beneficiariosFiltrados = todosLosBeneficiarios.AsEnumerable();
            var egresosFiltrados = todosLosEgresos.AsEnumerable();

            if (FechaDesde.HasValue)
            {
                beneficiariosFiltrados = beneficiariosFiltrados.Where(b => b.Fecha_Ingreso >= FechaDesde.Value);
                egresosFiltrados = egresosFiltrados.Where(e => e.Fecha_Salida >= FechaDesde.Value);
            }

            if (FechaHasta.HasValue)
            {
                // .Date.AddDays(1).AddTicks(-1) asegura que incluya todo el día límite hasta las 11:59:59 PM
                var fechaLimite = FechaHasta.Value.Date.AddDays(1).AddTicks(-1);
                beneficiariosFiltrados = beneficiariosFiltrados.Where(b => b.Fecha_Ingreso <= fechaLimite);
                egresosFiltrados = egresosFiltrados.Where(e => e.Fecha_Salida <= fechaLimite);
            }

            // 2. Mapear Contadores Globales (KPIs)
            TotalActivos = beneficiariosFiltrados.Count(b => b.Estatus == "Activo");
            TotalEgresados = egresosFiltrados.Count();
            TotalPendientes = beneficiariosFiltrados.Count(b =>
                b.Estatus_Legal != null &&
                b.Estatus_Legal.Equals("Pendiente", StringComparison.OrdinalIgnoreCase));

            // 3. Calcular la Tasa de Reunificación Familiar (Indicador de impacto clave del IDENNA)
            if (TotalEgresados > 0)
            {
                var reunificaciones = egresosFiltrados.Count(e => e.Motivo_Egreso != null &&
                                      e.Motivo_Egreso.Contains("Reunificación Familiar", StringComparison.OrdinalIgnoreCase));
                TasaReunificacion = Math.Round((double)reunificaciones / TotalEgresados * 100, 1);
            }
            else
            {
                TasaReunificacion = 0;
            }

            // 4. Gráfico de Torta (Distribución por Motivos de Egreso Jurídico)
            SeriesEstatus = egresosFiltrados
                .GroupBy(e => e.Motivo_Egreso)
                .Select(g => new PieSeries<int>
                {
                    Values = new[] { g.Count() },
                    Name = g.Key ?? "No Especificado",
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle
                }).ToArray();

            // 5. 🔥 IMPLEMENTACIÓN: SeriesMensual (Evolución Temporal del Año Actual)
            int anioActual = DateTime.Now.Year;

            var ingresosDistribuidos = Enumerable.Range(1, 12)
                .Select(mes => beneficiariosFiltrados.Count(b => b.Fecha_Ingreso.Year == anioActual && b.Fecha_Ingreso.Month == mes))
                .ToArray();

            var egresosDistribuidos = Enumerable.Range(1, 12)
                .Select(mes => egresosFiltrados.Count(e => e.Fecha_Salida.Year == anioActual && e.Fecha_Salida.Month == mes))
                .ToArray();

            SeriesMensual =
            [
                new LineSeries<int>
                {
                    Values = ingresosDistribuidos,
                    Name = "Ingresos (Casos Nuevos)",
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = 3 },
                    GeometryFill = new SolidColorPaint(SKColors.CornflowerBlue),
                    GeometrySize = 8
                },
                new LineSeries<int>
                {
                    Values = egresosDistribuidos,
                    Name = "Egresos (Casos Cerrados)",
                    Stroke = new SolidColorPaint(SKColors.Tomato) { StrokeThickness = 3 },
                    GeometryFill = new SolidColorPaint(SKColors.Tomato),
                    GeometrySize = 8
                }
            ];

            // 6. 🔥 IMPLEMENTACIÓN: Archivo Histórico Compuesto (Modelado con un DTO limpio)
            var historicoCompuesto = from egreso in egresosFiltrados
                                     join bene in beneficiariosFiltrados on egreso.Id_Beneficiario equals bene.Id_Beneficiario
                                     orderby egreso.Fecha_Salida descending
                                     select new RegistroHistoricoDTO
                                     {
                                         Nombre = $"{bene.Nombres} {bene.Apellidos}",
                                         Cedula = bene.Cedula,
                                         Fecha_Egreso = egreso.Fecha_Salida, // Viene de Egreso.cs
                                         Motivo_Egreso = egreso.Motivo_Egreso ?? "Sin especificar" // Viene de Egreso.cs
                                     };

            ArchivoHistoricoCompuesto = new ObservableCollection<object>(historicoCompuesto.Cast<object>());

            // Opcional: Si tu vista usa la lista plana de modelos crudos de beneficiarios egresados:
            var idsEgresados = todosLosEgresos.Select(e => e.Id_Beneficiario).ToHashSet();
            ArchivoHistorico = new ObservableCollection<Beneficiario>(beneficiariosFiltrados.Where(b => idsEgresados.Contains(b.Id_Beneficiario)));

            // 7. Sincronizar colores finales según el tema visual activo
            ActualizarColoresGraficas(_esModoOscuroActual);
        }

        [RelayCommand]
        private async Task ExportarPDF()
        {
            // 1. Validar que tengamos datos para exportar
            if (ArchivoHistoricoCompuesto == null || !ArchivoHistoricoCompuesto.Any())
            {
                // Aquí podrías notificar al usuario si la lista está vacía
                return;
            }

            // 2. Configurar el diálogo para guardar el archivo
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Documento PDF (*.pdf)|*.pdf",
                FileName = $"Reporte_Gestion_{DateTime.Now:yyyyMMdd}",
                Title = "Guardar Reporte Estadístico"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string rutaArchivo = saveFileDialog.FileName;

                try
                {
                    // 3. Crear el documento PDF con diseño fluido
                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            // Configuración de página estándar
                            page.Size(PageSizes.A4);
                            page.Margin(2, Unit.Centimetre);
                            page.PageColor(Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                            // --- ENCABEZADO ---
                            page.Header().Column(column =>
                            {
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("IDENNA").Bold().FontSize(18).FontColor(Colors.Red.Medium);
                                        col.Item().Text("Instituto Autónomo Consejo Nacional de Derecho de Niños, Niñas y Adolescentes").FontSize(10).Italic();
                                    });

                                    row.ConstantItem(100).AlignRight().Column(col =>
                                    {
                                        col.Item().Text($"Fecha: {DateTime.Now:dd/MM/yyyy}").FontSize(10);
                                        col.Item().Text($"Hora: {DateTime.Now:hh:mm tt}").FontSize(10);
                                    });
                                });

                                column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                            });

                            // --- CONTENIDO PRINCIPAL ---
                            page.Content().Column(column =>
                            {
                                // Título del Reporte
                                column.Item().PaddingBottom(15).Text("REPORTE ESTADÍSTICO Y HISTÓRICO").Bold().FontSize(14).AlignCenter();

                                // Mostrar rango de fechas si existen filtros activos
                                if (FechaDesde.HasValue || FechaHasta.HasValue)
                                {
                                    string filtroTexto = "Filtros aplicados: ";
                                    if (FechaDesde.HasValue) filtroTexto += $"Desde: {FechaDesde.Value:dd/MM/yyyy} ";
                                    if (FechaHasta.HasValue) filtroTexto += $"Hasta: {FechaHasta.Value:dd/MM/yyyy}";

                                    column.Item().PaddingBottom(10).Background(Colors.Grey.Lighten3).Padding(5)
                                          .Text(filtroTexto).FontSize(10).Italic().AlignCenter();
                                }

                                // --- SECCIÓN 1: BLOQUE DE INDICADORES (KPIs) ---
                                column.Item().PaddingBottom(5).Text("Resumen Cuantitativo").Bold().FontSize(12);
                                column.Item().PaddingBottom(20).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    // Diseñar las "Tarjetas" como celdas estructuradas
                                    table.Cell().Background(Colors.Blue.Lighten5).Border(1).BorderColor(Colors.Blue.Lighten3).Padding(8).Column(c =>
                                    {
                                        c.Item().AlignCenter().Text("CASOS ACTIVOS").FontSize(9).Bold().FontColor(Colors.Blue.Darken3);
                                        c.Item().AlignCenter().Text(TotalActivos.ToString()).FontSize(16).Bold();
                                    });

                                    table.Cell().Background(Colors.Green.Lighten5).Border(1).BorderColor(Colors.Green.Lighten3).Padding(8).Column(c =>
                                    {
                                        c.Item().AlignCenter().Text("EGRESOS TOTALES").FontSize(9).Bold().FontColor(Colors.Green.Darken3);
                                        c.Item().AlignCenter().Text(TotalEgresados.ToString()).FontSize(16).Bold();
                                    });

                                    table.Cell().Background(Colors.Amber.Lighten5).Border(1).BorderColor(Colors.Amber.Lighten3).Padding(8).Column(c =>
                                    {
                                        c.Item().AlignCenter().Text("PENDIENTES").FontSize(9).Bold().FontColor(Colors.Amber.Darken3);
                                        c.Item().AlignCenter().Text(TotalPendientes.ToString()).FontSize(16).Bold();
                                    });

                                    table.Cell().Background(Colors.Purple.Lighten5).Border(1).BorderColor(Colors.Purple.Lighten3).Padding(8).Column(c =>
                                    {
                                        c.Item().AlignCenter().Text("REUNIFICACIÓN").FontSize(9).Bold().FontColor(Colors.Purple.Darken3);
                                        c.Item().AlignCenter().Text($"{TasaReunificacion}%").FontSize(16).Bold();
                                    });
                                });

                                // --- SECCIÓN 2: TABLA DETALLADA (HISTÓRICO) ---
                                column.Item().PaddingBottom(5).Text("Listado Histórico de Egresos").Bold().FontSize(12);

                                column.Item().Table(table =>
                                {
                                    // Definir proporciones de columnas (idéntico al DataGrid de tu XAML)
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);  // Beneficiario
                                        columns.RelativeColumn(1.5f); // Cédula
                                        columns.RelativeColumn(1.5f); // Fecha Egreso
                                        columns.RelativeColumn(4);  // Motivo
                                    });

                                    // Cabecera de la Tabla
                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Grey.Darken2).Padding(5).Text("Beneficiario").Bold().FontColor(Colors.White);
                                        header.Cell().Background(Colors.Grey.Darken2).Padding(5).Text("Cédula").Bold().FontColor(Colors.White);
                                        header.Cell().Background(Colors.Grey.Darken2).Padding(5).Text("Fecha Egreso").Bold().FontColor(Colors.White);
                                        header.Cell().Background(Colors.Grey.Darken2).Padding(5).Text("Motivo de Egreso").Bold().FontColor(Colors.White);
                                    });

                                    // Iterar sobre los datos filtrados actuales de la UI
                                    int filaIndex = 0;
                                    foreach (var objeto in ArchivoHistoricoCompuesto)
                                    {
                                        if (objeto is RegistroHistoricoDTO registro)
                                        {
                                            // Alternar fondo para legibilidad
                                            var colorFondo = filaIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                                            table.Cell().Background(colorFondo).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(registro.Nombre);
                                            table.Cell().Background(colorFondo).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(registro.Cedula);
                                            table.Cell().Background(colorFondo).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(registro.Fecha_Egreso.ToString("dd/MM/yyyy"));
                                            table.Cell().Background(colorFondo).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(registro.Motivo_Egreso);

                                            filaIndex++;
                                        }
                                    }
                                });
                            });

                            // --- PIE DE PÁGINA ---
                            page.Footer().AlignCenter().Row(row =>
                            {
                                row.RelativeItem().Text("Documento generado automáticamente por el Sistema IDENNA.").FontSize(9).FontColor(Colors.Grey.Medium);
                                row.RelativeItem().AlignRight().Text(text =>
                                {
                                    text.Span("Página ").FontSize(9).FontColor(Colors.Grey.Medium);
                                    text.CurrentPageNumber().FontSize(9).Bold();
                                    text.Span(" de ").FontSize(9).FontColor(Colors.Grey.Medium);
                                    text.TotalPages().FontSize(9).Bold();
                                });
                            });
                        });
                    }).GeneratePdf(rutaArchivo);

                    await _auditoriaService.RegistrarAccionAsync(
                accion: "CREAR",
                modulo: "Reportes",
                detalles: $"Se exportó el PDF de reportes el dia {DateTime.Now:dd/MM/yyyy}"
            );

                    // Opcional: Podrías abrir el PDF automáticamente al terminar si lo deseas:
                    // System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(rutaArchivo) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    // Manejo de errores (por ejemplo, si el archivo está abierto en otra parte)
                    System.Windows.MessageBox.Show($"Error al generar el PDF: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using TEST_IDENNA.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using TEST_IDENNA.Data;

namespace TEST_IDENNA.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly IIntervencionService _service;

        // Propiedades de las Tarjetas Superiores (KPIs)
        [ObservableProperty]
        private int _totalExpedientes;

        [ObservableProperty]
        private int _atencionesHoy;

        [ObservableProperty]
        private int _alertasPendientes;

        [ObservableProperty]
        private int _alertasSinTelefono;

        [ObservableProperty]
        private int _alertasSinNombre;

        [ObservableProperty]
        private int _alertasSinUbicacion;

        [ObservableProperty]
        private double _porcentajeSeguimiento;

        [ObservableProperty]
        private double _porcentajeConcluidos;

        // Lista que alimenta el control de expedientes recientes
        public ObservableCollection<BeneficiarioResumenDTO> UltimosExpedientes { get; set; } = new();

        public DashboardViewModel(IIntervencionService service)
        {
            // Inicializar carga de datos de manera asíncrona de inmediato
            _ = CargarDatosDashboardAsync();
            _service = service;
        }

        /// <summary>
        /// Realiza las consultas LINQ en la base de datos para actualizar las métricas del panel.
        /// </summary>
        [RelayCommand]
        private async Task CargarDatosDashboardAsync()
        {
            try
            {
                using (var context = new AppDbContext()) // Tu DbContext de EF Core
                {
                    // 1. Calcular el total de expedientes registrados
                    int total = await context.Beneficiarios.CountAsync();
                    TotalExpedientes = total;

                    // Agrupamos o contamos los IDs únicos de beneficiarios egresados
                    int concluidos = await context.Beneficiarios
                        .Where(b => b.Estatus_Legal == "Egresado")
                        .CountAsync();

                    int seguimiento = total - concluidos;

                    if (total > 0)
                    {
                        // Math.Round para que no salgan demasiados decimales en la UI
                        PorcentajeSeguimiento = Math.Round((double)seguimiento / total * 100, 1);
                        PorcentajeConcluidos = Math.Round((double)concluidos / total * 100, 1);
                    }
                    else
                    {
                        PorcentajeSeguimiento = 0;
                        PorcentajeConcluidos = 0;
                    }

                    // 2. Contar atenciones/evoluciones realizadas el día de hoy
                    DateTime hoy = DateTime.Today;
                    AtencionesHoy = await context.Evoluciones
                        .Where(e => e.Fecha_Registro >= hoy)
                        .CountAsync();

                    // 3. Contar alertas críticas (Ej: Representantes sin teléfono o campos obligatorios vacíos)
                    AlertasSinTelefono = await context.Beneficiarios
                        .CountAsync(b => string.IsNullOrEmpty(b.TelefonoContacto));

                    AlertasSinNombre = await context.Beneficiarios
                        .CountAsync(b => string.IsNullOrEmpty(b.NombreContacto));

                    AlertasSinUbicacion = await context.Beneficiarios
                        .CountAsync(b => string.IsNullOrEmpty(b.UbicacionFisica));

                    AlertasPendientes = AlertasSinNombre + AlertasSinTelefono + AlertasSinUbicacion;

                    // 4. Obtener los últimos 5 expedientes modificados o agregados
                    var recientes = await context.Beneficiarios
                        .OrderByDescending(b => b.FechaModificacion) // Asegúrate de tener este campo en tu BD
                        .Take(5)
                        .Select(b => new BeneficiarioResumenDTO
                        {
                            NombreCompleto = $"{b.Nombres} {b.Apellidos}",
                            IdentificadorExpediente = $"EXP-{b.Id_Beneficiario:D5}", // Formato: EXP-00024
                            FechaModificacion = b.FechaModificacion ?? DateTime.Now
                        })
                        .ToListAsync();

                    // Actualizar la colección en la UI de forma limpia
                    UltimosExpedientes.Clear();
                    foreach (var item in recientes)
                    {
                        UltimosExpedientes.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejar excepciones de conexión a base de datos de manera silenciosa o con logs
                System.Diagnostics.Debug.WriteLine($"Error al cargar Dashboard: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Estructura de datos simple (DTO) para optimizar la consulta de actividad reciente
    /// </summary>
    public class BeneficiarioResumenDTO
    {
        public string NombreCompleto { get; set; }
        public string IdentificadorExpediente { get; set; }
        public DateTime FechaModificacion { get; set; }
    }
}

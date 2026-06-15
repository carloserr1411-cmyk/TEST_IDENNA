using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Models;
using TEST_IDENNA.Services;

namespace TEST_IDENNA.ViewModels
{
    public partial class RegistroBeneficiarioViewModel(IIntervencionService service, IBeneficiarioRepository repository, IAuditoriaService auditoriaService) : ObservableObject
    {
        private readonly IIntervencionService _service = service;
        private readonly IBeneficiarioRepository _repository = repository;
        private readonly IAuditoriaService _auditoriaService = auditoriaService;
        private bool _esEdicion = false;

        [ObservableProperty]
        private string _tituloVista = "Registrar Nuevo Beneficiario";

        [ObservableProperty]
        private string _botonGuardarTexto = "REGISTRAR BENEFICIARIO";

        [ObservableProperty]
        private Beneficiario _modeloFormulario = GenerarNuevoBeneficiario();

        [ObservableProperty]
        private byte[]? _fotoSeleccionada;

        [ObservableProperty]
        private bool _esEgresadoOriginal;

        partial void OnModeloFormularioChanged(Beneficiario value)
        {
            if (value != null && !string.IsNullOrWhiteSpace(value.Estatus_Legal))
            {
                EsEgresadoOriginal = value.Estatus_Legal.Trim().Equals("Egresado", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                EsEgresadoOriginal = false;
            }
        }

        public void CargarBeneficiario(Beneficiario beneficiarioAEditar)
        {
            ModeloFormulario = beneficiarioAEditar;
            FotoSeleccionada = beneficiarioAEditar.Foto;
            _esEdicion = true;
            TituloVista = "Editar Datos del Beneficiario";
            BotonGuardarTexto = "GUARDAR CAMBIOS";

            EsEgresadoOriginal = !string.IsNullOrEmpty(ModeloFormulario.Estatus_Legal) &&
                                 ModeloFormulario.Estatus_Legal.Equals("Egresado", StringComparison.OrdinalIgnoreCase);
        }

        private static Beneficiario GenerarNuevoBeneficiario() => new()
        {
            Fecha_Nacimiento = DateTime.Today.AddYears(-12),
            Fecha_Ingreso = DateTime.Now,
            Estatus_Legal = "En Proceso de Investigación",
            Estatus = "Activo",
            Observaciones = string.Empty,
            Estatus_Color = "#27AE60"
        };

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("BeneficiarioParaEditar", out object? value))
            {
                ModeloFormulario = (Beneficiario)value;
                FotoSeleccionada = ModeloFormulario.Foto;
                TituloVista = "Editar Datos del Beneficiario";
                BotonGuardarTexto = "GUARDAR CAMBIOS";
                _esEdicion = true;
            }
            else
            {
                ModeloFormulario = GenerarNuevoBeneficiario(); // Mantiene consistencia de inicialización
                _esEdicion = false;
            }
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
                    BitmapImage bitmap = new();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    var ventanaRecorte = new Views.RecortarImagenView(bitmap);

                    if (ventanaRecorte.ShowDialog() == true)
                    {
                        FotoSeleccionada = ventanaRecorte.ImagenRecortadaBytes;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar la imagen: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Realiza la validación exhaustiva de los datos antes de operar en la BD
        /// </summary>
        private async Task<string?> ValidarFormularioAsync()
        {
            if (ModeloFormulario == null) return "El formulario no contiene datos.";

            // 1. Sanitización preliminar de campos de texto
            ModeloFormulario.Cedula = ModeloFormulario.Cedula?.Trim() ?? string.Empty;
            ModeloFormulario.Nombres = ModeloFormulario.Nombres?.Trim() ?? string.Empty;
            ModeloFormulario.Apellidos = ModeloFormulario.Apellidos?.Trim() ?? string.Empty;

            // 2. Validar Campos Requeridos
            if (string.IsNullOrWhiteSpace(ModeloFormulario.Cedula)) return "La cédula es obligatoria.";
            if (string.IsNullOrWhiteSpace(ModeloFormulario.Nombres)) return "El nombre es obligatorio.";
            if (string.IsNullOrWhiteSpace(ModeloFormulario.Apellidos)) return "El apellido es obligatorio.";

            // 3. Validar Formato de Cédula (Solo números, ej: Venezolana estándar entre 6 y 9 dígitos)
            if (!Regex.IsMatch(ModeloFormulario.Cedula, @"^\d{6,9}$"))
            {
                return "La cédula debe contener únicamente números y tener una longitud válida (entre 6 y 9 dígitos).";
            }

            // 4. Validar Coherencia de Fechas
            if (ModeloFormulario.Fecha_Nacimiento > DateTime.Today)
            {
                return "La fecha de nacimiento no puede ser una fecha futura.";
            }
            if (ModeloFormulario.Fecha_Nacimiento > DateTime.Today.AddYears(-1))
            {
                return "Verifique la fecha de nacimiento. El beneficiario debe tener al menos 1 año de edad.";
            }
            if (ModeloFormulario.Fecha_Ingreso > DateTime.Now.AddMinutes(5)) // Margen por desfase de reloj
            {
                return "La fecha de ingreso institucional no puede ser una fecha futura.";
            }

            // 5. CONTROL CRÍTICO: Duplicados de Cédula en Base de Datos
            var existente = await _repository.ObtenerPorCedulaAsync(ModeloFormulario.Cedula);
            if (existente != null)
            {
                if (!_esEdicion)
                {
                    // En modo creación: Si ya existe la cédula, es un duplicado prohibido
                    return $"Ya existe un beneficiario registrado con la cédula N° {ModeloFormulario.Cedula} " +
                           $"({existente.Nombres} {existente.Apellidos}).";
                }
                else
                {
                    // En modo edición: Si existe, pero el ID es diferente, están intentando cambiar la cédula por la de OTRA persona
                    if (existente.Id_Beneficiario != ModeloFormulario.Id_Beneficiario)
                    {
                        return $"No se puede modificar la cédula a N° {ModeloFormulario.Cedula} porque ya pertenece a otro beneficiario activo.";
                    }
                }
            }

            return null; // Pasó todas las validaciones sólidamente
        }

        [RelayCommand]
        private async Task Guardar()
        {
            // Validación 0: Restricción legal de Egreso Manual
            if (!EsEgresadoOriginal && !string.IsNullOrWhiteSpace(ModeloFormulario.Estatus_Legal) &&
                ModeloFormulario.Estatus_Legal.Trim().Equals("Egresado", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "El estatus 'Egresado' no se puede asignar de forma manual escribiendo en este campo.\n\n" +
                    "El egreso institucional es un proceso legal que requiere registrar información obligatoria " +
                    "(Fecha de salida, Motivo jurídico y Observaciones).\n\n" +
                    "Para egresar a este beneficiario, por favor vuelva a la pantalla del expediente y utilice el botón verde 'Registrar Egreso'.",
                    "Operación No Permitida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // CORRECCIÓN: Ejecución de las validaciones sólidas antes de tocar la BD
            string? errorValidacion = await ValidarFormularioAsync();
            if (errorValidacion != null)
            {
                MessageBox.Show(errorValidacion, "Validación de Datos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // Detiene la operación completamente
            }

            // Asignamos la foto al modelo de forma segura
            ModeloFormulario.Foto = FotoSeleccionada;

            try
            {
                if (_esEdicion)
                {
                    await _repository.Actualizar(ModeloFormulario);

                    await _auditoriaService.RegistrarAccionAsync(
                accion: "MODIFICAR",
                modulo: "Expedientes (Archivo Físico)",
                detalles: $"Se actualizó la información del beneficiario {ModeloFormulario.NombreCompleto} (Cédula: {ModeloFormulario.Cedula})"
            );

                    var dialog = new Views.Dialogs.ExitoDialog("Beneficiario actualizado con éxito");
                    dialog.ShowDialog();

                    var expedientesVM = new ExpedientesViewModel(_repository, _service, _auditoriaService);
                    await expedientesVM.InicializarConBeneficiarioAsync(ModeloFormulario);
                    WeakReferenceMessenger.Default.Send(new NavegarMensaje(expedientesVM));
                }
                else
                {
                    ModeloFormulario.Estatus = "Activo";
                    bool exito = await _service.RegistrarNuevoIngreso(ModeloFormulario);

                    if (exito)
                    {
                        await _auditoriaService.RegistrarAccionAsync(
                accion: "REGISTRAR BENEFICIARIO",
                modulo: "Expedientes (Archivo Físico)",
                detalles: $"Se registró al beneficiario {ModeloFormulario.NombreCompleto} (Cédula: {ModeloFormulario.Cedula})"
            );
                        new Views.Dialogs.ExitoDialog("Beneficiario registrado con éxito").ShowDialog();
                        Volver();
                    }
                    else
                    {
                        MessageBox.Show("Error de consistencia al registrar en la base de datos", "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error inesperado al procesar la solicitud: {ex.Message}", "Error de Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task Cancelar()
        {
            if (_esEdicion)
            {
                var beneficiarioLimpio = await _repository.ObtenerPorCedulaAsync(ModeloFormulario.Cedula);
                var beneficiarioAAsignar = beneficiarioLimpio ?? ModeloFormulario;

                var expedientesVM = new ExpedientesViewModel(_repository, _service, _auditoriaService);
                await expedientesVM.InicializarConBeneficiarioAsync(beneficiarioAAsignar);
                WeakReferenceMessenger.Default.Send(new NavegarMensaje(expedientesVM));
            }
            else
            {
                Volver();
            }
        }

        [RelayCommand]
        public void Volver()
        {
            WeakReferenceMessenger.Default.Send(new NavegarMensaje(new ExpedientesViewModel(_repository, _service, _auditoriaService)));
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using TEST_IDENNA.Data;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Models;
using TEST_IDENNA.Services;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace TEST_IDENNA.ViewModels
{
    // =================================================================
    // MENSAJE PERSONALIZADO PARA NOTIFICAR ACTUALIZACIONES
    // =================================================================
    public class BeneficiarioActualizadoMensaje : CommunityToolkit.Mvvm.Messaging.Messages.ValueChangedMessage<Beneficiario>
    {
        public BeneficiarioActualizadoMensaje(Beneficiario beneficiario) : base(beneficiario) { }
    }

    public partial class ExpedientesViewModel : ObservableObject
    {
        private readonly IBeneficiarioRepository _repository;
        private readonly IIntervencionService _service;
        private readonly IAuditoriaService _auditoriaService;

        [ObservableProperty]
        private ObservableCollection<DocumentoAdjunto>? _listaDocumentos = new();

        [ObservableProperty]
        private bool _isOcrProcesando;

        [ObservableProperty] private ObservableCollection<Actividad>? _actividadesDisponibles = new();
        [ObservableProperty] private ObservableCollection<Tutores>? _tutoresDisponibles = new();
        [ObservableProperty] private DateTime _fechaSeleccionada = DateTime.Now;
        [ObservableProperty] private Actividad? _actividadSeleccionada;
        [ObservableProperty] private Tutores? _especialistaSeleccionado;
        [ObservableProperty] private string? _nuevaEvolucionTexto = string.Empty;
        [ObservableProperty] private string? _searchText;
        [ObservableProperty] private bool? _isEvolucionDialogOpen;
        [ObservableProperty] private ObservableCollection<Beneficiario>? _resultadosBusqueda = new();
        [ObservableProperty] private Beneficiario? _beneficiarioActual;
        [ObservableProperty] private ObservableCollection<Evolucion>? _listaEvoluciones = new();
        [ObservableProperty] private byte[]? _fotoSeleccionada;
        [ObservableProperty] private bool _isEditEvolucionVisible;
        [ObservableProperty] private string _descripcionEdit;
        [ObservableProperty] private Tutores? _tutorSeleccionadoEdit;
        [ObservableProperty] private DateTime? _fechaSeleccionadaEdit;
        [ObservableProperty] private Actividad? _actividadSeleccionadaEdit;
        [ObservableProperty] private bool _isEditarContactoDialogOpen;
        [ObservableProperty] private string? _nombreContactoEdit;
        [ObservableProperty] private string? _parentescoContactoEdit;
        [ObservableProperty] private string? _telefonoContactoEdit;
        [ObservableProperty] private bool isLocationDialogOpen;
        [ObservableProperty] private Beneficiario? expedienteSeleccionado;
        [ObservableProperty] private string nuevaUbicacion = string.Empty;

        public bool PermisoAdmin => SesionSistema.EsAdmin;

        public ObservableCollection<Tutores> TutoresActivos { get; set; } = new();
        private Evolucion _evolucionEnEdicion;
        public bool HayResultados => ResultadosBusqueda.Count > 0;

        public ExpedientesViewModel(IBeneficiarioRepository repository, IIntervencionService service, IAuditoriaService auditoriaService)
        {
            _repository = repository;
            _service = service;
            _auditoriaService = auditoriaService;

            // 🔥 SOLUCIÓN A: Registrar receptor si el ViewModel sigue vivo en memoria
            WeakReferenceMessenger.Default.Register<BeneficiarioActualizadoMensaje>(this, async (r, m) =>
            {
                if (m.Value != null)
                {
                    await InicializarConBeneficiarioAsync(m.Value);
                }
            });
        }

        [RelayCommand]
        private async Task GuardarUbicacion()
        {
            if (ExpedienteSeleccionado == null) return;

            try
            {
                string ubicacionFinal = string.IsNullOrWhiteSpace(NuevaUbicacion) ? "No asignada" : NuevaUbicacion.Trim();

                // TODO: Aquí llamarías a tu servicio para persistir el cambio en SQLite mediante tu BeneficiarioService
                await _repository.ActualizarUbicacionAsync(ExpedienteSeleccionado.Id_Beneficiario, ubicacionFinal);

                // Actualizamos la lista
                //await CargarDatos(); // Reaplicamos el filtro para actualizar la vista

                await _auditoriaService.RegistrarAccionAsync(
                accion: "MODIFICAR",
                modulo: "Expedientes (Archivo Físico)",
                detalles: $"Se cambió la ubicación del beneficiario {BeneficiarioActual.NombreCompleto} (Cédula: {BeneficiarioActual.Cedula}) a: {NuevaUbicacion}"
            );

                OnPropertyChanged(nameof(BeneficiarioActual));

                // Cerramos el diálogo
                IsLocationDialogOpen = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo guardar la nueva ubicación: {ex.Message}", "Error al Guardar", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Comando para el botón CANCELAR del diálogo
        [RelayCommand]
        private void CancelarEdicionUbicacion()
        {
            // Simplemente cerramos el Dialog sin alterar nada
            IsLocationDialogOpen = false;
            ExpedienteSeleccionado = null;
            NuevaUbicacion = string.Empty;
        }

        [RelayCommand]
        private void EditarUbicacion(Beneficiario beneficiario)
        {
            if (beneficiario == null) return;

            // 1. Asignamos el renglón seleccionado a nuestra propiedad de seguimiento
            ExpedienteSeleccionado = beneficiario;

            // 2. Precargamos el TextBox con la ubicación actual (si dice "No asignada", lo dejamos en blanco para comodidad)
            NuevaUbicacion = beneficiario.UbicacionFisica == "No asignada" ? string.Empty : beneficiario.UbicacionFisica;

            // 3. Abrimos el diálogo de Material Design
            IsLocationDialogOpen = true;
        }

        [RelayCommand]
        private async Task EgresarBeneficiario(Beneficiario beneficiario)
        {
            if (beneficiario == null) return;

            // 1. Validar si ya está egresado para evitar reprocesar
            if (beneficiario.Estatus_Legal == "Egresado")
            {
                // Aquí podrías lanzar un Snackbar o mensaje notificando que ya está egresado
                return;
            }

            // 2. Control de seguridad: Confirmar la acción
            // Nota: Sustituye esto por tu método personalizado de confirmación si usas DialogHost
            var resultado = MessageBox.Show(
                $"¿Está seguro de registrar el Egreso Legal de {beneficiario.Nombres} {beneficiario.Apellidos}?\nEsta acción cerrará el seguimiento activo.",
                "Confirmar Egreso",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                try
                {
                    // 3. Actualizar los campos del modelo
                    beneficiario.Estatus_Legal = "Egresado";
                    beneficiario.Estatus_Color = "#7F8C8D"; // Cambiamos a un gris neutro institucional para denotar inactivo/cerrado

                    // 4. Guardar cambios en la Base de Datos a través de tu contexto/repositorio
                    using (var context = new AppDbContext()) // Reemplaza por tu DbContext
                    {
                        context.Beneficiarios.Update(beneficiario);
                        await context.SaveChangesAsync();

                        await _auditoriaService.RegistrarAccionAsync(
                accion: "EGRESAR",
                modulo: "Expedientes (Archivo Físico)",
                detalles: $"Se egresó al beneficiario {BeneficiarioActual.NombreCompleto} (Cédula: {BeneficiarioActual.Cedula})"
            );
                    }

                    // 5. Notificar a la interfaz del cambio de propiedades si no usas un modelo Observable
                    OnPropertyChanged(nameof(BeneficiarioActual));

                    // Opcional: Podrías refrescar tu "ListaEvoluciones" para meter una fila automática 
                    // que diga: "SISTEMA: Caso cerrado por Egreso Legal el día X".
                }
                catch (Exception ex)
                {
                    // Manejo de errores al guardar en SQLite/EF Core
                    MessageBox.Show($"Error al procesar el egreso: {ex.Message}", "Error");
                }
            }
        }

        [RelayCommand]
        private async Task EditarObservaciones()
        {
            await NavegarAEditar();
        }

        [RelayCommand]
        private async Task GuardarCambiosContactoAsync()
        {
            // 1. Validación previa opcional
            if (BeneficiarioActual == null) return;

            if (string.IsNullOrWhiteSpace(NombreContactoEdit) || string.IsNullOrWhiteSpace(ParentescoContactoEdit) || string.IsNullOrWhiteSpace(TelefonoContactoEdit))
            {
                // Aquí podrías lanzar un mensaje en un Snackbar o MessageBox
                MessageBox.Show("Todos los campos del contacto son obligatorios.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                BeneficiarioActual.NombreContacto = NombreContactoEdit;
                BeneficiarioActual.ParentescoContacto = ParentescoContactoEdit;
                BeneficiarioActual.TelefonoContacto = TelefonoContactoEdit;

                // 2. Persistencia en la Base de Datos con EF Core
                using (var context = new AppDbContext())
                {
                    // Le decimos a EF que esta entidad ya existe pero sufrió modificaciones
                    context.Beneficiarios.Update(BeneficiarioActual);

                    // Guardamos los cambios de manera asíncrona en SQLite/SQLServer
                    await context.SaveChangesAsync();

                    await _auditoriaService.RegistrarAccionAsync(
                accion: "EDITAR CONTACTO",
                modulo: "Expedientes (Archivo Físico)",
                detalles: $"Se editó el contacto del beneficiario {BeneficiarioActual.NombreCompleto} (Cédula: {BeneficiarioActual.Cedula})"
            );

                    /*int beneficiarioId = BeneficiarioActual.Id_Beneficiario;

                    BeneficiarioActual = await context.Beneficiarios
                                           .FirstOrDefaultAsync(b => b.Id_Beneficiario == BeneficiarioActual.Id_Beneficiario);*/
                }

                // REFRESH: Forzar a WPF a re-evaluar los Bindings de este objeto
                OnPropertyChanged(nameof(BeneficiarioActual));

                // 3. Éxito: Cerramos el diálogo cambiando la propiedad a false
                IsEditarContactoDialogOpen = false;

                new Views.Dialogs.ExitoDialog("Contacto de emergencia actualizado con éxito.").ShowDialog();


            }
            catch (DbUpdateException ex)
            {
                // Manejo de errores de base de datos
                MessageBox.Show($"Error al guardar en la base de datos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // Manejo de errores generales
                MessageBox.Show($"Ocurrió un error inesperado: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void CancelarEdicionContacto()
        {
            IsEditarContactoDialogOpen = false;
        }

        [RelayCommand]
        private void AbrirDialogoContacto()
        {
            // Aquí podrías cargar los contactos relacionados al beneficiario actual si es necesario
            //Primero se limpia cualquier estado previo para evitar mostrar datos de otro beneficiario
            NombreContactoEdit = BeneficiarioActual?.NombreContacto ?? string.Empty;
            ParentescoContactoEdit = BeneficiarioActual?.ParentescoContacto ?? string.Empty;
            TelefonoContactoEdit = BeneficiarioActual?.TelefonoContacto ?? string.Empty;


            IsEditarContactoDialogOpen = true;
        }

        [RelayCommand]
        private async Task MoverBeneficiarioAPapelera(Beneficiario beneficiario)
        {
            if (beneficiario == null) return;

            // 1. Primera confirmación global
            var resultado = MessageBox.Show(
                "¿Está seguro de que desea mover este beneficiario a la papelera?",
                "Confirmar acción",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado != MessageBoxResult.Yes) return;

            try
            {
                // 2. Validar si tiene registros relacionados
                var evoluciones = await _service.ObtenerIntervencionesPorBeneficiario(beneficiario.Id_Beneficiario);
                var documentos = await _service.ObtenerDocumentosPorBeneficiarioAsync(beneficiario.Id_Beneficiario);

                bool proceder = true;

                if ((evoluciones != null && evoluciones.Any()) || (documentos != null && documentos.Any()))
                {
                    var mensajeAdicional = "Este beneficiario tiene " +
                        $"{(evoluciones != null ? evoluciones.Count() : 0)} evolución(es) y " +
                        $"{(documentos != null ? documentos.Count() : 0)} documento(s) asociados.\n\n" +
                        "Moverlo a la papelera también ocultará estos registros relacionados. ¿Desea continuar?";

                    var resultadoRelacionado = MessageBox.Show(
                        mensajeAdicional,
                        "Registros relacionados encontrados",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (resultadoRelacionado != MessageBoxResult.Yes)
                    {
                        proceder = false; // El usuario canceló la operación en la advertencia
                    }
                }

                // 3. Ejecutar el borrado si superó las validaciones (o si no tenía dependencias)
                if (proceder)
                {
                    bool exito = await _service.MoverBeneficiarioAPapeleraAsync(beneficiario.Id_Beneficiario);

                    if (exito)
                    {
                        await _auditoriaService.RegistrarAccionAsync(
                accion: "ELIMINAR",
                modulo: "Expedientes (Archivo Físico)",
                detalles: $"Se movió el beneficiario {BeneficiarioActual.NombreCompleto} (Cédula: {BeneficiarioActual.Cedula}) a la papelera"
            );
                        // Si tienes una lista observable en el sidebar de este mismo ViewModel, 
                        // deberías removerlo de ella aquí para que desaparezca visualmente de inmediato:
                        // ListaBeneficiarios?.Remove(beneficiario);

                        // Forzar el regreso a la pantalla principal (MostrarSiEsNull)
                        if (BeneficiarioActual != null)
                        {
                            BeneficiarioActual = null;
                        }

                        ListaEvoluciones?.Clear();
                        ListaDocumentos?.Clear();

                        new Views.Dialogs.ExitoDialog("Beneficiario enviado a la papelera con éxito.").ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show("No se pudo mover el Beneficiario a la papelera. Inténtelo nuevamente.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al procesar la solicitud: {ex.Message}", "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task MoverDocumentoAPapelera(DocumentoAdjunto documento)
        {
            if (documento == null) return;

            var resultado = MessageBox.Show(
                "¿Está seguro de que desea mover este documento a la papelera?",
                "Confirmar acción",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                // Ejecuta el borrado lógico en la base de datos
                await _service.MoverDocumentoAPapeleraAsync(documento.Id_Documento);

                await _auditoriaService.RegistrarAccionAsync(
                accion: "ELIMINAR",
                modulo: "Expedientes (Archivo Físico)",
                detalles: $"Se movió el documento {documento.NombreDocumento} a la papelera"
            );

                // 🔥 CORREGIDO: Recargar el historial del beneficiario actual para que desaparezca de la lista
                if (BeneficiarioActual != null)
                {
                    await CargarDocumentosDelBeneficiarioAsync(BeneficiarioActual.Id_Beneficiario);
                }

                new Views.Dialogs.ExitoDialog("Elemento enviado a la papelera.").ShowDialog();
            }
        }

        [RelayCommand]
        private void AbrirDialogoAgregar()
        {
            // Limpias el formulario anterior antes de abrir
            NuevaEvolucionTexto = string.Empty;
            ActividadSeleccionada = null;
            EspecialistaSeleccionado = null;
            FechaSeleccionada = DateTime.Now;

            // Abres el diálogo de forma segura
            IsEvolucionDialogOpen = true;
        }

        [RelayCommand]
        private void CancelarAgregarEvolucion()
        {
            IsEvolucionDialogOpen = false;
        }

        [RelayCommand]
        private async Task MoverEvolucionAPapelera(Evolucion evolucion)
        {
            if (evolucion == null) return;

            var resultado = MessageBox.Show(
                "¿Está seguro de que desea mover esta evolución a la papelera?",
                "Confirmar acción",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                // Ejecuta el borrado lógico en la base de datos
                await _service.MoverEvolucionAPapeleraAsync(evolucion.Id_Evolucion);

                await _auditoriaService.RegistrarAccionAsync(
                accion: "ELIMINAR",
                modulo: "Expedientes (Archivo Físico)",
                detalles: $"Se movió la evolución {evolucion.Actividad.Tipo_Actividad} del beneficiario {BeneficiarioActual.NombreCompleto} a la papelera"
            );

                // 🔥 CORREGIDO: Recargar el historial del beneficiario actual para que desaparezca de la lista
                if (BeneficiarioActual != null)
                {
                    await CargarHistorialEvolucion(BeneficiarioActual.Id_Beneficiario);
                }

                new Views.Dialogs.ExitoDialog("Elemento enviado a la papelera.").ShowDialog();
            }
        }

        // 🔥 SOLUCIÓN B: Método para inicializar el estado directamente al regresar de una navegación
        public async Task InicializarConBeneficiarioAsync(Beneficiario beneficiario)
        {
            if (beneficiario == null) return;

            BeneficiarioActual = beneficiario;
            SearchText = string.Empty;
            ResultadosBusqueda.Clear();
            OnPropertyChanged(nameof(HayResultados));

            // Cargar hilos de datos vinculados inmediatamente
            await CargarHistorialEvolucion(beneficiario.Id_Beneficiario);
            await CargarDocumentosDelBeneficiarioAsync(beneficiario.Id_Beneficiario);
        }

        private (string rutaImagen, string rutaWord) GuardarArchivosFisicos(string cedulaBeneficiario, string rutaImagenTemporal, string textoOcr)
        {
            string carpetaRaiz = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "IDENNA_Files");
            string carpetaImagenes = Path.Combine(carpetaRaiz, "ImagenesOriginales");
            string carpetaDocumentos = Path.Combine(carpetaRaiz, "DocumentosWord");

            Directory.CreateDirectory(carpetaImagenes);
            Directory.CreateDirectory(carpetaDocumentos);

            string identificadorUnico = Guid.NewGuid().ToString();
            string nombreArchivoImagen = $"{cedulaBeneficiario}_{identificadorUnico}.png";
            string nombreArchivoWord = $"{cedulaBeneficiario}_{identificadorUnico}.docx";

            string rutaDestinoImagen = Path.Combine(carpetaImagenes, nombreArchivoImagen);
            string rutaDestinoWord = Path.Combine(carpetaDocumentos, nombreArchivoWord);

            if (File.Exists(rutaImagenTemporal))
            {
                File.Copy(rutaImagenTemporal, rutaDestinoImagen, overwrite: true);
            }

            using (DocX documento = DocX.Create(rutaDestinoWord))
            {
                documento.InsertParagraph("INSTITUTO AUTÓNOMO CONSEJO NACIONAL DE DERECHO DE NIÑOS, NIÑAS Y ADOLESCENTES (IDENNA)")
                         .Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;

                documento.InsertParagraph("MÓDULO DE DIGITALIZACIÓN AUTOMATIZADA - EXPEDIENTE DIGITAL")
                         .Font("Times New Roman").FontSize(12).Italic().Alignment = Alignment.center;

                documento.InsertParagraph($"\nFecha de Procesamiento: {DateTime.Now:dd/MM/yyyy hh:mm tt}")
                         .Font("Times New Roman").FontSize(10);

                documento.InsertParagraph($"Cédula Asociada: {cedulaBeneficiario}\n")
                         .Font("Times New Roman").FontSize(10).Bold();

                documento.InsertParagraph("--------------------------------------------------------------------------------------------------------\n")
                         .Alignment = Alignment.center;

                documento.InsertParagraph("TEXTO DIGITALIZADO AUTOMÁTICAMENTE POR LA HERRAMIENTA OCR:\n")
                         .Font("Times New Roman").FontSize(12).Bold();

                documento.InsertParagraph(textoOcr)
                         .Font("Times New Roman").FontSize(12).Alignment = Alignment.left;

                documento.Save();
            }

            return (rutaDestinoImagen, rutaDestinoWord);
        }

        [RelayCommand]
        public async Task DigitalizarDocumentoAsync()
        {
            if (BeneficiarioActual == null)
            {
                MessageBox.Show("Por favor, seleccione un beneficiario antes de subir un documento.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Imágenes (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                Title = "Seleccionar documento / informe escaneado"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string rutaArchivo = openFileDialog.FileName;
                string nombreArchivo = openFileDialog.SafeFileName;

                try
                {
                    IsOcrProcesando = true;

                    byte[] archivoBytes = await Task.Run(() => File.ReadAllBytes(rutaArchivo));
                    var ocrService = new Services.OcrService();
                    string textoExtraido = await Task.Run(() => ocrService.ExtraerTextoDeImagen(rutaArchivo));

                    var (rutaImagen, rutaWord) = GuardarArchivosFisicos(BeneficiarioActual.Cedula, rutaArchivo, textoExtraido);

                    var nuevoDocumento = new DocumentoAdjunto
                    {
                        Id_Beneficiario = BeneficiarioActual.Id_Beneficiario,
                        NombreArchivo = nombreArchivo,
                        ArchivoBytes = archivoBytes,
                        TextoOcr = textoExtraido,
                        RutaImagen = rutaImagen,
                        RutaWord = rutaWord,
                        FechaRegistro = DateTime.Now
                    };

                    await _service.GuardarDocumentoAdjuntoAsync(nuevoDocumento);
                    await CargarDocumentosDelBeneficiarioAsync(BeneficiarioActual.Id_Beneficiario);

                    await _auditoriaService.RegistrarAccionAsync(
                accion: "CREAR",
                modulo: "Expedientes (Archivo Físico)",
                detalles: $"Se digitalizó el documento {nombreArchivo} del beneficiario {BeneficiarioActual.NombreCompleto} (Cédula: {BeneficiarioActual.Cedula})"
            );

                    MessageBox.Show("¡Documento digitalizado y formateado con éxito bajo los lineamientos institucionales!", "Completado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al procesar el documento: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsOcrProcesando = false;
                }
            }
        }

        [RelayCommand]
        private async Task AdjuntarDocumentoDigital()
        {
            if (BeneficiarioActual == null)
            {
                MessageBox.Show("Por favor, seleccione un beneficiario antes de adjuntar un documento.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Documentos (*.pdf;*.docx;*.jpg;*.png)|*.pdf;*.docx;*.jpg;*.png",
                Title = "Seleccionar documento para adjuntar"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string rutaArchivo = openFileDialog.FileName;
                string nombreArchivo = openFileDialog.SafeFileName;

                try
                {
                    IsOcrProcesando = true;
                    byte[] archivoBytes = await Task.Run(() => File.ReadAllBytes(rutaArchivo));

                    var nuevoDocumento = new DocumentoAdjunto
                    {
                        Id_Beneficiario = BeneficiarioActual.Id_Beneficiario,
                        NombreArchivo = nombreArchivo,
                        ArchivoBytes = archivoBytes,
                        TextoOcr = string.Empty,
                        FechaRegistro = DateTime.Now
                    };

                    await _service.GuardarDocumentoAdjuntoAsync(nuevoDocumento);
                    await CargarDocumentosDelBeneficiarioAsync(BeneficiarioActual.Id_Beneficiario);

                    await _auditoriaService.RegistrarAccionAsync(
                accion: "CREAR",
                modulo: "Expedientes (Archivo Físico)",
                detalles: $"Se adjuntó el documento {nombreArchivo} del beneficiario {BeneficiarioActual.NombreCompleto} (Cédula: {BeneficiarioActual.Cedula})"
            );

                    MessageBox.Show("¡Documento adjuntado y guardado en la base de datos con éxito!", "Completado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al adjuntar el documento: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsOcrProcesando = false;
                }
            }
        }

        [RelayCommand]
        public void AbrirDocumento(DocumentoAdjunto? documento)
        {
            if (documento == null || documento.ArchivoBytes == null || documento.ArchivoBytes.Length == 0)
            {
                MessageBox.Show("El archivo solicitado no contiene datos válidos.", "Archivo no encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string carpetaTemporal = Path.Combine(Path.GetTempPath(), "IDENNA_Cache");
                if (!Directory.Exists(carpetaTemporal)) Directory.CreateDirectory(carpetaTemporal);

                string rutaTemporal = Path.Combine(carpetaTemporal, documento.NombreArchivo);
                File.WriteAllBytes(rutaTemporal, documento.ArchivoBytes);

                Process.Start(new ProcessStartInfo(rutaTemporal) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir el documento: {ex.Message}", "Error del Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public void AbrirDocumentoDigitalizado(DocumentoAdjunto? documento)
        {
            if (documento == null)
            {
                MessageBox.Show("El documento seleccionado no es válido.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(documento.RutaWord) && File.Exists(documento.RutaWord))
                {
                    Process.Start(new ProcessStartInfo(documento.RutaWord) { UseShellExecute = true });
                    return;
                }

                if (string.IsNullOrWhiteSpace(documento.TextoOcr))
                {
                    MessageBox.Show("No se encontró el archivo original en el disco ni texto digitalizado (OCR) para abrir.", "Archivo no disponible", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string carpetaTemporal = Path.Combine(Path.GetTempPath(), "IDENNA_Cache");
                if (!Directory.Exists(carpetaTemporal)) Directory.CreateDirectory(carpetaTemporal);

                string nombreRespaldo = (!string.IsNullOrWhiteSpace(documento.NombreArchivo)
                    ? Path.GetFileNameWithoutExtension(documento.NombreArchivo)
                    : "Documento") + "_Recuperado.doc";

                string rutaTemporal = Path.Combine(carpetaTemporal, nombreRespaldo);

                StringBuilder htmlBuilder = new StringBuilder();
                htmlBuilder.AppendLine("<html><head><meta charset='utf-8'></head>");
                htmlBuilder.AppendLine("<body style='font-family: Arial, sans-serif; font-size: 12pt; line-height: 1.6; padding: 30px;'>");

                string cuerpoTexto = documento.TextoOcr.Replace("\r\n", "<br/>").Replace("\n", "<br/>");
                htmlBuilder.AppendLine($"<p>{cuerpoTexto}</p></body></html>");

                File.WriteAllText(rutaTemporal, htmlBuilder.ToString(), Encoding.UTF8);
                Process.Start(new ProcessStartInfo(rutaTemporal) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir el documento en Word: {ex.Message}", "Error del Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public async Task CargarDocumentosDelBeneficiarioAsync(int beneficiarioId)
        {
            var documentos = await _service.ObtenerDocumentosPorBeneficiarioAsync(beneficiarioId);
            ListaDocumentos = new ObservableCollection<DocumentoAdjunto>(documentos);
        }

        [RelayCommand]
        private async Task AbrirEditorEvolucion(Evolucion evolucion)
        {
            if (evolucion == null) return;

            // 🔥 REGLA DE SEGURIDAD: Si las listas del sistema están vacías, las cargamos en el momento
            if (TutoresDisponibles == null || !TutoresDisponibles.Any()) await CargarTutoresAsync();
            if (ActividadesDisponibles == null || !ActividadesDisponibles.Any()) await CargarActividadesAsync();

            _evolucionEnEdicion = evolucion;
            DescripcionEdit = evolucion.Detalle;

            if (evolucion.Especialista != null && TutoresDisponibles != null)
            {
                TutorSeleccionadoEdit = TutoresDisponibles.FirstOrDefault(t => t.Id_Especialista == evolucion.Especialista.Id_Especialista);
            }
            else
            {
                TutorSeleccionadoEdit = null;
            }

            if (evolucion.Actividad != null && ActividadesDisponibles != null)
            {
                ActividadSeleccionadaEdit = ActividadesDisponibles.FirstOrDefault(a => a.Id_Actividad == evolucion.Actividad.Id_Actividad);
            }
            else
            {
                ActividadSeleccionadaEdit = null;
            }

            FechaSeleccionadaEdit = evolucion.Fecha_Registro;

            IsEditEvolucionVisible = true;
        }

        [RelayCommand]
        private async Task GuardarCambiosEvolucion()
        {
            // 🔥 VALIDACIÓN DE SEGURIDAD: Si es null, aborta antes de que la app se caiga
            if (_evolucionEnEdicion == null)
            {
                MessageBox.Show("Error del sistema: Se intentó guardar una edición sin haber seleccionado una evolución.",
                                "Error de enlace", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(DescripcionEdit) || TutorSeleccionadoEdit == null || ActividadSeleccionadaEdit == null) return;

            if (BeneficiarioActual == null || ActividadSeleccionadaEdit == null || TutorSeleccionadoEdit == null || FechaSeleccionadaEdit == DateTime.MinValue)
            {
                return;
            }

            _evolucionEnEdicion.Detalle = DescripcionEdit;
            _evolucionEnEdicion.Especialista = TutorSeleccionadoEdit;
            _evolucionEnEdicion.Actividad = ActividadSeleccionadaEdit;
            _evolucionEnEdicion.Id_Actividad = ActividadSeleccionadaEdit.Id_Actividad;
            _evolucionEnEdicion.Fecha_Registro = FechaSeleccionadaEdit ?? DateTime.Now;

            await _service.UpdateEvolucionAsync(_evolucionEnEdicion);

            await _auditoriaService.RegistrarAccionAsync(
                accion: "MODIFICAR",
                modulo: "Expedientes (Archivo Físico)",
                detalles: $"Se editó la evolución {_evolucionEnEdicion.Actividad.Tipo_Actividad} del beneficiario {BeneficiarioActual.NombreCompleto} (Cédula: {BeneficiarioActual.Cedula})"
            );

            if (ListaEvoluciones != null)
            {
                int index = ListaEvoluciones.IndexOf(_evolucionEnEdicion);
                if (index >= 0)
                {
                    ListaEvoluciones.RemoveAt(index);
                    ListaEvoluciones.Insert(index, _evolucionEnEdicion);
                }
            }

            await CargarHistorialEvolucion(BeneficiarioActual.Id_Beneficiario);
            IsEditEvolucionVisible = false;
        }

        [RelayCommand]
        private void CancelarEdicion() => IsEditEvolucionVisible = false;

        [RelayCommand]
        private async Task NavegarAEditar()
        {
            if (BeneficiarioActual == null) return;
            var vm = new RegistroBeneficiarioViewModel(_service, _repository, _auditoriaService);
            vm.CargarBeneficiario(BeneficiarioActual.Clonar());
            WeakReferenceMessenger.Default.Send(new NavegarMensaje(vm));
        }

        [RelayCommand]
        private async Task CargarActividadesAsync()
        {
            try
            {
                var lista = await _service.ObtenerTodasLasActividades();
                ActividadesDisponibles = new ObservableCollection<Actividad>(lista);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cargando actividades: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task CargarTutoresAsync()
        {
            try
            {
                var lista = await _service.ObtenerTodosLosTutores();
                TutoresDisponibles = new ObservableCollection<Tutores>(lista);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cargando Tutores: {ex.Message}");
            }
        }

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
            await InicializarConBeneficiarioAsync(seleccionado);
        }

        [RelayCommand]
        public async Task CargarHistorialEvolucion(int beneficiarioId)
        {
            var historial = await _service.ObtenerIntervencionesPorBeneficiario(beneficiarioId);
            ListaEvoluciones = new ObservableCollection<Evolucion>(historial);
        }

        [RelayCommand]
        public async Task GuardarEvolucion()
        {
            if (BeneficiarioActual == null || ActividadSeleccionada == null || string.IsNullOrWhiteSpace(NuevaEvolucionTexto) || EspecialistaSeleccionado == null || FechaSeleccionada == DateTime.MinValue)
            {
                return;
            }

            var nuevaEvo = new Evolucion
            {
                Id_Beneficiario = BeneficiarioActual.Id_Beneficiario,
                Id_Actividad = ActividadSeleccionada.Id_Actividad,
                Especialista = EspecialistaSeleccionado,
                Actividad = ActividadSeleccionada,
                Beneficiario = BeneficiarioActual,
                Detalle = NuevaEvolucionTexto,
                Fecha_Registro = FechaSeleccionada
            };

            await _service.RegistrarIntervencion(nuevaEvo);

            await _auditoriaService.RegistrarAccionAsync(
                accion: "CREAR",
                modulo: "Expedientes (Archivo Físico)",
                detalles: $"Se registró una nueva evolución de {ActividadSeleccionada.Tipo_Actividad} para el beneficiario {BeneficiarioActual.NombreCompleto} (Cédula: {BeneficiarioActual.Cedula})"
            );

            NuevaEvolucionTexto = string.Empty;
            ActividadSeleccionada = null;
            EspecialistaSeleccionado = null;
            FechaSeleccionada = DateTime.Now;
            await CargarHistorialEvolucion(BeneficiarioActual.Id_Beneficiario);

            IsEvolucionDialogOpen = false;
        }

        [RelayCommand]
        public void NavegarARegistro()
        {
            WeakReferenceMessenger.Default.Send(new NavegarMensaje(new RegistroBeneficiarioViewModel(_service, _repository, _auditoriaService)));
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
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    var ventanaRecorte = new Views.RecortarImagenView(bitmap);

                    if (ventanaRecorte.ShowDialog() == true)
                    {
                        FotoSeleccionada = ventanaRecorte.ImagenRecortadaBytes;

                        if (BeneficiarioActual != null)
                        {
                            BeneficiarioActual.Foto = FotoSeleccionada;
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

        #region Egresos

        [ObservableProperty]
        private bool _isEgresoDialogOpen;

        [ObservableProperty]
        private DateTime _fechaEgresoForm = DateTime.Today;

        [ObservableProperty]
        private string? _motivoEgresoSeleccionado;

        [ObservableProperty]
        private string? _observacionesEgresoForm;

        [RelayCommand]
        private async Task RevertirEgresoAsync()
        {
            if (BeneficiarioActual == null) return;

            // 1. Confirmación de seguridad extrema (evitar clics accidentales)
            MessageBoxResult resultado = MessageBox.Show(
                $"¿Está seguro de que desea REVERTIR el egreso de {BeneficiarioActual.Nombres}? " +
                "El expediente volverá a estar activo y se eliminará el registro de salida.",
                "Advertencia de Seguridad",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado != MessageBoxResult.Yes) return;

            try
            {
                using (var context = new AppDbContext()) // Reemplaza por tu DbContext
                {
                    // 2. Buscar el registro de egreso en la Base de Datos
                    var egresoExistente = await context.Egresos
                    .FirstOrDefaultAsync(e => e.Id_Beneficiario == BeneficiarioActual.Id_Beneficiario); // Usa la propiedad PK de tu Beneficiario

                    if (egresoExistente != null)
                    {
                        // 3. Remover el egreso de la base de datos
                        context.Egresos.Remove(egresoExistente);
                    }
                    else
                    {
                        // Alerta de diagnóstico: El estatus decía egresado, pero no había fila en Egresos
                        System.Diagnostics.Debug.WriteLine($"[ADVERTENCIA] No se encontró un registro histórico en la tabla Egresos para el ID: {BeneficiarioActual.Id_Beneficiario}");
                    }

                    // 4. Restaurar el estado original del Beneficiario
                    BeneficiarioActual.Estatus_Legal = "Activo"; // O el estatus por defecto que manejes
                    BeneficiarioActual.Estatus_Color = "#3498DB"; // Volver al azul institucional (o el color activo)

                    // 5. Actualizar la entidad del beneficiario en el contexto
                    context.Beneficiarios.Update(BeneficiarioActual);

                    // 6. Impactar los cambios en SQLite de forma atómica
                    await context.SaveChangesAsync();

                    await _auditoriaService.RegistrarAccionAsync(
                accion: "ELIMINAR",
                modulo: "Expedientes (Archivo Físico)",
                detalles: $"Se revertió el egreso del beneficiario {BeneficiarioActual.NombreCompleto} (Cédula: {BeneficiarioActual.Cedula})"
            );

                    // 7. Notificar a la UI para que refresque los colores y etiquetas
                    OnPropertyChanged(nameof(BeneficiarioActual));

                    MessageBox.Show("El egreso ha sido revertido con éxito. El expediente está activo nuevamente.",
                                    "Proceso Exitoso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al revertir el egreso: {ex.Message}", "Error de Base de Datos");
            }
        }

        // Catálogo de motivos institucionales comunes en el IDENNA
        public List<string> ListaMotivosEgreso { get; } = new()
        {
            "Reunificación Familiar",
            "Colocación Familiar (Terceros)",
            "Adopción (Decreto Definitivo)",
            "Mayoría de Edad (Egreso por 18 años)",
            "Traslado a otra Entidad de Atención",
            "Modificación o Revocación de la Medida",
            "Otros"
        };

        [RelayCommand]
        private void AbrirFormularioEgreso()
        {
            if (BeneficiarioActual == null) return;

            if (BeneficiarioActual.Estatus_Legal == "Egresado")
            {
                new Views.Dialogs.ExitoDialog("El beneficiario ya se encuentra egresado.").ShowDialog();
                return;
            }

            // Inicializar el formulario con valores por defecto
            FechaEgresoForm = DateTime.Today;
            MotivoEgresoSeleccionado = null;
            ObservacionesEgresoForm = string.Empty;

            // Mostrar el modal en la interfaz
            IsEgresoDialogOpen = true;
        }

        [RelayCommand]
        private async Task GuardarEgresoAsync()
        {
            if (string.IsNullOrWhiteSpace(MotivoEgresoSeleccionado))
            {
                // Aquí podrías validar o mostrar un error en la UI
                MessageBox.Show($"Error: Debe seleccionar un motivo de egreso.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // 1. Crear la instancia de tu modelo Egreso
                var nuevoEgreso = new Egreso
                {
                    Id_Beneficiario = BeneficiarioActual.Id_Beneficiario, // Asegúrate de que coincida con la PK de tu Beneficiario
                    Fecha_Salida = FechaEgresoForm,
                    Motivo_Egreso = MotivoEgresoSeleccionado,
                    Observaciones_Salida = ObservacionesEgresoForm,
                    Beneficiario = BeneficiarioActual // Establece la relación con el beneficiario actual
                };

                // 2. Actualizar el estado del Beneficiario activo
                BeneficiarioActual.Estatus_Legal = "Egresado";
                BeneficiarioActual.Estatus_Color = "#7F8C8D"; // Gris institucional

                // 3. Guardar todo en la Base de Datos a través de EF Core
                using (var context = new AppDbContext()) // Reemplaza por tu DbContext
                {
                    context.Egresos.Add(nuevoEgreso);
                    context.Beneficiarios.Update(BeneficiarioActual);

                    await context.SaveChangesAsync();

                    await _auditoriaService.RegistrarAccionAsync(
                accion: "EGRESO",
                modulo: "Expedientes (Archivo Físico)",
                detalles: $"Se registró el egreso del beneficiario {BeneficiarioActual.NombreCompleto} (Cédula: {BeneficiarioActual.Cedula})"
            );
                }

                // 4. Cerrar diálogo y refrescar interfaz
                IsEgresoDialogOpen = false;
                OnPropertyChanged(nameof(BeneficiarioActual));

                // Opcional: Recargar la bitácora o emitir evolución automática de cierre.
            }
            catch (Exception ex)
            {
                // Manejar errores de SQLite/DB de forma amigable
                MessageBox.Show($"Error al registrar el egreso en la base de datos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void CancelarEgreso()
        {
            IsEgresoDialogOpen = false;
        }

        #endregion
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using TEST_IDENNA.Data;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Models;
using TEST_IDENNA.Services;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace TEST_IDENNA.ViewModels
{
    public partial class DigitalizarDocumentosViewModel : ObservableObject
    {
        // 1. Traemos las dependencias de tu arquitectura
        private readonly IBeneficiarioRepository _repository;
        private readonly IIntervencionService _service;
        private readonly IAuditoriaService _auditoriaService;
        private readonly OcrService _ocrService;

        // Propiedades internas para conservar el archivo cargado
        [ObservableProperty] private byte[]? _archivoBytes;
        private string _nombreArchivo = string.Empty;

        [ObservableProperty] private bool _isProcessing;
        [ObservableProperty] private string _mensajeEstado = "Esperando documento...";
        [ObservableProperty] private string _textoOcrResult = string.Empty;
        [ObservableProperty] private string _cedulaExtraida = string.Empty;
        [ObservableProperty] private bool _documentoReconocido;

        // Colección de beneficiarios a los que se vinculará el documento
        [ObservableProperty]
        private ObservableCollection<Beneficiario> _beneficiariosAsignados = new();

        // Texto informativo para la interfaz de usuario
        [ObservableProperty]
        private string _nombresBeneficiariosAsignados = "Ninguno (No ligado)";

        // 2. El constructor recibe e inyecta todo lo necesario de forma automática
        public DigitalizarDocumentosViewModel(IBeneficiarioRepository repository, IIntervencionService service, OcrService ocrService, IAuditoriaService auditoriaService)
        {
            _repository = repository;
            _service = service;
            _ocrService = ocrService;
            _auditoriaService = auditoriaService;
        }

        private string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

            string textoFormaD = texto.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            foreach (char caracter in textoFormaD)
            {
                UnicodeCategory categoria = CharUnicodeInfo.GetUnicodeCategory(caracter);
                if (categoria != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(caracter);
                }
            }

            // Corregido: Se eliminó el texto residual "Michael" que causaba error de sintaxis
            return sb.ToString().ToLower().Trim();
        }

        public Beneficiario? BuscarBeneficiarioPorNombre(string textoOcr, IEnumerable<Beneficiario> listaBeneficiarios)
        {
            if (string.IsNullOrEmpty(textoOcr) || listaBeneficiarios == null) return null;

            string textoLimpio = NormalizarTexto(textoOcr);

            foreach (var beneficiario in listaBeneficiarios)
            {
                if (string.IsNullOrEmpty(beneficiario.Nombres)) continue;

                string nombreCompletoDB = NormalizarTexto($"{beneficiario.Nombres} {beneficiario.Apellidos}");

                // Estrategia A: Búsqueda por la cadena exacta del nombre completo
                if (textoLimpio.Contains(nombreCompletoDB))
                {
                    return beneficiario;
                }

                // Estrategia B: Coexistencia del primer nombre y primer apellido
                string primerNombre = NormalizarTexto(beneficiario.Nombres.Split(' ')[0]);
                string primerApellido = !string.IsNullOrEmpty(beneficiario.Apellidos)
                    ? NormalizarTexto(beneficiario.Apellidos.Split(' ')[0])
                    : string.Empty;

                if (!string.IsNullOrEmpty(primerApellido) && textoLimpio.Contains(primerNombre) && textoLimpio.Contains(primerApellido))
                {
                    return beneficiario;
                }
            }

            return null;
        }

        // Ruta temporal de la imagen que el usuario soltó en el Drag & Drop
        private string? _rutaImagenTemporal;

        private (string rutaImagen, string rutaWord) GuardarArchivosFisicos(string cedulaBeneficiario)
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

            if (File.Exists(_rutaImagenTemporal))
            {
                if (ArchivoBytes != null)
                {
                    File.WriteAllBytes(rutaDestinoImagen, ArchivoBytes);
                }
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

                documento.InsertParagraph(TextoOcrResult)
                         .Font("Times New Roman").FontSize(12).Alignment = Alignment.left;

                documento.Save();
            }

            return (rutaDestinoImagen, rutaDestinoWord);
        }

        [RelayCommand]
        private async Task ProcesarDocumentoAsync(string rutaArchivo)
        {
            if (string.IsNullOrEmpty(rutaArchivo) || !File.Exists(rutaArchivo)) return;

            IsProcessing = true;
            MensajeEstado = "Analizando documento con OCR...";
            DocumentoReconocido = false;
            TextoOcrResult = string.Empty;
            BeneficiariosAsignados.Clear();
            ActualizarTextoAsignados();

            try
            {
                _rutaImagenTemporal = rutaArchivo;

                _nombreArchivo = Path.GetFileName(rutaArchivo);
                ArchivoBytes = await Task.Run(() => File.ReadAllBytes(rutaArchivo));

                // Ejecución del OCR
                string textoLector = await Task.Run(() => _ocrService.ExtraerTextoDeImagen(rutaArchivo));
                TextoOcrResult = textoLector;
                CedulaExtraida = BuscarCedulaEnTexto(textoLector);

                DocumentoReconocido = true;

                using (var db = new AppDbContext())
                {
                    Beneficiario? beneficiarioDetectado = null;

                    // ESTRATEGIA 1: Buscar por cédula si el OCR logró extraer una
                    if (!string.IsNullOrEmpty(CedulaExtraida))
                    {
                        beneficiarioDetectado = await _repository.ObtenerPorCedulaAsync(CedulaExtraida);
                    }

                    // ESTRATEGIA 2: Si no se encontró por cédula, buscamos por Nombre cruzando los datos
                    if (beneficiarioDetectado == null)
                    {
                        MensajeEstado = "Cédula no encontrada. Buscando coincidencias por nombres...";
                        var todosLosBeneficiarios = await _repository.ObtenerTodos();
                        beneficiarioDetectado = BuscarBeneficiarioPorNombre(textoLector, todosLosBeneficiarios);
                    }

                    // Evaluar si alguna de las dos estrategias tuvo éxito
                    if (beneficiarioDetectado != null)
                    {
                        BeneficiariosAsignados.Add(beneficiarioDetectado);
                        MensajeEstado = "¡Beneficiario identificado automáticamente!";
                    }
                    else
                    {
                        MensajeEstado = "Documento procesado. No se encontró ninguna coincidencia por Cédula ni por Nombre.";
                    }
                }
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al ejecutar el OCR: {ex.Message}";
            }
            finally
            {
                ActualizarTextoAsignados();
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private void AbrirSelectorManual()
        {
            var selectorVm = new SeleccionarBeneficiariosViewModel(_repository, BeneficiariosAsignados.ToList());
            var ventana = new Views.SeleccionarBeneficiariosView(selectorVm);

            if (ventana.ShowDialog() == true)
            {
                BeneficiariosAsignados.Clear();
                foreach (var b in selectorVm.ObtenerSeleccionados())
                {
                    BeneficiariosAsignados.Add(b);
                }
                ActualizarTextoAsignados();
            }
        }

        private void ActualizarTextoAsignados()
        {
            if (BeneficiariosAsignados.Count == 0)
            {
                NombresBeneficiariosAsignados = "Ninguno (No ligado)";
            }
            else if (BeneficiariosAsignados.Count == 1)
            {
                var b = BeneficiariosAsignados[0];
                NombresBeneficiariosAsignados = $"{b.Nombres} {b.Apellidos} (C.I: {b.Cedula})";
            }
            else
            {
                NombresBeneficiariosAsignados = $"{BeneficiariosAsignados.Count} beneficiarios seleccionados";
            }
        }

        [RelayCommand]
        private async Task VerificarYEnrutarAsync()
        {
            if (BeneficiariosAsignados.Count == 0)
            {
                MessageBox.Show("No se ha ligado el documento a ningún Beneficiario. Use la opción de asignación manual.",
                                "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ArchivoBytes == null)
            {
                MessageBox.Show("No hay ningún archivo cargado para guardar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IsProcessing = true;
            MensajeEstado = "Guardando expediente digital y físico...";

            try
            {
                foreach (var b in BeneficiariosAsignados)
                {
                    var (rutaImagen, rutaWord) = GuardarArchivosFisicos(b.Cedula);

                    var nuevoDocumento = new DocumentoAdjunto
                    {
                        Id_Beneficiario = b.Id_Beneficiario,
                        NombreArchivo = _nombreArchivo,
                        ArchivoBytes = ArchivoBytes,
                        TextoOcr = TextoOcrResult,
                        RutaImagen = rutaImagen,
                        RutaWord = rutaWord,
                        FechaRegistro = DateTime.Now
                    };

                    await _service.GuardarDocumentoAdjuntoAsync(nuevoDocumento);

                    await _auditoriaService.RegistrarAccionAsync(
                accion: "GUARDAR",
                modulo: "Digitalización de Documentos",
                detalles: $"Se guardó el documento para el beneficiario {b.NombreCompleto} (Cédula: {b.Cedula})"
            );
                }

                MessageBox.Show($"¡El expediente se ha unificado con éxito! Se guardaron los archivos en disco y el registro en la base de datos para los ({BeneficiariosAsignados.Count}) beneficiarios.",
                                "Éxito de Indexación", MessageBoxButton.OK, MessageBoxImage.Information);

                ReiniciarFormulario();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error crítico al unificar el registro: {ex.Message}", "Error de Persistencia", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ReiniciarFormulario()
        {
            TextoOcrResult = string.Empty;
            CedulaExtraida = string.Empty;
            BeneficiariosAsignados.Clear();
            ArchivoBytes = null;
            _nombreArchivo = string.Empty;
            DocumentoReconocido = false;
            MensajeEstado = "Esperando documento...";
            ActualizarTextoAsignados();
        }

        private string BuscarCedulaEnTexto(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return string.Empty;

            string patronRobusto = @"\b(?:V|E|v|e)?\s*[-.\s,]*(\d{1,2})\s*[-.\s,]*(\d{3})\s*[-.\s,]*(\d{3})\b";
            Match match = Regex.Match(texto, patronRobusto);

            if (match.Success)
            {
                return match.Groups[1].Value + match.Groups[2].Value + match.Groups[3].Value;
            }

            Match matchLimpio = Regex.Match(texto, @"\b\d{7,8}\b");
            return matchLimpio.Success ? matchLimpio.Value : string.Empty;
        }
    }
}
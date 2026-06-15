using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Models;
using TEST_IDENNA.Services;

namespace TEST_IDENNA.ViewModels
{
    public partial class PapeleraViewModel : ObservableObject
    {
        private readonly IIntervencionService _intervencionService;
        private readonly IAuditoriaService _auditoriaService;

        public bool PermisoAdmin => SesionSistema.EsAdmin;

        #region Colecciones Blandas (UI Bindings)

        [ObservableProperty]
        private ObservableCollection<Evolucion> _listaEvolucionesPapelera = new();

        [ObservableProperty]
        private ObservableCollection<DocumentoAdjunto> _listaDocumentosPapelera = new();

        [ObservableProperty]
        private ObservableCollection<Beneficiario> _listaBeneficiariosPapelera = new();

        #endregion

        // Constructor con Inyección de Dependencias
        public PapeleraViewModel(IIntervencionService intervencionService, IAuditoriaService auditoriaService)
        {
            _intervencionService = intervencionService;
            _auditoriaService = auditoriaService;

            // PROTECCIÓN CRÍTICA: Detener la ejecución si estamos en el diseñador de Visual Studio
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return;
            }
        }

        #region Comando de Carga Principal

        [RelayCommand]
        private async Task CargarPapeleraAsync()
        {
            if (_intervencionService == null) return;

            try
            {
                // 1. Cargar Evoluciones eliminadas
                var evoluciones = await _intervencionService.ObtenerEvolucionesPapeleraAsync();
                ListaEvolucionesPapelera = new ObservableCollection<Evolucion>(evoluciones);

                // 2. Cargar Documentos eliminados
                var documentos = await _intervencionService.ObtenerDocumentosPapeleraAsync();
                ListaDocumentosPapelera = new ObservableCollection<DocumentoAdjunto>(documentos);

                // 3. Cargar Beneficiarios eliminados (Apunta a tu futuro método del servicio)
                 var beneficiarios = await _intervencionService.ObtenerBeneficiariosPapeleraAsync();
                 ListaBeneficiariosPapelera = new ObservableCollection<Beneficiario>(beneficiarios);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar la papelera: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Acciones: Evoluciones

        [RelayCommand]
        private async Task RestaurarEvolucionAsync(Evolucion evolucion)
        {
            if (evolucion == null) return;

            bool exito = await _intervencionService.RestaurarEvolucionAsync(evolucion.Id_Evolucion); // Asegura que coincida con tu PK
            if (exito)
            {
                await _auditoriaService.RegistrarAccionAsync(
                accion: "CREAR",
                modulo: "Bitácora de Actividades",
                detalles: $"Se restauró la evolución: {evolucion.Actividad} del beneficiario {evolucion.Beneficiario.NombreCompleto}"
            );
                ListaEvolucionesPapelera.Remove(evolucion);
            }
        }

        [RelayCommand]
        private async Task EliminarEvolucionPermanenteAsync(Evolucion evolucion)
        {
            if (evolucion == null) return;

            if(PermisoAdmin == false)
            {
                MessageBox.Show("No tienes permisos para eliminar evoluciones de forma permanente.", "Permiso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var resultado = MessageBox.Show("¿Está completamente seguro de eliminar esta evolución de forma permanente? Esta acción destruirá el registro físico.",
                "Confirmar Eliminación Definitiva", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                bool exito = await _intervencionService.EliminarEvolucionPermanenteAsync(evolucion.Id_Evolucion);
                if (exito)
                {
                    await _auditoriaService.RegistrarAccionAsync(
                accion: "ELIMINAR",
                modulo: "Bitácora de Actividades",
                detalles: $"Se eliminó la evolución: {evolucion.Actividad} del beneficiario {evolucion.Beneficiario.NombreCompleto}"
            );

                    ListaEvolucionesPapelera.Remove(evolucion);
                    new Views.Dialogs.ExitoDialog("Evolución eliminada de forma permanente.").ShowDialog();
                }
            }
        }

        #endregion

        #region Acciones: Documentos Adjuntos

        [RelayCommand]
        private async Task RestaurarDocumentoAsync(DocumentoAdjunto documento)
        {
            if (documento == null) return;

            bool exito = await _intervencionService.RestaurarDocumentoAsync(documento.Id_Documento); // Asegura que coincida con tu PK
            if (exito)
            {
                await _auditoriaService.RegistrarAccionAsync(
                accion: "CREAR",
                modulo: "Bitácora de Actividades",
                detalles: $"Se restauró el documento: {documento.NombreArchivo} del beneficiario {documento.Beneficiario.NombreCompleto}"
            );

                ListaDocumentosPapelera.Remove(documento);
            }
        }

        [RelayCommand]
        private async Task EliminarDocumentoPermanenteAsync(DocumentoAdjunto documento)
        {
            if (documento == null) return;

            if (PermisoAdmin == false)
            {
                MessageBox.Show("No tienes permisos para eliminar documentos de forma permanente.", "Permiso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var resultado = MessageBox.Show($"¿Está seguro de eliminar permanentemente el archivo '{documento.NombreArchivo}'?",
                "Confirmar Eliminación Definitiva", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                bool exito = await _intervencionService.EliminarDocumentoPermanenteAsync(documento.Id_Documento);
                if (exito)
                {
                    await _auditoriaService.RegistrarAccionAsync(
                accion: "ELIMINAR",
                modulo: "Bitácora de Actividades",
                detalles: $"Se eliminó el documento: {documento.NombreArchivo} del beneficiario {documento.Beneficiario.NombreCompleto}"
            );

                    ListaDocumentosPapelera.Remove(documento);
                }
            }
        }

        #endregion

        #region Acciones: Beneficiarios (Estructura lista para cuando crees tus métodos)

        [RelayCommand]
        private async Task RestaurarBeneficiarioAsync(Beneficiario beneficiario)
        {
            if (beneficiario == null) return;

            bool exito = await _intervencionService.RestaurarBeneficiarioAsync(beneficiario.Id_Beneficiario);
            if (exito) 
            {
                await _auditoriaService.RegistrarAccionAsync(
                accion: "CREAR",
                modulo: "Bitácora de Actividades",
                detalles: $"Se restauró el beneficiario: {beneficiario.Nombres} {beneficiario.Apellidos}"
            );

                ListaBeneficiariosPapelera.Remove(beneficiario); 
            }

        }

        [RelayCommand]
        private async Task EliminarBeneficiarioPermanenteAsync(Beneficiario beneficiario)
        {
            if (beneficiario == null) return;

            if (PermisoAdmin == false)
            {
                MessageBox.Show("No tienes permisos para eliminar beneficiarios de forma permanente.", "Permiso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var resultado = MessageBox.Show($"¡ALERTA MÁXIMA!\n¿Está seguro de eliminar permanentemente al beneficiario {beneficiario.Nombres}? Se borrará todo su historial clínico, legal y evoluciones.",
                "Peligro Crítico", MessageBoxButton.YesNo, MessageBoxImage.Error);

            if (resultado == MessageBoxResult.Yes)
            {
                bool exito = await _intervencionService.EliminarBeneficiarioPermanenteAsync(beneficiario.Id_Beneficiario);
                if (exito) 
                { 
                    await _auditoriaService.RegistrarAccionAsync(
                        accion: "ELIMINAR",
                        modulo: "Bitácora de Actividades",
                        detalles: $"Se eliminó el beneficiario: {beneficiario.Nombres} {beneficiario.Apellidos}"
                    );
                    ListaBeneficiariosPapelera.Remove(beneficiario); 
                }
            }

            await Task.CompletedTask; // Borra esto cuando uses código asíncrono real aquí
        }

        #endregion
    }
}
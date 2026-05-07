using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Models;
using TEST_IDENNA.Services;

namespace TEST_IDENNA.ViewModels
{
    public partial class BitacoraViewModel(IIntervencionService service) : ObservableObject
    {
        private readonly IIntervencionService _service = service;

        [ObservableProperty]
        private ObservableCollection<Actividad> _listaActividades = new();

        [ObservableProperty]
        private ObservableCollection<Evolucion> _historialCompleto = new();

        [ObservableProperty]
        private string _nombreNuevaActividad;

        [ObservableProperty]
        private string _areaSeleccionada;

        [RelayCommand]
        public async Task CargarDatos()
        {
            var actividades = await _service.ObtenerTodasLasActividades();
            ListaActividades = new ObservableCollection<Actividad>(actividades);

            var historial = await _service.ObtenerHistorialGlobal();
            HistorialCompleto = new ObservableCollection<Evolucion>(historial);
        }

        [RelayCommand]
        public async Task GuardarActividad()
        {
            if (string.IsNullOrWhiteSpace(NombreNuevaActividad) || string.IsNullOrWhiteSpace(AreaSeleccionada)) return;

            await _service.CrearNuevaActividad(NombreNuevaActividad, AreaSeleccionada);
            await CargarDatos(); // Refrescar lista
            NombreNuevaActividad = string.Empty;
            AreaSeleccionada = string.Empty;
        }
    }
}

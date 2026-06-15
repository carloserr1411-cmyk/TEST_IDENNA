using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using TEST_IDENNA.Data;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Models;
using TEST_IDENNA.Services;

namespace TEST_IDENNA.ViewModels
{
    public partial class GestionTutoresViewModel : ObservableObject
    {
        private readonly ITutorRepository _tutorRepository;
        private readonly IIntervencionService _service;
        private readonly IAuditoriaService _auditoriaService;

        [ObservableProperty]
        private string? _nombreInput;

        [ObservableProperty]
        private string? _cargoInput;

        [ObservableProperty]
        private string? _cedulaInput;

        [ObservableProperty]
        private bool _isActivoInput = true;

        // Variable para saber cuál tutor estamos modificando (null si estamos registrando uno nuevo)
        private Tutores? _tutorEnEdicion;

        [ObservableProperty]
        private string _textoBotonFormulario = "AÑADIR RESPONSABLE";

        // 1. Comando que se ejecuta al presionar el lápiz en la fila
        [RelayCommand]
        private async Task CargarTutorParaEditar(Tutores tutorSeleccionado)
        {
            if (tutorSeleccionado == null) return;

            // Guardamos la referencia del tutor que estamos editando
            _tutorEnEdicion = tutorSeleccionado;

            // Cargamos sus datos en los campos de entrada
            NombreInput = tutorSeleccionado.NombreCompleto;
            CargoInput = tutorSeleccionado.Cargo;
            CedulaInput = tutorSeleccionado.Cedula;
            IsActivoInput = tutorSeleccionado.IsActivo;

            // Cambiamos el texto del botón
            TextoBotonFormulario = "GUARDAR CAMBIOS";
        }

        public ObservableCollection<Tutores> Tutores { get; } = new();

        public GestionTutoresViewModel(ITutorRepository tutorRepository, IIntervencionService service, IAuditoriaService auditoriaService)
        {
            _tutorRepository = tutorRepository;
            _service = service;
            _auditoriaService = auditoriaService;

            // Cargar la lista inicial
            _ = CargarTutores();
        }

        private async Task CargarTutores()
        {
            var lista = await _tutorRepository.GetAllAsync();
            // El operador ?? asegura que si lista es null, usemos una lista vacía
            foreach (var t in lista ?? Enumerable.Empty<Tutores>())
            {
                Tutores.Add(t);
            }
        }

        // 2. Modificamos el método RegistrarTutor para que detecte si es una inserción o una edición
        [RelayCommand]
        private async Task RegistrarTutor()
        {
            if (string.IsNullOrWhiteSpace(NombreInput) || string.IsNullOrWhiteSpace(CargoInput) || string.IsNullOrWhiteSpace(CedulaInput))
                return;

            if (_tutorEnEdicion == null)
            {
                // === MODO REGISTRO NUEVO ===
                var nuevoTutor = new Tutores
                {
                    NombreCompleto = NombreInput,
                    Cargo = CargoInput,
                    Cedula = CedulaInput,
                    IsActivo = IsActivoInput
                };

                await _tutorRepository.AddAsync(nuevoTutor);

                await _auditoriaService.RegistrarAccionAsync(
                accion: "CREAR",
                modulo: "Gestión de Tutores",
                detalles: $"Se registró un nuevo tutor: {NombreInput}"
            );

                Tutores.Add(nuevoTutor);
            }
            else
            {
                // === MODO EDICIÓN ===
                _tutorEnEdicion.NombreCompleto = NombreInput;
                _tutorEnEdicion.Cargo = CargoInput;
                _tutorEnEdicion.Cedula = CedulaInput;
                _tutorEnEdicion.IsActivo = IsActivoInput;

                // Asegúrate de agregar un método UpdateAsync en tu ITutorRepository
                await _tutorRepository.UpdateAsync(_tutorEnEdicion);

                await _auditoriaService.RegistrarAccionAsync(
                accion: "MODIFICAR",
                modulo: "Gestión de Tutores",
                detalles: $"Se editó el tutor: {NombreInput}"
            );

                // Truco de WPF: Forzar la actualización visual en la tabla
                int index = Tutores.IndexOf(_tutorEnEdicion);
                if (index >= 0)
                {
                    Tutores.RemoveAt(index);
                    Tutores.Insert(index, _tutorEnEdicion);
                    //Tutores[index] = _tutorEnEdicion;
                }

                // Salir del modo edición
                _tutorEnEdicion = null;
                TextoBotonFormulario = "AÑADIR RESPONSABLE";
            }

            // Limpiar campos comunes
            NombreInput = CargoInput = CedulaInput = string.Empty;
            IsActivoInput = true;
        }
    }
}

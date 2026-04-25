using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using TEST_IDENNA.Models;

namespace TEST_IDENNA.ViewModels
{
    public partial class ExpedientesViewModel : ObservableObject
    {
        private readonly IBeneficiarioRepository _repository;

        [ObservableProperty]
        private string? _searchText;

        // Disparamos la búsqueda cada vez que cambia el texto (sin necesidad de presionar Enter)
        partial void OnSearchTextChanged(string? value)
        {
            _ = BuscarBeneficiario();
        }

        [ObservableProperty]
        private ObservableCollection<Beneficiario> _resultadosBusqueda = new();

        [ObservableProperty]
        private Beneficiario _beneficiarioActual = new();

        // La lista de evoluciones para la línea de tiempo
        [ObservableProperty]
        private ObservableCollection<Evolucion>? _listaEvoluciones;

        public ExpedientesViewModel(IBeneficiarioRepository repository)
        {
            _repository = repository;

            // --- PRUEBA DE DATOS (Hardcoded) ---
            // Para que veas el diseño sin base de datos
            BeneficiarioActual = new Beneficiario
            {
                Nombres = "JUAN MANUEL",
                Apellidos = "RODRÍGUEZ TORRES",
                Estatus_Legal = "En Seguimiento - Estable",
                // Necesitas crear propiedades Edad y Cedula en tu modelo
            };

            ListaEvoluciones = new ObservableCollection<Evolucion>
            {
                new Evolucion { Fecha_Registro = DateTime.Now.AddDays(-1), Area = "Terapia Individual", Descripcion = "Evolución: Muestra mayor apertura. I Evolución de nuestra conversar..." },
                new Evolucion { Fecha_Registro = DateTime.Now.AddDays(-2), Area = "Taller de Alfabetización Digital", Descripcion = "Evolución: Asistencia confirmada. Evolución: no onarización..." },
                // ... añade más ítems para probar el scroll
            };
        }

        // Este método se llamará cada vez que el usuario escriba
        [RelayCommand]
        public async Task BuscarBeneficiario()
        {
            if (string.IsNullOrWhiteSpace(SearchText) || SearchText.Length < 2)
            {
                ResultadosBusqueda.Clear();
                OnPropertyChanged(nameof(HayResultados));
                return;
            }

            var lista = await _repository.ObtenerPorNombre(SearchText);
            ResultadosBusqueda = new ObservableCollection<Beneficiario>(lista);
            OnPropertyChanged(nameof(HayResultados));

        }

        // Este método carga el expediente completo
        [RelayCommand]
        public void SeleccionarBeneficiario(Beneficiario seleccionado)
        {
            if (seleccionado == null) return;

            BeneficiarioActual = seleccionado;

            // Cargamos las evoluciones reales de este beneficiario desde la DB
            // Asumiendo que agregas este método a tu repositorio
            //var evos = await _repository.ObtenerEvolucionesPorBeneficiario(seleccionado.Id_Beneficiario);
            //ListaEvoluciones = new ObservableCollection<Evolucion>(evos);

            SearchText = string.Empty;
            ResultadosBusqueda.Clear();
            OnPropertyChanged(nameof(HayResultados));
        }
        /*public class IntToBooleanConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return (int)value > 0; // Ejemplo: devuelve true si el entero es mayor a 0
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }*/

        // En ExpedientesViewModel.cs
        public bool HayResultados => ResultadosBusqueda.Count > 0;


        // En el XAML:
        
    }
}

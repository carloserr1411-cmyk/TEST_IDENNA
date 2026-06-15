using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TEST_IDENNA.Data;
using TEST_IDENNA.Interfaces;
using TEST_IDENNA.Models;
using static MaterialDesignThemes.Wpf.Theme.ToolBar;

namespace TEST_IDENNA.ViewModels
{
    public partial class SeleccionarBeneficiariosViewModel : ObservableObject
    {
        private readonly IBeneficiarioRepository _repository;
        private readonly List<Beneficiario>? _preSeleccionados;

        [ObservableProperty] private ObservableCollection<Beneficiario> _seleccionadosFijados = new();

        [ObservableProperty] private string _filtroTexto = string.Empty;
        [ObservableProperty] private ObservableCollection<SeleccionItemWrapper> _items = new();

        public SeleccionarBeneficiariosViewModel(IBeneficiarioRepository repository, List<Beneficiario> preSeleccionados)
        {
            _repository = repository;
            // Cargamos los beneficiarios que ya venían asociados (si los hay)
            foreach (var b in preSeleccionados)
            {
                SeleccionadosFijados.Add(b);
            }
            _ = CargarTodosLosBeneficiariosAsync();
        }

        [RelayCommand]
        public async Task CargarTodosLosBeneficiariosAsync()
        {
            using (var db = new AppDbContext())
            {
                IQueryable<Beneficiario> query = db.Beneficiarios;

                // REQUERIMIENTO: Búsqueda inteligente por coincidencia en múltiples campos
                if (!string.IsNullOrEmpty(FiltroTexto))
                {
                    string filtro = FiltroTexto.Trim().ToLower();
                    query = query.Where(b => b.Nombres.ToLower().Contains(filtro) ||
                                             b.Apellidos.ToLower().Contains(filtro) ||
                                             b.Cedula.Contains(filtro));
                }

                var lista = await EntityFrameworkQueryableExtensions.ToListAsync(query);

                Items.Clear();
                foreach (var b in lista)
                {
                    // Comprobamos si el elemento ya está guardado en la lista de permanencia
                    bool yaFijado = SeleccionadosFijados.Any(p => p.Id_Beneficiario == b.Id_Beneficiario);

                    // Instanciamos el wrapper inyectándole la acción de escucha reactiva
                    Items.Add(new SeleccionItemWrapper(b, yaFijado, (beneficiario, isChecked) =>
                    {
                        SincronizarListaFijados(beneficiario, isChecked);
                    }));
                }
            }
        }

        // Agrega o remueve elementos de la lista persistente según los clics del DataGrid
        private void SincronizarListaFijados(Beneficiario beneficiario, bool isChecked)
        {
            var existente = SeleccionadosFijados.FirstOrDefault(p => p.Id_Beneficiario == beneficiario.Id_Beneficiario);

            if (isChecked && existente == null)
            {
                SeleccionadosFijados.Add(beneficiario);
            }
            else if (!isChecked && existente != null)
            {
                SeleccionadosFijados.Remove(existente);
            }
        }

        // REQUERIMIENTO: Permite eliminar un fijado directamente haciendo clic en su etiqueta visual
        [RelayCommand]
        private void QuitarFijado(Beneficiario beneficiario)
        {
            if (beneficiario == null) return;

            var existente = SeleccionadosFijados.FirstOrDefault(p => p.Id_Beneficiario == beneficiario.Id_Beneficiario);
            if (existente != null)
            {
                SeleccionadosFijados.Remove(existente);
            }

            // Si el beneficiario quitado está actualmente visible en la tabla de búsqueda, desmarcamos su Checkbox
            var wrapperEnTabla = Items.FirstOrDefault(i => i.Beneficiario.Id_Beneficiario == beneficiario.Id_Beneficiario);
            if (wrapperEnTabla != null)
            {
                wrapperEnTabla.IsSelected = false;
            }
        }

        // Lógica de búsqueda reactiva al escribir
        partial void OnFiltroTextoChanged(string value)
        {
            _ = CargarTodosLosBeneficiariosAsync();
        }

        public List<Beneficiario> ObtenerSeleccionados()
        {
            return SeleccionadosFijados.ToList();
        }
    }

    // Wrapper optimizado con callback de estado
    public partial class SeleccionItemWrapper : ObservableObject
    {
        private readonly Action<Beneficiario, bool> _onSelectionChanged;
        public Beneficiario Beneficiario { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    _onSelectionChanged?.Invoke(Beneficiario, value);
                }
            }
        }

        public SeleccionItemWrapper(Beneficiario beneficiario, bool isSelected, Action<Beneficiario, bool> onSelectionChanged)
        {
            Beneficiario = beneficiario;
            _isSelected = isSelected;
            _onSelectionChanged = onSelectionChanged;
        }
    }
}

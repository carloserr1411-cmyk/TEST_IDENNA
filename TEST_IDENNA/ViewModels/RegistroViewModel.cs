using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Xaml.Behaviors.Core;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TEST_IDENNA.Data;
using TEST_IDENNA.Models;

namespace TEST_IDENNA.ViewModels
{
    public partial class RegistroViewModel : ObservableObject
    {
        private string nombreUsuario;
        public string NombreUsuario
        {
            get => nombreUsuario;
            set => SetProperty(ref nombreUsuario, value);
        }

        private ActionCommand registrarCommand;
        public ICommand RegistrarCommand
        {
            get
            {
                if (registrarCommand == null)
                {
                    registrarCommand = new ActionCommand(Registrar);
                }

                return registrarCommand;
            }
        }

        //[RelayCommand]
        public void Registrar()
        {
            if (!string.IsNullOrWhiteSpace(NombreUsuario))
            {
                DatabaseConfig.GuardarNino(NombreUsuario);
                CargarDatos(); // Refresca la tabla
                MessageBox.Show($"¡{NombreUsuario} ha sido registrado en la base de datos!");
                NombreUsuario = string.Empty; // Limpiar el campo
            }
            else
            {
                MessageBox.Show("Por favor, ingresa un nombre.");
            }
        }

        // Inicialización movida al constructor del ViewModel
        public RegistroViewModel()
        {
            Batteries_V2.Init();                // <-- inicializa el proveedor nativo
            DatabaseConfig.InitializeDatabase();
            CargarDatos();
        }

        //[ObservableProperty]
        private ObservableCollection<Nino> _listaNinos;

        public ObservableCollection<Nino> ListaNinos
        {
            get => _listaNinos;
            set => SetProperty(ref _listaNinos, value);
        }

        public void CargarDatos()
        {
            ListaNinos = new ObservableCollection<Nino>(DatabaseConfig.ObtenerTodos());
        }

        //[ObservableProperty]
        private object _currentView;

        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        //[RelayCommand]
        /*public void ShowExpedientes()
        {
            CurrentView = new ExpedientesView(); // Al cambiar esto, la interfaz cambia sola
        }*/

        private ActionCommand showExpedientes;

        public ICommand ShowExpedientes
        {
            get
            {
                if (showExpedientes == null)
                {
                    showExpedientes = new ActionCommand(PerformShowExpedientes);
                }

                return showExpedientes;
            }
        }

        private void PerformShowExpedientes()
        {
            CurrentView = new ExpedientesView();
        }

        private ActionCommand showDashboard;

        public ICommand ShowDashboard
        {
            get
            {
                if (showDashboard == null)
                {
                    showDashboard = new ActionCommand(PerformShowDashboard);
                }

                return showDashboard;
            }
        }

        private void PerformShowDashboard()
        {
            CurrentView = new DashboardView();
        }

        private ActionCommand showBitacora;

        public ICommand ShowBitacora
            {
            get
            {
                if (showBitacora == null)
                {
                    showBitacora = new ActionCommand(PerformShowBitacora);
                }
                return showBitacora;
            }
        }

        private void PerformShowBitacora()
        {
            CurrentView = new BitacoraView();
        }
    }
}

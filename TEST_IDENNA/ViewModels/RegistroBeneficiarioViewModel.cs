using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TEST_IDENNA.Models;
using TEST_IDENNA.Services;

namespace TEST_IDENNA.ViewModels
{
    public partial class RegistroBeneficiarioViewModel(IIntervencionService service) : ObservableObject
    {
        private readonly IIntervencionService _service = service;

        [ObservableProperty]
        private Beneficiario _nuevoBeneficiario = new()
        {
            Fecha_Nacimiento = DateTime.Today.AddYears(-12), // Fecha promedio
            Fecha_Ingreso = DateTime.Now,
            Estatus_Legal = "En Proceso de Investigación",
            Observaciones = string.Empty
        };

        [RelayCommand]
        private async Task Registrar()
        {
            // Llamamos al servicio de negocio
            bool exito = await _service.RegistrarNuevoIngreso(NuevoBeneficiario);

            if (exito)
            {
                // Limpiar formulario o mostrar mensaje
                //NuevoBeneficiario = new Beneficiario();
                NuevoBeneficiario = new Beneficiario
                {
                    Fecha_Nacimiento = DateTime.Today.AddYears(-12), // Fecha promedio
                    Fecha_Ingreso = DateTime.Now,
                    Estatus_Legal = "En Proceso de Investigación",
                    Observaciones = string.Empty
                };
            }
        }

        [RelayCommand]
        private async Task Cancelar()
        {
                // Lógica para cancelar el registro, por ejemplo, limpiar el formulario
                NuevoBeneficiario = new Beneficiario
                {
                    Fecha_Nacimiento = DateTime.Today.AddYears(-12), // Fecha promedio
                    Fecha_Ingreso = DateTime.Now,
                    Estatus_Legal = "En Proceso de Investigación",
                    Observaciones = string.Empty
                };
        }
    }
}

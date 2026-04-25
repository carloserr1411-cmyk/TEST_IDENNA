using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace TEST_IDENNA.ViewModels
{
    public partial class BitacoraViewModel(IIntervencionService service) : ObservableObject
    {
        private readonly IIntervencionService _service = service;
    }
}

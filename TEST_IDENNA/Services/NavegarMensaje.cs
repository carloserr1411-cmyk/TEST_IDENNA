using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace TEST_IDENNA.Services
{
    public class NavegarMensaje(object viewModel) : ValueChangedMessage<object>(viewModel)
    {
    }
}

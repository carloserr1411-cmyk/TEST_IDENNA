using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace TEST_IDENNA.Messages
{
    public class TemaCambiadoMensaje : ValueChangedMessage<bool>
    {
        public TemaCambiadoMensaje(bool esModoOscuro) : base(esModoOscuro)
        {
        }
    }
}

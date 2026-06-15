using System;
using System.Collections.Generic;
using System.Text;
using TEST_IDENNA.Models;

namespace TEST_IDENNA
{
    public static class SesionSistema
    {
        // Aquí guardaremos al usuario que logre iniciar sesión
        public static Usuario? UsuarioActual { get; set; }

        public static bool IsLoggedIn => UsuarioActual != null;

        // Helpers directos para los permisos
        public static bool EsAdmin => UsuarioActual?.Rol == "Administrador";
        public static bool EsUsuarioComun => UsuarioActual?.Rol == "UsuarioComun";
    }
}

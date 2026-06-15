using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace TEST_IDENNA.Helpers
{
    public class FileToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string nombreArchivo = value as string;
            if (string.IsNullOrEmpty(nombreArchivo))
                return Visibility.Collapsed;

            string extension = Path.GetExtension(nombreArchivo).ToLower();
            string tipoDeseado = parameter as string; // Recibe "Imagen" o "Documento"

            bool esImagen = extension == ".jpg" || extension == ".jpeg" || extension == ".png";
            bool esDocumento = extension == ".pdf" || extension == ".docx" || extension == ".doc";

            if (tipoDeseado == "Imagen")
                return esImagen ? Visibility.Visible : Visibility.Collapsed;

            if (tipoDeseado == "Documento")
                return esDocumento ? Visibility.Visible : Visibility.Collapsed;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
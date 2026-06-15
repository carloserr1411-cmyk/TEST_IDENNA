using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace TEST_IDENNA.Services
{
    public class BytesToImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is byte[] bytes) || bytes.Length == 0)
            {
                return null; // Devuelve null si no hay datos
            }

            try
            {
                using var stream = new System.IO.MemoryStream(bytes);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit(); // Si no es imagen, saltará al catch
                return image;
            }
            catch (Exception)
            {
                // Opción A: Devolver null para que el control Image quede vacío
                // return null;

                // Opción B: Devolver un icono genérico de "Documento" o "Word" desde tus Assets
                try
                {
                    return new BitmapImage(new Uri("pack://application:,,,/Assets/document_icon.png", UriKind.Absolute));
                }
                catch
                {
                    return null; // Por si acaso el icono no existe en la ruta
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
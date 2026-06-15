using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using MaterialDesignThemes.Wpf; // <-- Importante para reconocer PackIconKind
using System.Runtime.Versioning; // <- añadido

namespace TEST_IDENNA.Helpers
{
    [SupportedOSPlatform("windows")] // <- añadido
    public class ExtensionToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string nombreArchivo = value as string;
            if (string.IsNullOrEmpty(nombreArchivo))
                return PackIconKind.FileDocument; // Ícono por defecto

            string extension = Path.GetExtension(nombreArchivo).ToLower();

            switch (extension)
            {
                case ".pdf":
                    return PackIconKind.FilePdfBox; // Ícono de PDF con caja (o usa PackIconKind.FilePdf)
                case ".doc":
                case ".docx":
                    return PackIconKind.FileWordBox; // Ícono de Word
                case ".jpg":
                case ".jpeg":
                case ".png":
                    return PackIconKind.Image; // Ícono de imagen por si acaso
                default:
                    return PackIconKind.FileDocument; // Genérico
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
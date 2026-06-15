using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.IO;
using Tesseract;

namespace TEST_IDENNA.Services
{
    public class OcrService
    {
        private readonly string _tessdataPath;

        public OcrService()
        {
            // Apunta a la carpeta del directorio de ejecución donde se copió tessdata
            _tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
        }

        public string ExtraerTextoDeImagen(string rutaImagen)
        {
            try
            {
                // "spa" indica que usará el archivo spa.traineddata
                using (var engine = new TesseractEngine(_tessdataPath, "spa", EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(rutaImagen))
                    {
                        using (var page = engine.Process(img))
                        {
                            return page.GetText();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en OCR: {ex.Message}");
                return string.Empty;
            }
        }
    }
}

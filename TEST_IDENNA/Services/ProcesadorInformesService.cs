using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TEST_IDENNA.Models;
using TEST_IDENNA.Interfaces;

namespace TEST_IDENNA.Services
{
    public class ProcesadorInformesService
    {
        private readonly OcrService _ocrService;
        private readonly IBeneficiarioRepository _repository;

        public ProcesadorInformesService(OcrService ocrService, IBeneficiarioRepository repository)
        {
            _ocrService = ocrService;
            _repository = repository;
        }

        public async Task<Beneficiario?> ProcesarYVincularInformeAsync(string rutaImagen)
        {
            // 1. Extraer el texto del informe escaneado
            string textoExtraido = _ocrService.ExtraerTextoDeImagen(rutaImagen);

            if (string.IsNullOrWhiteSpace(textoExtraido)) return null;

            // 2. Buscar un patrón (Por ejemplo, una cédula venezolana: V-12345678 o solo los números)
            // Este regex busca secuencias de 7 u 8 dígitos consecutivos
            var coincidenciaCedula = Regex.Match(textoExtraido, @"\b\d{7,8}\b");

            if (coincidenciaCedula.Success)
            {
                string cedulaEncontrada = coincidenciaCedula.Value;

                // 3. Buscar en la base de datos de SQLite por esa cédula
                // (Asumiendo que tienes un método similar en tu repositorio)
                var beneficiario = await _repository.ObtenerPorCedulaAsync(cedulaEncontrada);

                if (beneficiario != null)
                {
                    return beneficiario; // ¡Enlazado con éxito!
                }
            }

            // Estrategia B: Si no hay cédula, buscar por los nombres que aparezcan en las primeras líneas
            // Aquí podrías procesar el texto buscando coincidencias de nombres de tu DB,
            // aunque el ID numérico/Cédula siempre es el método más seguro.

            return null;
        }
    }
}
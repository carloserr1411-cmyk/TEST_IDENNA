using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace TEST_IDENNA.Views
{
    public partial class RecortarImagenView : Window
    {
        private Point _anchorPoint;
        private bool _isDragging = false;
        private bool _isInternalUpdate = false;
        public byte[] ImagenRecortadaBytes { get; private set; }

        public RecortarImagenView(BitmapImage source)
        {
            InitializeComponent();
            ImagenOriginal.Source = source;
        }

        private Rect ObtenerLimitesImagen()
        {
            var fuente = ImagenOriginal.Source as BitmapSource;
            if (fuente == null) return new Rect(0, 0, CanvasRecorte.ActualWidth, CanvasRecorte.ActualHeight);

            // Calculamos el factor de escala que WPF aplica internamente por "Uniform"
            double ratioX = CanvasRecorte.ActualWidth / fuente.PixelWidth;
            double ratioY = CanvasRecorte.ActualHeight / fuente.PixelHeight;
            double escala = Math.Min(ratioX, ratioY); // Uniform usa el menor para no recortar

            // Dimensiones reales de la imagen en pantalla
            double anchoReal = fuente.PixelWidth * escala;
            double altoReal = fuente.PixelHeight * escala;

            // Posición (X, Y) donde empieza la imagen (para centrarla)
            double izquierda = (CanvasRecorte.ActualWidth - anchoReal) / 2;
            double arriba = (CanvasRecorte.ActualHeight - altoReal) / 2;

            return new Rect(izquierda, arriba, anchoReal, altoReal);
        }

        // 1. Cuando el usuario mueve el Slider
        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInternalUpdate || Selector == null) return;
            ActualizarPosicionSelector(e.NewValue);
        }

        // 3. Método centralizado para redimensionar y mantener centrado
        private void ActualizarPosicionSelector(double nuevoTamaño)
        {
            Rect limites = ObtenerLimitesImagen();

            // El círculo no puede ser más grande que el lado más corto de la imagen
            double tamañoMaximo = Math.Min(limites.Width, limites.Height);
            if (nuevoTamaño > tamañoMaximo) nuevoTamaño = tamañoMaximo;

            double diferencia = nuevoTamaño - Selector.Width;
            double nuevoLeft = Canvas.GetLeft(Selector) - (diferencia / 2);
            double nuevoTop = Canvas.GetTop(Selector) - (diferencia / 2);

            // Validar que al redimensionar no se salga de los límites de la IMAGEN
            if (nuevoLeft < limites.Left) nuevoLeft = limites.Left;
            if (nuevoTop < limites.Top) nuevoTop = limites.Top;

            if (nuevoLeft + nuevoTamaño > limites.Right)
                nuevoLeft = limites.Right - nuevoTamaño;
            if (nuevoTop + nuevoTamaño > limites.Bottom)
                nuevoTop = limites.Bottom - nuevoTamaño;

            Selector.Width = nuevoTamaño;
            Selector.Height = nuevoTamaño;
            Canvas.SetLeft(Selector, nuevoLeft);
            Canvas.SetTop(Selector, nuevoTop);

            // Sincronizar Slider si es necesario
            if (ZoomSlider.Value != nuevoTamaño)
            {
                _isInternalUpdate = true;
                ZoomSlider.Value = nuevoTamaño;
                _isInternalUpdate = false;
            }
        }

        private void CanvasRecorte_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double paso = 15;
            double nuevoValor = ZoomSlider.Value + (e.Delta > 0 ? paso : -paso);
            // Al cambiar el valor del Slider, se dispara automáticamente ZoomSlider_ValueChanged
            ZoomSlider.Value = Math.Clamp(nuevoValor, ZoomSlider.Minimum, ZoomSlider.Maximum);

            double cambioEscala = 10; // Cuántos píxeles crece/encoge por cada "clic" de rueda
            double nuevoTamaño;

            if (e.Delta > 0) // Hacia arriba: Agrandar círculo (Alejar recorte)
            {
                nuevoTamaño = Selector.Width + cambioEscala;
            }
            else // Hacia abajo: Achicar círculo (Acercar recorte)
            {
                nuevoTamaño = Selector.Width - cambioEscala;
            }

            // 1. Limitar el tamaño (mínimo 50px, máximo el tamaño del canvas)
            double maxTamaño = Math.Min(ImagenOriginal.ActualWidth, ImagenOriginal.ActualHeight);
            if (nuevoTamaño < 50) nuevoTamaño = 50;
            if (nuevoTamaño > maxTamaño) nuevoTamaño = maxTamaño;

            // 2. Calcular la diferencia de tamaño para re-centrar
            double diferencia = nuevoTamaño - Selector.Width;

            // 3. Ajustar posición para que escale desde el centro
            double nuevoLeft = Canvas.GetLeft(Selector) - (diferencia / 2);
            double nuevoTop = Canvas.GetTop(Selector) - (diferencia / 2);

            // 4. Validar que al crecer no se salga de los bordes
            if (nuevoLeft < 0) nuevoLeft = 0;
            if (nuevoTop < 0) nuevoTop = 0;
            if (nuevoLeft + nuevoTamaño > CanvasRecorte.ActualWidth)
                nuevoLeft = CanvasRecorte.ActualWidth - nuevoTamaño;
            if (nuevoTop + nuevoTamaño > CanvasRecorte.ActualHeight)
                nuevoTop = CanvasRecorte.ActualHeight - nuevoTamaño;

            // 5. Aplicar cambios
            Selector.Width = nuevoTamaño;
            Selector.Height = nuevoTamaño;
            Canvas.SetLeft(Selector, nuevoLeft);
            Canvas.SetTop(Selector, nuevoTop);
        }

        // 1. Centrar el círculo al cargar la ventana
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            double maxPossible = Math.Min(ImagenOriginal.ActualWidth, ImagenOriginal.ActualHeight);
            ZoomSlider.Maximum = maxPossible;
            ZoomSlider.Value = 200; // Tamaño inicial

            // Centrar inicialmente
            ActualizarPosicionSelector(200);

            double left = (CanvasRecorte.ActualWidth - Selector.Width) / 2;
            double top = (CanvasRecorte.ActualHeight - Selector.Height) / 2;

            Canvas.SetLeft(Selector, left);
            Canvas.SetTop(Selector, top);
        }

        private void Selector_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            // Guardamos la posición del clic relativa al propio selector
            _anchorPoint = e.GetPosition(Selector);
            Selector.CaptureMouse();
        }

        private void Selector_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            Point p = e.GetPosition(CanvasRecorte);
            double newX = p.X - _anchorPoint.X;
            double newY = p.Y - _anchorPoint.Y;

            // --- NUEVA LÓGICA DE LÍMITES ---
            Rect limites = ObtenerLimitesImagen();

            // Restricción Horizontal
            if (newX < limites.Left) newX = limites.Left;
            if (newX + Selector.Width > limites.Right)
                newX = limites.Right - Selector.Width;

            // Restricción Vertical
            if (newY < limites.Top) newY = limites.Top;
            if (newY + Selector.Height > limites.Bottom)
                newY = limites.Bottom - Selector.Height;

            Canvas.SetLeft(Selector, newX);
            Canvas.SetTop(Selector, newY);
        }

        private void Selector_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            Selector.ReleaseMouseCapture();
        }

        private void Aceptar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BitmapSource source = (BitmapSource)ImagenOriginal.Source;

                // IMPORTANTE: Como la imagen está en modo "Uniform", hay que calcular
                // el factor de escala real entre el control Image y el archivo original.
                double scaleX = source.PixelWidth / ImagenOriginal.ActualWidth;
                double scaleY = source.PixelHeight / ImagenOriginal.ActualHeight;

                // Usamos el factor de escala que sea mayor para mantener la proporción Uniform
                double scale = Math.Max(scaleX, scaleY);

                // Calculamos el offset si la imagen no llena todo el canvas (bordes negros)
                double imageDisplayedWidth = source.PixelWidth / scale;
                double imageDisplayedHeight = source.PixelHeight / scale;
                double offsetX = (CanvasRecorte.ActualWidth - imageDisplayedWidth) / 2;
                double offsetY = (CanvasRecorte.ActualHeight - imageDisplayedHeight) / 2;

                int x = (int)((Canvas.GetLeft(Selector) - offsetX) * scale);
                int y = (int)((Canvas.GetTop(Selector) - offsetY) * scale);
                int size = (int)(Selector.Width * scale);

                // Validaciones de seguridad para el recorte
                x = Math.Max(0, x);
                y = Math.Max(0, y);
                if (x + size > source.PixelWidth) size = source.PixelWidth - x;
                if (y + size > source.PixelHeight) size = source.PixelHeight - y;

                CroppedBitmap cb = new CroppedBitmap(source, new Int32Rect(x, y, size, size));

                // Convertir a bytes para el ViewModel
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(cb));
                using (var ms = new System.IO.MemoryStream())
                {
                    encoder.Save(ms);
                    ImagenRecortadaBytes = ms.ToArray();
                }

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Asegúrate de que el círculo esté sobre la imagen.");
            }
        }
    }
}
using System.Windows;
using System.Windows.Input;

namespace TEST_IDENNA.Views.Dialogs
{
    public partial class ExitoDialog : Window
    {
        // Constructor que recibe el mensaje
        public ExitoDialog(string mensaje)
        {
            InitializeComponent();
            TxtMensaje.Text = mensaje;

            // Opcional: Hacer que se cierre sola tras 3 segundos
            // IniciarTemporizadorCierre();
        }

        private void Aceptar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        // Permite arrastrar la ventana si haces clic en cualquier parte del fondo
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}
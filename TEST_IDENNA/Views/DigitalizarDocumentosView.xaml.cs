using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TEST_IDENNA.ViewModels;

namespace TEST_IDENNA.Views
{
    /// <summary>
    /// Lógica de interacción para DigitalizarDocumentosView.xaml
    /// </summary>
    public partial class DigitalizarDocumentosView : UserControl
    {
        public DigitalizarDocumentosView()
        {
            InitializeComponent();
        }

        private void Card_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 1. Configurar el cuadro de diálogo para imágenes
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Archivos de Imagen (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|Archivos de Documento (*.docx;*.pdf;*.txt)|*.docx;*.pdf;*.txt",
                Title = "Seleccionar Ficha o Cédula para Digitalizar"
            };

            // 2. Si el usuario selecciona un archivo válido
            if (openFileDialog.ShowDialog() == true)
            {
                // 3. Corregido: Se separó el 'is' y se simplificó la validación del DataContext
                if (this.DataContext is DigitalizarDocumentosViewModel viewModel)
                {
                    // Ejecutamos el comando generado por el CommunityToolkit
                    viewModel.ProcesarDocumentoCommand.Execute(openFileDialog.FileName);
                }
            }
        }

        // Permite que el cursor cambie al icono de "copiar/soltar" cuando pasa un archivo por encima
        private void Card_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        // Captura el archivo soltado y se lo pasa al ViewModel
        private void Card_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] archivos = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (archivos != null && archivos.Length > 0)
                {
                    string rutaArchivo = archivos[0]; // Tomamos el primer archivo

                    // Obtenemos el DataContext (tu ViewModel) y ejecutamos el comando
                    if (DataContext is DigitalizarDocumentosViewModel viewModel)
                    {
                        if (viewModel.ProcesarDocumentoCommand.CanExecute(rutaArchivo))
                        {
                            viewModel.ProcesarDocumentoCommand.Execute(rutaArchivo);
                        }
                    }
                }
            }
        }
    }
}
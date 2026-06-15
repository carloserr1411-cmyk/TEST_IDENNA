using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace TEST_IDENNA.Services
{
    public static class HighlightService
    {
        public static readonly DependencyProperty HighlightTextProperty =
            DependencyProperty.RegisterAttached("HighlightText", typeof(string), typeof(HighlightService), new PropertyMetadata(string.Empty, OnHighlightChanged));

        public static readonly DependencyProperty FullTextProperty =
            DependencyProperty.RegisterAttached("FullText", typeof(string), typeof(HighlightService), new PropertyMetadata(string.Empty, OnHighlightChanged));

        // NUEVA: Para controlar el color de la letra resaltada desde XAML
        public static readonly DependencyProperty HighlightForegroundProperty =
            DependencyProperty.RegisterAttached("HighlightForeground", typeof(Brush), typeof(HighlightService), new PropertyMetadata(null, OnHighlightChanged));

        // NUEVA: Para controlar el sombreado de fondo (Background) desde XAML
        public static readonly DependencyProperty HighlightBackgroundProperty =
            DependencyProperty.RegisterAttached("HighlightBackground", typeof(Brush), typeof(HighlightService), new PropertyMetadata(null, OnHighlightChanged));

        public static string GetHighlightText(DependencyObject obj) => (string)obj.GetValue(HighlightTextProperty);
        public static void SetHighlightText(DependencyObject obj, string value) => obj.SetValue(HighlightTextProperty, value);

        public static string GetFullText(DependencyObject obj) => (string)obj.GetValue(FullTextProperty);
        public static void SetFullText(DependencyObject obj, string value) => obj.SetValue(FullTextProperty, value);

        public static Brush GetHighlightForeground(DependencyObject obj) => (Brush)obj.GetValue(HighlightForegroundProperty);
        public static void SetHighlightForeground(DependencyObject obj, Brush value) => obj.SetValue(HighlightForegroundProperty, value);

        public static Brush GetHighlightBackground(DependencyObject obj) => (Brush)obj.GetValue(HighlightBackgroundProperty);
        public static void SetHighlightBackground(DependencyObject obj, Brush value) => obj.SetValue(HighlightBackgroundProperty, value);

        private static void OnHighlightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock tb)
            {
                string fullText = GetFullText(tb);
                string highlight = GetHighlightText(tb);

                tb.Inlines.Clear();
                if (string.IsNullOrWhiteSpace(highlight) || string.IsNullOrWhiteSpace(fullText))
                {
                    tb.Text = fullText;
                    return;
                }

                int index = fullText.IndexOf(highlight, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    tb.Text = fullText;
                }
                else
                {
                    // Parte antes de la coincidencia
                    tb.Inlines.Add(new Run(fullText.Substring(0, index)));

                    // Recuperar los pinceles asignados por el usuario (o usar valores por defecto si no se definen)
                    Brush fgBrush = GetHighlightForeground(tb) ?? (Brush)new BrushConverter().ConvertFromString("#0b103d");
                    Brush bgBrush = GetHighlightBackground(tb); // Si es null, no aplicará fondo

                    // Parte coincidente (en NEGRITA y con estilos dinámicos)
                    Run highlightedRun = new Run(fullText.Substring(index, highlight.Length))
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = fgBrush
                    };

                    // Aplicar sombreado de fondo solo si se configuró en el XAML
                    if (bgBrush != null)
                    {
                        highlightedRun.Background = bgBrush;
                    }

                    tb.Inlines.Add(highlightedRun);

                    // Parte después de la coincidencia
                    tb.Inlines.Add(new Run(fullText.Substring(index + highlight.Length)));
                }
            }
        }
    }
}
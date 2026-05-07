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

        public static string GetHighlightText(DependencyObject obj) => (string)obj.GetValue(HighlightTextProperty);
        public static void SetHighlightText(DependencyObject obj, string value) => obj.SetValue(HighlightTextProperty, value);

        public static string GetFullText(DependencyObject obj) => (string)obj.GetValue(FullTextProperty);
        public static void SetFullText(DependencyObject obj, string value) => obj.SetValue(FullTextProperty, value);

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
                    // Parte coincidente (en NEGRITA)
                    tb.Inlines.Add(new Run(fullText.Substring(index, highlight.Length))
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = (Brush)new BrushConverter().ConvertFromString("#0b103d") // Tu color azul oscuro
                    });
                    // Parte después de la coincidencia
                    tb.Inlines.Add(new Run(fullText.Substring(index + highlight.Length)));
                }
            }
        }
    }
}
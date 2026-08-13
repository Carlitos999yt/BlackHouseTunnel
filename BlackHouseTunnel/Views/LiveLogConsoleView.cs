using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using BlackHouseTunnel.Services;

namespace BlackHouseTunnel.Views
{
    public class LiveLogConsoleView : UserControl
    {
        private readonly RichTextBox _rtb;

        public LiveLogConsoleView(double height = 220)
        {
            Grid grid = new Grid();

            Border border = new Border
            {
                Background = ThemeManager.InputBgBrush,
                BorderBrush = ThemeManager.InputBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Height = height
            };

            _rtb = new RichTextBox
            {
                Background = Brushes.Transparent,
                Foreground = ThemeManager.TextPrimaryBrush,
                FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                FontSize = 12,
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10)
            };

            border.Child = _rtb;
            grid.Children.Add(border);

            this.Content = grid;
        }

        public void AppendLog(string message, string tag = "")
        {
            Dispatcher.Invoke(() =>
            {
                Paragraph p = new Paragraph { Margin = new Thickness(0, 0, 0, 2) };

                string timeStr = $"[{DateTime.Now:HH:mm:ss}]  ";
                Run timeRun = new Run(timeStr)
                {
                    Foreground = ThemeManager.TextMutedBrush
                };
                p.Inlines.Add(timeRun);

                Color textColor = tag switch
                {
                    "ok" => (Color)ColorConverter.ConvertFromString("#10B981"),
                    "err" => (Color)ColorConverter.ConvertFromString("#ED4245"),
                    "warn" => (Color)ColorConverter.ConvertFromString("#F59E0B"),
                    "info" => (Color)ColorConverter.ConvertFromString("#5865F2"),
                    "dim" => (Color)ColorConverter.ConvertFromString("#8E9297"),
                    _ => ThemeManager.TextPrimaryBrush.Color
                };

                Run msgRun = new Run(message)
                {
                    Foreground = new SolidColorBrush(textColor)
                };
                p.Inlines.Add(msgRun);

                _rtb.Document.Blocks.Add(p);
                _rtb.ScrollToEnd();
            });
        }

        public void Clear()
        {
            Dispatcher.Invoke(() =>
            {
                _rtb.Document.Blocks.Clear();
            });
        }

        public string GetText()
        {
            TextRange textRange = new TextRange(_rtb.Document.ContentStart, _rtb.Document.ContentEnd);
            return textRange.Text;
        }
    }
}

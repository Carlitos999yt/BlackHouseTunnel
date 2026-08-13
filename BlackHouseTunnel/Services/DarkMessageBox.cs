using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BlackHouseTunnel.Services
{
    public static class DarkMessageBox
    {
        public static MessageBoxResult Show(string messageBoxText, string caption = "BlackHouse Tunnel", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information)
        {
            MessageBoxResult result = MessageBoxResult.OK;

            Window win = new Window
            {
                Title = caption,
                Width = 460,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false
            };

            Border card = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0C0C16")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(24),
                Margin = new Thickness(10),
                Effect = new DropShadowEffect
                {
                    Color = (Color)ColorConverter.ConvertFromString("#5865F2"),
                    BlurRadius = 25,
                    Opacity = 0.4,
                    ShadowDepth = 0
                }
            };

            StackPanel stack = new StackPanel();

            // Header Title
            TextBlock titleTxt = new TextBlock
            {
                Text = GetIconSymbol(icon) + " " + caption,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stack.Children.Add(titleTxt);

            // Message Body
            TextBlock bodyTxt = new TextBlock
            {
                Text = messageBoxText,
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20)
            };
            stack.Children.Add(bodyTxt);

            // Buttons Panel
            StackPanel btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            if (button == MessageBoxButton.OK)
            {
                Button okBtn = CreateDialogButton("Aceptar", "#5865F2");
                okBtn.Click += (s, e) => { result = MessageBoxResult.OK; win.Close(); };
                btnPanel.Children.Add(okBtn);
            }
            else if (button == MessageBoxButton.OKCancel)
            {
                Button okBtn = CreateDialogButton("Aceptar", "#5865F2");
                okBtn.Click += (s, e) => { result = MessageBoxResult.OK; win.Close(); };
                Button cancelBtn = CreateDialogButton("Cancelar", "#1F1F30");
                cancelBtn.Click += (s, e) => { result = MessageBoxResult.Cancel; win.Close(); };

                btnPanel.Children.Add(okBtn);
                btnPanel.Children.Add(cancelBtn);
            }
            else if (button == MessageBoxButton.YesNo || button == MessageBoxButton.YesNoCancel)
            {
                Button yesBtn = CreateDialogButton("Sí", "#10B981");
                yesBtn.Click += (s, e) => { result = MessageBoxResult.Yes; win.Close(); };
                Button noBtn = CreateDialogButton("No", "#ED4245");
                noBtn.Click += (s, e) => { result = MessageBoxResult.No; win.Close(); };

                btnPanel.Children.Add(yesBtn);
                btnPanel.Children.Add(noBtn);

                if (button == MessageBoxButton.YesNoCancel)
                {
                    Button cancelBtn = CreateDialogButton("Cancelar", "#1F1F30");
                    cancelBtn.Click += (s, e) => { result = MessageBoxResult.Cancel; win.Close(); };
                    btnPanel.Children.Add(cancelBtn);
                }
            }

            stack.Children.Add(btnPanel);
            card.Child = stack;

            win.Content = card;
            win.ShowDialog();

            return result;
        }

        private static string GetIconSymbol(MessageBoxImage icon)
        {
            return icon switch
            {
                MessageBoxImage.Error => "❌",
                MessageBoxImage.Warning => "⚠",
                MessageBoxImage.Question => "❓",
                _ => "ℹ️"
            };
        }

        private static Button CreateDialogButton(string text, string hexBg)
        {
            Button btn = new Button
            {
                Content = text,
                Height = 36,
                MinWidth = 90,
                Margin = new Thickness(6, 0, 0, 0),
                Padding = new Thickness(14, 0, 14, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexBg)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            FrameworkElementFactory presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);
            template.VisualTree = borderFactory;
            btn.Template = template;

            return btn;
        }
    }
}

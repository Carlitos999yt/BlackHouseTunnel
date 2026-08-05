using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using BlackHouseTunnel.Models;

namespace BlackHouseTunnel.Views
{
    public class AccessDeniedView : UserControl
    {
        public event EventHandler? OnRetryRequested;

        private readonly DiscordUser? _user;

        public AccessDeniedView(DiscordUser? user)
        {
            _user = user;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Grid mainGrid = new Grid
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#070709"))
            };

            Border cardBorder = new Border
            {
                Width = 520,
                Padding = new Thickness(32),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#130D11")),
                CornerRadius = new CornerRadius(16),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ED4245")), // Discord Red
                BorderThickness = new Thickness(1.5),
                Effect = new DropShadowEffect
                {
                    Color = (Color)ColorConverter.ConvertFromString("#ED4245"),
                    BlurRadius = 30,
                    Opacity = 0.3,
                    ShadowDepth = 0
                }
            };

            StackPanel panel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Warning Icon / Text
            TextBlock iconBlock = new TextBlock
            {
                Text = "🚫",
                FontSize = 48,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };

            TextBlock titleBlock = new TextBlock
            {
                Text = "Acceso Denegado",
                FontSize = 26,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };

            string username = _user != null ? _user.DisplayName : "Usuario";
            TextBlock descBlock = new TextBlock
            {
                Text = $"Hola {username}, para usar BlackHouseTunnel debes ser miembro de nuestro servidor exclusivo de Discord.\n\nServidor ID: 1529015986135502951",
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC")),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20,
                Margin = new Thickness(0, 0, 0, 24)
            };

            // Retry Button
            Button retryButton = new Button
            {
                Content = "Volver a Intentar",
                Width = 240,
                Height = 44,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            ControlTemplate btnTemplate = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            FrameworkElementFactory presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);
            btnTemplate.VisualTree = borderFactory;
            retryButton.Template = btnTemplate;

            retryButton.Click += (s, e) =>
            {
                OnRetryRequested?.Invoke(this, EventArgs.Empty);
            };

            panel.Children.Add(iconBlock);
            panel.Children.Add(titleBlock);
            panel.Children.Add(descBlock);
            panel.Children.Add(retryButton);

            cardBorder.Child = panel;
            mainGrid.Children.Add(cardBorder);

            this.Content = mainGrid;
        }
    }
}

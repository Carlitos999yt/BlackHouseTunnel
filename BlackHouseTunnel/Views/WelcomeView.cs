using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace BlackHouseTunnel.Views
{
    public class WelcomeView : UserControl
    {
        public event EventHandler? OnLoginRequested;

        private Button _loginButton = null!;
        private TextBlock _statusText = null!;
        private Border _welcomeCard = null!;

        public WelcomeView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Main Container Grid with pure sleek dark background
            Grid mainGrid = new Grid
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#040406"))
            };

            // Outer Card (Welcome Card - Deep sleek black)
            _welcomeCard = new Border
            {
                Width = 530,
                Height = 320,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#08080C")), // Deeper black
                CornerRadius = new CornerRadius(18),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D1E2C")),
                BorderThickness = new Thickness(1.5),
                Opacity = 0, // Starts 100% transparent for 0.5s animation
                Effect = new DropShadowEffect
                {
                    Color = (Color)ColorConverter.ConvertFromString("#5865F2"),
                    BlurRadius = 40,
                    Opacity = 0.35,
                    ShadowDepth = 0
                }
            };

            // Prepare RenderTransform for Slide-Up animation
            TranslateTransform slideTransform = new TranslateTransform { Y = 40 };
            _welcomeCard.RenderTransform = slideTransform;

            StackPanel cardContent = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(24)
            };

            // Gothic Stylized Headline Text ("Welcome To BlackHouse")
            TextBlock headlineText = new TextBlock
            {
                Text = "Welcome To\nBlackHouse",
                FontFamily = new FontFamily("Cinzel, Times New Roman, Segoe UI Black, Georgia, sans-serif"),
                FontSize = 38,
                FontWeight = FontWeights.Bold,
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                LineHeight = 46,
                Margin = new Thickness(0, 0, 0, 32),
                Effect = new DropShadowEffect
                {
                    Color = Colors.White,
                    BlurRadius = 14,
                    Opacity = 0.7,
                    ShadowDepth = 0
                }
            };

            // Discord Login Button ("Iniciar sesión con Discord" + Logo)
            _loginButton = new Button
            {
                Content = CreateButtonContent(),
                Width = 290,
                Height = 50,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Rounded Template for Button
            ControlTemplate btnTemplate = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "Border";
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
            borderFactory.SetValue(Border.EffectProperty, new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#5865F2"),
                BlurRadius = 18,
                Opacity = 0.45,
                ShadowDepth = 0
            });

            FrameworkElementFactory presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);

            btnTemplate.VisualTree = borderFactory;
            _loginButton.Template = btnTemplate;

            // Hover animations
            _loginButton.MouseEnter += (s, e) =>
            {
                _loginButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4752C4"));
            };
            _loginButton.MouseLeave += (s, e) =>
            {
                _loginButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2"));
            };

            _loginButton.Click += (s, e) =>
            {
                SetLoadingState(true);
                OnLoginRequested?.Invoke(this, EventArgs.Empty);
            };

            // Status Label below button
            _statusText = new TextBlock
            {
                Text = "",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8E9297")),
                FontSize = 13,
                Margin = new Thickness(0, 14, 0, 0),
                TextAlignment = TextAlignment.Center,
                Visibility = Visibility.Collapsed
            };

            cardContent.Children.Add(headlineText);
            cardContent.Children.Add(_loginButton);
            cardContent.Children.Add(_statusText);

            _welcomeCard.Child = cardContent;
            mainGrid.Children.Add(_welcomeCard);

            this.Content = mainGrid;

            // Trigger 0.5s Fade-In + Slide-Up Animation when loaded
            this.Loaded += WelcomeView_Loaded;
        }

        private void WelcomeView_Loaded(object sender, RoutedEventArgs e)
        {
            // Opacity Animation (0.0 to 1.0 in 0.5s)
            DoubleAnimation fadeAnim = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Slide Up Y Animation (40 to 0 in 0.5s)
            DoubleAnimation slideAnim = new DoubleAnimation
            {
                From = 40.0,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Execute direct WPF element animations safely
            _welcomeCard.BeginAnimation(UIElement.OpacityProperty, fadeAnim);

            if (_welcomeCard.RenderTransform is TranslateTransform slideTransform)
            {
                slideTransform.BeginAnimation(TranslateTransform.YProperty, slideAnim);
            }
        }

        private UIElement CreateButtonContent()
        {
            StackPanel panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Official Discord Logo SVG Icon Path
            Path discordIcon = new Path
            {
                Data = Geometry.Parse("M19.27 5.33C17.94 4.71 16.5 4.26 15 4a.09.09 0 0 0-.07.03c-.18.33-.39.76-.53 1.09a16.09 16.09 0 0 0-4.8 0c-.14-.34-.35-.76-.54-1.09c-.01-.02-.04-.03-.07-.03c-1.5.26-2.93.71-4.27 1.33c-.01 0-.02.01-.03.02C2.1 9.3 1.33 13.16 1.7 16.97c0 .02.01.04.03.05c1.78 1.31 3.5 2.11 5.17 2.63c.03.01.06 0 .07-.02c.4-.55.76-1.13 1.07-1.74c.02-.04 0-.09-.04-.11c-.57-.22-1.11-.48-1.63-.78c-.04-.02-.04-.08 0-.11c.11-.08.22-.17.33-.25c.02-.02.05-.02.07-.01c3.44 1.57 7.15 1.57 10.55 0c.02-.01.05-.01.07.01c.11.09.22.17.33.26c.04.03.04.09 0 .11c-.52.31-1.07.56-1.64.78c-.04.02-.05.07-.04.11c.32.61.68 1.19 1.07 1.74c.01.02.04.03.07.02c1.68-.52 3.4-1.32 5.18-2.63c.02-.01.03-.03.03-.05c.44-4.38-.73-8.21-3.1-11.62c-.01-.01-.02-.02-.03-.02zM8.52 14.91c-1.03 0-1.89-.95-1.89-2.12s.84-2.12 1.89-2.12c1.06 0 1.9.96 1.89 2.12c0 1.17-.84 2.12-1.89 2.12zm6.97 0c-1.03 0-1.89-.95-1.89-2.12s.84-2.12 1.89-2.12c1.06 0 1.9.96 1.89 2.12c0 1.17-.83 2.12-1.89 2.12z"),
                Fill = Brushes.White,
                Width = 22,
                Height = 18,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Text Label
            TextBlock label = new TextBlock
            {
                Text = "Iniciar sesión con Discord",
                FontFamily = new FontFamily("Segoe UI, sans-serif"),
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(discordIcon);
            panel.Children.Add(label);
            return panel;
        }

        public void SetLoadingState(bool isLoading, string message = "Iniciando sesión en el navegador...")
        {
            _loginButton.IsEnabled = !isLoading;
            if (isLoading)
            {
                _statusText.Text = message;
                _statusText.Visibility = Visibility.Visible;
            }
            else
            {
                _statusText.Visibility = Visibility.Collapsed;
            }
        }
    }
}

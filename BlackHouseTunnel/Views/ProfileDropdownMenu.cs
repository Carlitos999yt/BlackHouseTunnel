using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using BlackHouseTunnel.Models;

namespace BlackHouseTunnel.Views
{
    public class ProfileDropdownMenu : UserControl
    {
        public event EventHandler? OnLogoutRequested;
        public event EventHandler? OnSettingsRequested;

        private readonly DiscordUser _user;
        private Border _menuBorder = null!;

        public ProfileDropdownMenu(DiscordUser user)
        {
            _user = user;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            _menuBorder = new Border
            {
                Width = 220,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#12121B")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C2C40")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(8),
                Opacity = 0,
                RenderTransformOrigin = new Point(1, 0),
                Effect = new DropShadowEffect
                {
                    Color = (Color)ColorConverter.ConvertFromString("#5865F2"),
                    BlurRadius = 25,
                    Opacity = 0.35,
                    ShadowDepth = 4
                }
            };

            ScaleTransform scaleTransform = new ScaleTransform(0.85, 0.85);
            _menuBorder.RenderTransform = scaleTransform;

            StackPanel menuPanel = new StackPanel();

            // Header User Tag
            TextBlock userHeader = new TextBlock
            {
                Text = $"@{_user.Username}",
                FontFamily = new FontFamily("Segoe UI, sans-serif"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8E9297")),
                Margin = new Thickness(10, 6, 10, 8)
            };
            menuPanel.Children.Add(userHeader);

            // Separator
            Separator sep = new Separator
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#222232")),
                Margin = new Thickness(4, 0, 4, 8)
            };
            menuPanel.Children.Add(sep);

            // Professional SVG Settings Icon (Gear/Cog)
            string gearPathData = "M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.38a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2zM12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z";
            Button settingsBtn = CreateMenuItemWithSvgIcon("Configuración", gearPathData, Brushes.White, () => OnSettingsRequested?.Invoke(this, EventArgs.Empty));
            menuPanel.Children.Add(settingsBtn);

            // Professional SVG Logout Icon (Door Exit)
            string logoutPathData = "M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4M16 17l5-5-5-5M21 12H9";
            Button logoutBtn = CreateMenuItemWithSvgIcon("Cerrar Sesión", logoutPathData, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ED4245")), () => OnLogoutRequested?.Invoke(this, EventArgs.Empty), isDanger: true);
            menuPanel.Children.Add(logoutBtn);

            _menuBorder.Child = menuPanel;
            this.Content = _menuBorder;

            this.Loaded += ProfileDropdownMenu_Loaded;
        }

        private Button CreateMenuItemWithSvgIcon(string labelText, string svgData, Brush iconBrush, Action onClick, bool isDanger = false)
        {
            Button btn = new Button
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            StackPanel itemPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 8, 10, 8)
            };

            // Vector SVG Path Icon
            Path iconPath = new Path
            {
                Data = Geometry.Parse(svgData),
                Stroke = iconBrush,
                StrokeThickness = 1.6,
                Width = 16,
                Height = 16,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBlock label = new TextBlock
            {
                Text = labelText,
                FontFamily = new FontFamily("Segoe UI, sans-serif"),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = isDanger 
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ED4245")) 
                    : Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };

            itemPanel.Children.Add(iconPath);
            itemPanel.Children.Add(label);

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "Border";
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));

            FrameworkElementFactory presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);
            template.VisualTree = borderFactory;
            btn.Template = template;

            btn.Content = itemPanel;

            Color hoverColor = isDanger 
                ? (Color)ColorConverter.ConvertFromString("#3A1417") 
                : (Color)ColorConverter.ConvertFromString("#202030");

            btn.MouseEnter += (s, e) => btn.Background = new SolidColorBrush(hoverColor);
            btn.MouseLeave += (s, e) => btn.Background = Brushes.Transparent;
            btn.Click += (s, e) => onClick();

            return btn;
        }

        private void ProfileDropdownMenu_Loaded(object sender, RoutedEventArgs e)
        {
            DoubleAnimation fadeAnim = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            DoubleAnimation scaleAnim = new DoubleAnimation
            {
                From = 0.85,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(0.2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            _menuBorder.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
            if (_menuBorder.RenderTransform is ScaleTransform scale)
            {
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            }
        }
    }
}

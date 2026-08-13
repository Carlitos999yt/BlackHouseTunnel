using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using BlackHouseTunnel.Models;
using BlackHouseTunnel.Services;

namespace BlackHouseTunnel.Views
{
    public class ProfileDropdownMenu : UserControl
    {
        public event EventHandler? OnLogoutRequested;
        public event EventHandler? OnEditNickRequested;

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
                Background = ThemeManager.CardBgBrush,
                BorderBrush = ThemeManager.CardBorderBrush,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(10),
                Opacity = 0,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true,
                RenderTransformOrigin = new Point(1, 0)
            };
            TextOptions.SetTextFormattingMode(_menuBorder, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(_menuBorder, TextRenderingMode.ClearType);
            RenderOptions.SetBitmapScalingMode(_menuBorder, BitmapScalingMode.HighQuality);

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
                Foreground = ThemeManager.TextMutedBrush,
                Margin = new Thickness(10, 6, 10, 8)
            };
            menuPanel.Children.Add(userHeader);

            // Separator
            Separator sep = new Separator
            {
                Background = ThemeManager.CardBorderBrush,
                Margin = new Thickness(4, 0, 4, 8)
            };
            menuPanel.Children.Add(sep);

            // SVG User Edit Icon (Person/Pen)
            string userEditPathData = "M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z";
            Button nickBtn = CreateMenuItemWithSvgIcon(LocalizationService.Get("profile_edit_nick"), userEditPathData, ThemeManager.TextPrimaryBrush, () => OnEditNickRequested?.Invoke(this, EventArgs.Empty));
            menuPanel.Children.Add(nickBtn);

            // Professional SVG Logout Icon (Door Exit)
            string logoutPathData = "M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4M16 17l5-5-5-5M21 12H9";
            Button logoutBtn = CreateMenuItemWithSvgIcon(LocalizationService.Get("profile_logout"), logoutPathData, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ED4245")), () => OnLogoutRequested?.Invoke(this, EventArgs.Empty), isDanger: true);
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

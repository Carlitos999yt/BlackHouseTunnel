using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Win32;
using BlackHouseTunnel.Models;
using BlackHouseTunnel.Services;

namespace BlackHouseTunnel.Views
{
    public class CreateHostModal : UserControl
    {
        public event EventHandler? OnCloseRequested;
        public event EventHandler<string>? OnHostStarted;

        private readonly DiscordUser _user;

        public CreateHostModal(DiscordUser user)
        {
            _user = user;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var config = ConfigManager.CurrentConfig;

            Grid modalRoot = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(190, 4, 4, 8))
            };

            Border modalCard = new Border
            {
                Width = 520,
                Padding = new Thickness(24),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D0D16")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C3AED")),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(16),
                Effect = new DropShadowEffect
                {
                    Color = (Color)ColorConverter.ConvertFromString("#7C3AED"),
                    BlurRadius = 30,
                    Opacity = 0.35,
                    ShadowDepth = 0
                }
            };

            StackPanel panel = new StackPanel();

            // Header Title
            StackPanel headerRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 18) };

            TextBlock title = new TextBlock
            {
                Text = "⚡ Crear Sesión de Host",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };

            headerRow.Children.Add(title);
            panel.Children.Add(headerRow);

            // Field 1: User ID
            panel.Children.Add(CreateLabel("ID de Usuario de Roblox (UID)"));
            string defaultUid = !string.IsNullOrWhiteSpace(_user.Id) ? _user.Id : (config.SavedUserId ?? "");
            TextBox uidBox = CreateStyledTextBox(defaultUid);
            panel.Children.Add(uidBox);

            // Field 2: Username
            panel.Children.Add(CreateLabel("Apodo en el Servidor (Username)"));
            string defaultUser = !string.IsNullOrWhiteSpace(_user.DisplayNick) ? _user.DisplayNick : (config.SavedUsername ?? "");
            TextBox userBox = CreateStyledTextBox(defaultUser);
            panel.Children.Add(userBox);

            // Field 3: Tunnel Name Input
            panel.Children.Add(CreateLabel("Nombre del Túnel / Servidor"));
            string defaultName = !string.IsNullOrWhiteSpace(config.SavedServerName) ? config.SavedServerName : $"Servidor de {defaultUser}";
            TextBox nameBox = CreateStyledTextBox(defaultName);
            panel.Children.Add(nameBox);

            // Field 4: Port Input
            panel.Children.Add(CreateLabel("Puerto UDP Local"));
            TextBox portBox = CreateStyledTextBox(config.SavedUdpPort > 0 ? config.SavedUdpPort.ToString() : "55555");
            panel.Children.Add(portBox);

            // Field 5: Host Address
            panel.Children.Add(CreateLabel("Dirección del Túnel Remoto (Host Address)"));
            TextBox addrBox = CreateStyledTextBox(config.SavedRemoteHostAddress ?? "");
            panel.Children.Add(addrBox);

            // Field 6: Map File Browse
            panel.Children.Add(CreateLabel("Archivo de Mapa Roblox (.rbxl / .rbxlx) [Opcional]"));
            Grid mapGrid = new Grid();
            mapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBox mapBox = CreateStyledTextBox(config.SavedMapPath ?? "");
            Grid.SetColumn(mapBox, 0);
            mapGrid.Children.Add(mapBox);

            Button browseBtn = new Button
            {
                Content = "📁 Examinar...",
                Height = 36,
                Padding = new Thickness(14, 0, 14, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F1F32")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(8, 0, 0, 10)
            };
            ControlTemplate browseTemplate = new ControlTemplate(typeof(Button));
            FrameworkElementFactory bBorderFactory = new FrameworkElementFactory(typeof(Border));
            bBorderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            bBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            FrameworkElementFactory bPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            bPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            bPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            bBorderFactory.AppendChild(bPresenterFactory);
            browseTemplate.VisualTree = bBorderFactory;
            browseBtn.Template = browseTemplate;

            browseBtn.Click += (s, e) =>
            {
                OpenFileDialog dlg = new OpenFileDialog
                {
                    Title = "Seleccionar Mapa de Roblox",
                    Filter = "Roblox Place (*.rbxl;*.rbxlx)|*.rbxl;*.rbxlx|Todos los archivos (*.*)|*.*"
                };
                if (dlg.ShowDialog() == true)
                {
                    mapBox.Text = dlg.FileName;
                }
            };
            Grid.SetColumn(browseBtn, 1);
            mapGrid.Children.Add(browseBtn);
            panel.Children.Add(mapGrid);

            // Action Buttons (Start Host & Cancel)
            StackPanel btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };

            Button cancelBtn = CreateButton("Cancelar", "#222234", Brushes.White, () => OnCloseRequested?.Invoke(this, EventArgs.Empty));
            Button startBtn = CreateButton("🚀 Iniciar Host", "#7C3AED", Brushes.White, () =>
            {
                try
                {
                    string uid = uidBox.Text.Trim();
                    string user = userBox.Text.Trim();
                    string port = portBox.Text.Trim();
                    string addr = addrBox.Text.Trim();
                    string mapPath = mapBox.Text.Trim();

                    if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(port) || string.IsNullOrEmpty(addr))
                    {
                        MessageBox.Show("Todos los campos obligatorios deben completarse.", "Campos Faltantes", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (!int.TryParse(port, out int targetPort))
                    {
                        MessageBox.Show("El puerto debe ser un número válido.", "Puerto Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string studioPath = RobloxStudioService.GetStudioPath();
                    if (string.IsNullOrEmpty(studioPath))
                    {
                        MessageBox.Show("No se encontró una instalación ejecutable de Roblox Studio en tu sistema.", "Error Roblox Studio", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Save host configuration
                    config.SavedUserId = uid;
                    config.SavedUsername = user;
                    config.SavedServerName = nameBox.Text.Trim();
                    config.SavedUdpPort = targetPort;
                    config.SavedRemoteHostAddress = addr;
                    config.SavedMapPath = mapPath;
                    ConfigManager.SaveConfig(config);

                    PluginInstaller.EnsurePluginInstalled(out _);
                    RbxmBridgeServer.ActiveUsername = string.IsNullOrWhiteSpace(user) ? "Player" : user;
                    RbxmBridgeServer.ActiveUid = uid;
                    RbxmBridgeServer.Start();

                    if (!string.IsNullOrEmpty(mapPath) && File.Exists(mapPath))
                    {
                        ScriptInjector.InjectScriptIntoMap(mapPath, ScriptInjector.GetSecurityLuauScript(RbxmBridgeServer.ActiveUsername));
                    }

                    UdpProxy.StartHostFirewallProxy(targetPort, UdpProxy.INTERNAL_HOST_PORT);

                    string sessionGuid = Guid.NewGuid().ToString();
                    string playGuid = Guid.NewGuid().ToString();

                    RobloxStudioService.LaunchServer(studioPath, UdpProxy.INTERNAL_HOST_PORT.ToString(), RbxmBridgeServer.ActiveUid, sessionGuid, playGuid, RbxmBridgeServer.ActiveUsername);

                    OnHostStarted?.Invoke(this, nameBox.Text);
                    OnCloseRequested?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al iniciar el Host: {ex.Message}", "Error Host", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            btnRow.Children.Add(cancelBtn);
            btnRow.Children.Add(startBtn);
            panel.Children.Add(btnRow);

            modalCard.Child = panel;
            modalRoot.Children.Add(modalCard);

            this.Content = modalRoot;
        }

        private TextBlock CreateLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9494B8")),
                Margin = new Thickness(0, 0, 0, 4)
            };
        }

        private TextBox CreateStyledTextBox(string defaultText)
        {
            return new TextBox
            {
                Text = defaultText,
                Height = 36,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#141422")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28283E")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 6, 10, 6),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10)
            };
        }

        private Button CreateButton(string label, string bgHex, Brush fg, Action onClick)
        {
            Button btn = new Button
            {
                Content = label,
                Height = 38,
                Padding = new Thickness(18, 0, 18, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex)),
                Foreground = fg,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(6, 0, 0, 0)
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

            btn.Click += (s, e) => onClick();
            return btn;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using BlackHouseTunnel.Models;
using BlackHouseTunnel.Services;
using Path = System.Windows.Shapes.Path;

namespace BlackHouseTunnel.Views
{
    public class MainMenuView : UserControl
    {
        public event EventHandler? OnLogoutRequested;

        private readonly DiscordUser _user;
        private readonly OnlineMembersMonitor _membersMonitor;
        private HostConsoleView? _activeHostConsoleView = null;
        private static readonly System.Net.Http.HttpClient AvatarHttpClient = new System.Net.Http.HttpClient();

        private Grid _rootGrid = null!;
        private Grid _dropdownOverlay = null!;
        private Border _notificationsDrawer = null!;
        private Grid _contentHostGrid = null!;
        private StackPanel _friendsRowPanel = null!;

        private Button _btnHome = null!;
        private Button _btnHost = null!;
        private Button _btnJoin = null!;
        private Button _btnRbxm = null!;
        private Button _btnRsm = null!;
        private Button _btnEcho = null!;
        private Button _btnSettings = null!;

        public MainMenuView(DiscordUser user)
        {
            _user = user;
            _membersMonitor = new OnlineMembersMonitor(ConfigManager.CurrentConfig);
            InitializeComponent();
            _membersMonitor.OnMembersUpdated += MembersMonitor_OnMembersUpdated;
            _membersMonitor.Start();
        }

        private void InitializeComponent()
        {
            _rootGrid = new Grid();

            Grid appLayoutGrid = new Grid
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#060609"))
            };

            appLayoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(68) });
            appLayoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border sidebar = CreateLeftSidebar();
            Grid.SetColumn(sidebar, 0);
            appLayoutGrid.Children.Add(sidebar);

            Grid rightAreaGrid = new Grid();
            rightAreaGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(74) });
            rightAreaGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Border topBar = CreateTopHeaderBar();
            Grid.SetRow(topBar, 0);
            rightAreaGrid.Children.Add(topBar);

            _contentHostGrid = new Grid();
            Grid.SetRow(_contentHostGrid, 1);
            rightAreaGrid.Children.Add(_contentHostGrid);

            Grid.SetColumn(rightAreaGrid, 1);
            appLayoutGrid.Children.Add(rightAreaGrid);

            _rootGrid.Children.Add(appLayoutGrid);

            CreateOverlayLayers(_rootGrid);

            SwitchTab("Home");

            this.Content = _rootGrid;
        }

        private Border CreateLeftSidebar()
        {
            Border sidebar = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#09090F")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#181826")),
                BorderThickness = new Thickness(0, 0, 1, 0)
            };

            StackPanel navPanel = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };

            Path logoIcon = new Path
            {
                Data = Geometry.Parse("M19.27 5.33C17.94 4.71 16.5 4.26 15 4a.09.09 0 0 0-.07.03c-.18.33-.39.76-.53 1.09a16.09 16.09 0 0 0-4.8 0c-.14-.34-.35-.76-.54-1.09c-.01-.02-.04-.03-.07-.03c-1.5.26-2.93.71-4.27 1.33c-.01 0-.02.01-.03.02C2.1 9.3 1.33 13.16 1.7 16.97c0 .02.01.04.03.05c1.78 1.31 3.5 2.11 5.17 2.63c.03.01.06 0 .07-.02c.4-.55.76-1.13 1.07-1.74c.02-.04 0-.09-.04-.11c-.57-.22-1.11-.48-1.63-.78c-.04-.02-.04-.08 0-.11c.11-.08.22-.17.33-.25c.02-.02.05-.02.07-.01c3.44 1.57 7.15 1.57 10.55 0c.02-.01.05-.01.07.01c.11.09.22.17.33.26c.04.03.04.09 0 .11c-.52.31-1.07.56-1.64.78c-.04.02-.05.07-.04.11c.32.61.68 1.19 1.07 1.74c.01.02.04.03.07.02c1.68-.52 3.4-1.32 5.18-2.63c.02-.01.03-.03.03-.05c.44-4.38-.73-8.21-3.1-11.62c-.01-.01-.02-.02-.03-.02zM8.52 14.91c-1.03 0-1.89-.95-1.89-2.12s.84-2.12 1.89-2.12c1.06 0 1.9.96 1.89 2.12c0 1.17-.84 2.12-1.89 2.12zm6.97 0c-1.03 0-1.89-.95-1.89-2.12s.84-2.12 1.89-2.12c1.06 0 1.9.96 1.89 2.12c0 1.17-.83 2.12-1.89 2.12z"),
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                Width = 26,
                Height = 22,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                Effect = new DropShadowEffect
                {
                    Color = (Color)ColorConverter.ConvertFromString("#5865F2"),
                    BlurRadius = 18,
                    Opacity = 0.8,
                    ShadowDepth = 0
                }
            };
            navPanel.Children.Add(logoIcon);

            string homeSvg = "M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z M9 22V12h6v10";
            string hostSvg = "M2 9h20M2 15h20M6 6h.01M6 12h.01M6 18h.01";
            string joinSvg = "M6 12h12M12 6v12";
            string rbxmSvg = "M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z";
            string rsmSvg = "M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z";
            string echoSvg = "M5 12.55a11 11 0 0 1 14.08 0M1.42 9a16 16 0 0 1 21.16 0M8.53 16.11a6 6 0 0 1 6.95 0M12 20h.01";
            string settingsSvg = "M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.38a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2zM12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z";

            _btnHome = CreateSidebarNavButton(homeSvg, "Inicio", () => SwitchTab("Home"));
            _btnHost = CreateSidebarNavButton(hostSvg, "Crear Host", () => SwitchTab("Host"));
            _btnJoin = CreateSidebarNavButton(joinSvg, "Unirse a Host", () => SwitchTab("Join"));
            _btnRbxm = CreateSidebarNavButton(rbxmSvg, "Importador Mapas .rbxm", () => SwitchTab("Rbxm"));
            _btnRsm = CreateSidebarNavButton(rsmSvg, "Asistente RSM Mod Manager", () => SwitchTab("Rsm"));
            _btnEcho = CreateSidebarNavButton(echoSvg, "Prueba Latencia & Eco UDP", () => SwitchTab("Echo"));
            _btnSettings = CreateSidebarNavButton(settingsSvg, "Ajustes", () => SwitchTab("Settings"));

            navPanel.Children.Add(_btnHome);
            navPanel.Children.Add(_btnHost);
            navPanel.Children.Add(_btnJoin);
            navPanel.Children.Add(_btnRbxm);
            navPanel.Children.Add(_btnRsm);
            navPanel.Children.Add(_btnEcho);
            navPanel.Children.Add(_btnSettings);

            sidebar.Child = navPanel;
            return sidebar;
        }

        private Button CreateSidebarNavButton(string svgData, string tooltipText, Action onClick)
        {
            Button btn = new Button
            {
                Width = 44,
                Height = 44,
                Margin = new Thickness(0, 0, 0, 10),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = tooltipText
            };

            Path iconPath = new Path
            {
                Data = Geometry.Parse(svgData),
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8E9297")),
                StrokeThickness = 1.8,
                Width = 19,
                Height = 19,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));

            FrameworkElementFactory presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);
            template.VisualTree = borderFactory;
            btn.Template = template;

            btn.Content = iconPath;
            btn.Click += (s, e) => onClick();

            return btn;
        }

        private void SetButtonActiveState(Button btn, bool isActive)
        {
            if (btn == null) return;
            btn.Background = isActive 
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E30")) 
                : Brushes.Transparent;

            if (btn.Content is Path path)
            {
                path.Stroke = isActive 
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")) 
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8E9297"));
            }
        }

        private void SwitchTab(string tabName)
        {
            SetButtonActiveState(_btnHome, tabName == "Home");
            SetButtonActiveState(_btnHost, tabName == "Host");
            SetButtonActiveState(_btnJoin, tabName == "Join");
            SetButtonActiveState(_btnRbxm, tabName == "Rbxm");
            SetButtonActiveState(_btnRsm, tabName == "Rsm");
            SetButtonActiveState(_btnEcho, tabName == "Echo");
            SetButtonActiveState(_btnSettings, tabName == "Settings");

            _contentHostGrid.Children.Clear();

            switch (tabName)
            {
                case "Home":
                    _contentHostGrid.Children.Add(BuildHomeDashboardView());
                    break;
                case "Host":
                    if (_activeHostConsoleView != null)
                    {
                        _contentHostGrid.Children.Add(_activeHostConsoleView);
                    }
                    else
                    {
                        _contentHostGrid.Children.Add(BuildHostView());
                    }
                    break;
                case "Join":
                    _contentHostGrid.Children.Add(BuildJoinView());
                    break;
                case "Rbxm":
                    _contentHostGrid.Children.Add(new RbxmImporterView());
                    break;
                case "Rsm":
                    _contentHostGrid.Children.Add(new RsmAssistantView());
                    break;
                case "Echo":
                    _contentHostGrid.Children.Add(new EchoTestView());
                    break;
                case "Settings":
                    _contentHostGrid.Children.Add(BuildSettingsView());
                    break;
            }
        }

        private Border CreateTopHeaderBar()
        {
            Border topBar = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A10")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#161624")),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            Grid headerGrid = new Grid
            {
                Margin = new Thickness(24, 0, 24, 0)
            };

            TextBlock logoTitle = new TextBlock
            {
                Text = "BLACKHOUSE TUNNEL",
                FontFamily = new FontFamily("Cinzel, Times New Roman, Segoe UI Black, Georgia, sans-serif"),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Effect = new DropShadowEffect
                {
                    Color = Colors.White,
                    BlurRadius = 8,
                    Opacity = 0.5,
                    ShadowDepth = 0
                }
            };
            headerGrid.Children.Add(logoTitle);

            StackPanel rightHeaderActions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid bellContainer = new Grid
            {
                Margin = new Thickness(0, 0, 16, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            Button bellBtn = new Button
            {
                Width = 42,
                Height = 42,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#12121A")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#222234")),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            ControlTemplate btnTemplate = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(21));
            FrameworkElementFactory presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);
            btnTemplate.VisualTree = borderFactory;
            bellBtn.Template = btnTemplate;

            Path bellIcon = new Path
            {
                Data = Geometry.Parse("M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 0 1-3.46 0"),
                Stroke = Brushes.White,
                StrokeThickness = 1.6,
                Width = 18,
                Height = 18,
                Stretch = Stretch.Uniform
            };
            bellBtn.Content = bellIcon;
            bellBtn.Click += (s, e) => ToggleNotificationsDrawer();

            Border badge = new Border
            {
                Width = 18,
                Height = 18,
                CornerRadius = new CornerRadius(9),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ED4245")),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -2, -2, 0)
            };
            TextBlock badgeTxt = new TextBlock
            {
                Text = "3",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            badge.Child = badgeTxt;

            bellContainer.Children.Add(bellBtn);
            bellContainer.Children.Add(badge);
            rightHeaderActions.Children.Add(bellContainer);

            Border profileCard = Create4LevelProfileBadge(_user);
            profileCard.Cursor = System.Windows.Input.Cursors.Hand;
            profileCard.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                ToggleProfileDropdown();
            };

            rightHeaderActions.Children.Add(profileCard);
            headerGrid.Children.Add(rightHeaderActions);

            topBar.Child = headerGrid;
            return topBar;
        }

        // TAB 1: HOME DASHBOARD VIEW
        private UIElement BuildHomeDashboardView()
        {
            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            StackPanel body = new StackPanel { Margin = new Thickness(28) };

            Border banner = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F0F1A")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#222238")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(24),
                Margin = new Thickness(0, 0, 0, 24)
            };

            StackPanel bannerPanel = new StackPanel();
            TextBlock welcomeTitle = new TextBlock
            {
                Text = $"¡Hola de nuevo, {_user.DisplayNick}! ⚡",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 6)
            };
            TextBlock welcomeSub = new TextBlock
            {
                Text = $"Tu cuenta ({_user.Handle}) está autenticada. Rol principal: {_user.PrimaryRole}. {(_user.IsPrivadito ? " (Rol Privadito Activo 🔒)" : "")}",
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAA0"))
            };

            bannerPanel.Children.Add(welcomeTitle);
            bannerPanel.Children.Add(welcomeSub);
            banner.Child = bannerPanel;
            body.Children.Add(banner);

            TextBlock friendsHeader = new TextBlock
            {
                Text = "Miembros en Línea",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 12)
            };
            body.Children.Add(friendsHeader);

            ScrollViewer friendsScroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 0, 0, 28)
            };

            _friendsRowPanel = new StackPanel { Orientation = Orientation.Horizontal };
            friendsScroll.Content = _friendsRowPanel;
            body.Children.Add(friendsScroll);

            TextBlock tunnelsHeader = new TextBlock
            {
                Text = "Túneles de Host Activos",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 14)
            };
            body.Children.Add(tunnelsHeader);

            WrapPanel tunnelGrid = new WrapPanel();
            tunnelGrid.Children.Add(CreateTunnelCard("Servidor Principal BlackHouse", $"Host: {_user.DisplayNick}", "24 ms"));
            tunnelGrid.Children.Add(CreateTunnelCard("Túnel Privadito Exclusivo", "Host: Sang", "18 ms", isPrivadito: true));

            body.Children.Add(tunnelGrid);

            scroll.Content = body;
            return scroll;
        }

        // TAB 2: COMPLETE HOST CONFIGURATION VIEW WITH ALL FIELDS (No player limit)
        private UIElement BuildHostView()
        {
            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(32), MaxWidth = 650, HorizontalAlignment = HorizontalAlignment.Left };

            TextBlock title = new TextBlock
            {
                Text = "🖥️ Configuración Completa de Host de Servidor",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 6)
            };

            TextBlock sub = new TextBlock
            {
                Text = "Configuración completa de servidor túnel.",
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9494B8")),
                Margin = new Thickness(0, 0, 0, 24)
            };

            panel.Children.Add(title);
            panel.Children.Add(sub);

            Border box = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D0D14")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F1F30")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(24)
            };

            StackPanel boxPanel = new StackPanel();

            // Field 1: ID de Usuario de Roblox (UID) - Defecto VACÍO salvo guardado
            boxPanel.Children.Add(CreateLabel("ID de Usuario de Roblox (User ID / UID)"));
            TextBox uidBox = CreateStyledTextBox(ConfigManager.CurrentConfig.SavedUserId);
            boxPanel.Children.Add(uidBox);

            // Field 2: Apodo / Username - Defecto VACÍO salvo guardado
            boxPanel.Children.Add(CreateLabel("Apodo en el Servidor (Username)"));
            TextBox userBox = CreateStyledTextBox(ConfigManager.CurrentConfig.SavedUsername);
            boxPanel.Children.Add(userBox);

            // Field 3: Nombre del Servidor Túnel - Defecto VACÍO salvo guardado
            boxPanel.Children.Add(CreateLabel("Nombre del Servidor Túnel"));
            TextBox nameBox = CreateStyledTextBox(ConfigManager.CurrentConfig.SavedServerName);
            boxPanel.Children.Add(nameBox);

            // Field 4: Puerto Local UDP (Único por defecto: 55555)
            boxPanel.Children.Add(CreateLabel("Puerto Local UDP"));
            TextBox portBox = CreateStyledTextBox(ConfigManager.CurrentConfig.SavedUdpPort.ToString());
            boxPanel.Children.Add(portBox);

            // Field 5: Dirección del Túnel Remoto - Defecto VACÍO salvo guardado
            boxPanel.Children.Add(CreateLabel("Dirección del Túnel Remoto (Host Address)"));
            TextBox addrBox = CreateStyledTextBox(ConfigManager.CurrentConfig.SavedRemoteHostAddress);
            boxPanel.Children.Add(addrBox);

            // Field 6: Archivo de Mapa Roblox (.rbxl / .rbxlx) [Opcional]
            boxPanel.Children.Add(CreateLabel("Archivo de Mapa Roblox (.rbxl / .rbxlx) [Opcional]"));
            Grid mapGrid = new Grid();
            mapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBox mapBox = CreateStyledTextBox(ConfigManager.CurrentConfig.SavedMapPath);
            Grid.SetColumn(mapBox, 0);
            mapGrid.Children.Add(mapBox);

            Button browseBtn = new Button
            {
                Content = "📁 Examinar...",
                Height = 36,
                Padding = new Thickness(12, 0, 12, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F1F30")),
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
                    ConfigManager.CurrentConfig.SavedMapPath = dlg.FileName;
                    ConfigManager.SaveConfig(ConfigManager.CurrentConfig);
                }
            };
            Grid.SetColumn(browseBtn, 1);
            mapGrid.Children.Add(browseBtn);
            boxPanel.Children.Add(mapGrid);

            // Field 7: Visibilidad & Permisos (para Staff / Hoster)
            ComboBox visCombo = new ComboBox
            {
                Height = 38,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#141420")),
                Foreground = Brushes.White,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 16)
            };

            visCombo.Items.Add("🌐 Público (Todos los miembros de Discord)");
            visCombo.Items.Add("🔒 Exclusivo Rol Privadito (Solo usuarios con rol Privadito)");
            visCombo.Items.Add("🛡️ Whitelist Personalizada (Especificar IDs de Discord)");
            visCombo.Items.Add("👤 Prueba Privada (Solo Yo)");
            visCombo.SelectedIndex = Math.Clamp(ConfigManager.CurrentConfig.SavedVisibilityOptionIndex, 0, 3);

            if (_user.IsStaffOrAdmin || _user.IsHoster)
            {
                boxPanel.Children.Add(CreateLabel("🔒 Visibilidad & Permisos de Acceso"));
                boxPanel.Children.Add(visCombo);
            }

            // Action Buttons Row (Import Scripts + Start Host)
            StackPanel btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };

            Button importBtn = new Button
            {
                Content = "📄 Importar Scripts",
                Height = 44,
                Padding = new Thickness(16, 0, 16, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E2E")),
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 12, 0)
            };
            ControlTemplate importTemplate = new ControlTemplate(typeof(Button));
            FrameworkElementFactory iBorder = new FrameworkElementFactory(typeof(Border));
            iBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            iBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            FrameworkElementFactory iPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            iPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            iPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            iBorder.AppendChild(iPresenter);
            importTemplate.VisualTree = iBorder;
            importBtn.Template = importTemplate;

            importBtn.Click += (s, e) =>
            {
                try
                {
                    RbxmBridgeServer.ForceScriptImport = true;
                    RbxmBridgeServer.ScriptsImported = true;
                    PluginInstaller.EnsurePluginInstalled(out string msg);
                    DarkMessageBox.Show($"✓ Scripts importados correctamente en Roblox Studio.\n\n{msg}", "✓ Importación de Scripts", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    DarkMessageBox.Show("Error al importar scripts: " + ex.Message, "Error Importación", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            btnRow.Children.Add(importBtn);

            Button startHostBtn = new Button
            {
                Content = "🚀 Iniciar Servidor Host",
                Height = 44,
                Padding = new Thickness(24, 0, 24, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            ControlTemplate startTemplate = new ControlTemplate(typeof(Button));
            FrameworkElementFactory sBorder = new FrameworkElementFactory(typeof(Border));
            sBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            sBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            FrameworkElementFactory sPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            sPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            sPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            sBorder.AppendChild(sPresenter);
            startTemplate.VisualTree = sBorder;
            startHostBtn.Template = startTemplate;

            startHostBtn.Click += (s, e) =>
            {
                try
                {
                    string studioPath = RobloxStudioService.GetStudioPath();
                    if (string.IsNullOrEmpty(studioPath))
                    {
                        DarkMessageBox.Show("No se encontró una instalación ejecutable de Roblox Studio en tu sistema.", "Error Roblox Studio", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    int targetPort = int.TryParse(portBox.Text, out var p) ? p : 55555;
                    string targetUsername = string.IsNullOrWhiteSpace(userBox.Text) ? _user.DisplayNick : userBox.Text.Trim();
                    string targetUid = string.IsNullOrWhiteSpace(uidBox.Text) ? _user.Id : uidBox.Text.Trim();
                    string targetServerName = nameBox.Text.Trim();
                    string mapPath = mapBox.Text.Trim();
                    string addr = addrBox.Text.Trim();

                    // Save all form values to config.json in BlackHouseTunnel folder
                    ConfigManager.CurrentConfig.SavedUserId = targetUid;
                    ConfigManager.CurrentConfig.SavedUsername = targetUsername;
                    ConfigManager.CurrentConfig.SavedServerName = targetServerName;
                    ConfigManager.CurrentConfig.SavedUdpPort = targetPort;
                    ConfigManager.CurrentConfig.SavedRemoteHostAddress = addr;
                    ConfigManager.CurrentConfig.SavedMapPath = mapPath;
                    ConfigManager.CurrentConfig.SavedVisibilityOptionIndex = visCombo.SelectedIndex;
                    ConfigManager.SaveConfig(ConfigManager.CurrentConfig);

                    PluginInstaller.EnsurePluginInstalled(out string pluginMsg);
                    RbxmBridgeServer.ActiveUsername = targetUsername;
                    RbxmBridgeServer.ActiveUid = targetUid;
                    RbxmBridgeServer.Start();

                    HostConsoleView hostConsole = new HostConsoleView(studioPath, targetUid, targetPort.ToString(), addr, mapPath, targetUsername);
                    _activeHostConsoleView = hostConsole;
                    hostConsole.OnStopHostRequested += (s2, e2) =>
                    {
                        UdpProxy.StopProxy();
                        RbxmBridgeServer.Stop();
                        _activeHostConsoleView = null;
                        SwitchTab("Host");
                    };

                    _contentHostGrid.Children.Clear();
                    _contentHostGrid.Children.Add(hostConsole);
                }
                catch (Exception ex)
                {
                    DarkMessageBox.Show($"Error al iniciar el Host: {ex.Message}", "Error Host", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            btnRow.Children.Add(startHostBtn);

            boxPanel.Children.Add(btnRow);
            box.Child = boxPanel;
            panel.Children.Add(box);

            scroll.Content = panel;
            return scroll;
        }

        // TAB 3: JOIN TUNNEL VIEW
        private UIElement BuildJoinView()
        {
            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(32), MaxWidth = 600, HorizontalAlignment = HorizontalAlignment.Left };

            TextBlock title = new TextBlock
            {
                Text = "🎮 Unirse a un Túnel de Host",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 6)
            };

            TextBlock sub = new TextBlock
            {
                Text = "Ingresa tu apodo de jugador y la dirección IP / Túnel del Host.",
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA")),
                Margin = new Thickness(0, 0, 0, 24)
            };

            panel.Children.Add(title);
            panel.Children.Add(sub);

            Border box = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D0D14")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F1F30")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(24)
            };

            StackPanel boxPanel = new StackPanel();

            boxPanel.Children.Add(CreateLabel("Mi Apodo en Roblox (Username)"));
            TextBox userBox = CreateStyledTextBox(_user.DisplayNick);
            boxPanel.Children.Add(userBox);

            boxPanel.Children.Add(CreateLabel("Dirección del Túnel (ej: manzana.gl.at.ply.gg:20573)"));
            TextBox addrBox = CreateStyledTextBox("manzana.gl.at.ply.gg:20573");
            boxPanel.Children.Add(addrBox);

            Button connectBtn = new Button
            {
                Content = "⚡ Conectar al Servidor",
                Height = 44,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 16, 0, 0)
            };

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            FrameworkElementFactory presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);
            template.VisualTree = borderFactory;
            connectBtn.Template = template;

            connectBtn.Click += (s, e) =>
            {
                try
                {
                    string studioPath = RobloxStudioService.GetStudioPath();
                    if (string.IsNullOrEmpty(studioPath))
                    {
                        MessageBox.Show("No se encontró una instalación ejecutable de Roblox Studio en tu sistema.", "Error Roblox Studio", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string rawAddress = addrBox.Text.Trim();
                    if (string.IsNullOrEmpty(rawAddress))
                    {
                        MessageBox.Show("Por favor ingresa una dirección de túnel válida.", "Error Dirección", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var parts = rawAddress.Split(':');
                    string dstHost = parts[0];
                    int dstPort = parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : 55555;
                    string username = string.IsNullOrWhiteSpace(userBox.Text) ? _user.DisplayNick : userBox.Text.Trim();

                    JoinConsoleView joinConsole = new JoinConsoleView(studioPath, dstHost, dstPort, username);
                    joinConsole.OnDisconnectRequested += (s2, e2) => SwitchTab("Join");

                    _contentHostGrid.Children.Clear();
                    _contentHostGrid.Children.Add(joinConsole);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al conectar al Túnel: {ex.Message}", "Error Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            boxPanel.Children.Add(connectBtn);
            box.Child = boxPanel;
            panel.Children.Add(box);

            scroll.Content = panel;
            return scroll;
        }

        // TAB 4: SETTINGS VIEW
        private UIElement BuildSettingsView()
        {
            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(32), MaxWidth = 650, HorizontalAlignment = HorizontalAlignment.Left };

            TextBlock title = new TextBlock
            {
                Text = "⚙️ Ajustes & Configuración de BlackHouseTunnel",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 24)
            };

            panel.Children.Add(title);

            Border box = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D0D14")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F1F30")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(24)
            };

            StackPanel boxPanel = new StackPanel();

            boxPanel.Children.Add(CreateLabel("Discord Client ID (🔒 Fijo - No modificable)"));
            TextBox clientIdBox = CreateStyledTextBox(ConfigManager.CurrentConfig.ClientId);
            clientIdBox.IsReadOnly = true;
            clientIdBox.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#08080E"));
            clientIdBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8E9297"));
            boxPanel.Children.Add(clientIdBox);

            boxPanel.Children.Add(CreateLabel("Servidor Guild ID Exigido"));
            TextBox guildIdBox = CreateStyledTextBox(ConfigManager.CurrentConfig.GuildId);
            boxPanel.Children.Add(guildIdBox);

            boxPanel.Children.Add(CreateLabel("Puerto del Servidor OAuth Local"));
            TextBox portBox = CreateStyledTextBox(ConfigManager.CurrentConfig.LocalServerPort.ToString());
            boxPanel.Children.Add(portBox);

            Button saveBtn = new Button
            {
                Content = "💾 Guardar Cambios de Configuración",
                Height = 42,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 16, 0, 0)
            };

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            FrameworkElementFactory presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);
            template.VisualTree = borderFactory;
            saveBtn.Template = template;

            saveBtn.Click += (s, e) =>
            {
                ConfigManager.CurrentConfig.GuildId = guildIdBox.Text;
                if (int.TryParse(portBox.Text, out int port)) ConfigManager.CurrentConfig.LocalServerPort = port;
                ConfigManager.SaveConfig(ConfigManager.CurrentConfig);
                DarkMessageBox.Show("¡Configuración guardada con éxito en %LocalAppData%\\BlackHouseTunnel\\config.json!", "Guardado", MessageBoxButton.OK, MessageBoxImage.Information);
            };

            boxPanel.Children.Add(saveBtn);
            box.Child = boxPanel;
            panel.Children.Add(box);

            scroll.Content = panel;
            return scroll;
        }

        private TextBlock CreateLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA")),
                Margin = new Thickness(0, 8, 0, 4)
            };
        }

        private TextBox CreateStyledTextBox(string defaultText)
        {
            return new TextBox
            {
                Text = defaultText,
                Height = 36,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#12121A")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A3E")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 6, 8, 6),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10)
            };
        }

        private void MembersMonitor_OnMembersUpdated(object? sender, List<DiscordUser> members)
        {
            if (_friendsRowPanel != null)
            {
                _friendsRowPanel.Children.Clear();
                foreach (var member in members)
                {
                    _friendsRowPanel.Children.Add(CreateOnlineFriendItem(member));
                }
            }
        }

        private async void LoadAvatarImageAsync(string avatarUrl, Ellipse circle)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(avatarUrl))
                {
                    circle.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2"));
                    return;
                }

                byte[] data = await AvatarHttpClient.GetByteArrayAsync(avatarUrl);
                using (var ms = new MemoryStream(data))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                    bmp.Freeze();
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        circle.Fill = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill };
                    });
                }
            }
            catch
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    circle.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2"));
                });
            }
        }

        private Border CreateOnlineFriendItem(DiscordUser member)
        {
            Border container = new Border
            {
                Margin = new Thickness(0, 0, 16, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            StackPanel panel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Grid avatarGrid = new Grid();

            Ellipse circle = new Ellipse
            {
                Width = 48,
                Height = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2"))
            };

            LoadAvatarImageAsync(member.AvatarUrl, circle);

            Ellipse statusDot = new Ellipse
            {
                Width = 14,
                Height = 14,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#060609")),
                StrokeThickness = 2,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            avatarGrid.Children.Add(circle);
            avatarGrid.Children.Add(statusDot);

            TextBlock nickTxt = new TextBlock
            {
                Text = member.DisplayNick,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 6, 0, 0),
                TextAlignment = TextAlignment.Center
            };

            TextBlock handleTxt = new TextBlock
            {
                Text = member.Handle,
                FontSize = 9,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8E9297")),
                TextAlignment = TextAlignment.Center
            };

            panel.Children.Add(avatarGrid);
            panel.Children.Add(nickTxt);
            panel.Children.Add(handleTxt);
            container.Child = panel;

            return container;
        }

        private Border CreateTunnelCard(string title, string host, string ping, bool isPrivadito = false)
        {
            Border card = new Border
            {
                Width = 290,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D0D14")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isPrivadito ? "#FFD700" : "#1F1F30")),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 16, 16)
            };

            StackPanel panel = new StackPanel();
            TextBlock titleTxt = new TextBlock
            {
                Text = title,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            };

            TextBlock hostTxt = new TextBlock
            {
                Text = host,
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8E9297")),
                Margin = new Thickness(0, 0, 0, 12)
            };

            StackPanel statsRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 14)
            };

            TextBlock pingTxt = new TextBlock
            {
                Text = $"⚡ {ping}",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"))
            };

            statsRow.Children.Add(pingTxt);

            Button connectBtn = new Button
            {
                Content = isPrivadito ? "🔒 Conectar (Privadito)" : "Conectar al Túnel",
                Height = 36,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isPrivadito ? "#D4AF37" : "#5865F2")),
                Foreground = isPrivadito ? Brushes.Black : Brushes.White,
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
            connectBtn.Template = template;

            panel.Children.Add(titleTxt);
            panel.Children.Add(hostTxt);
            panel.Children.Add(statsRow);
            panel.Children.Add(connectBtn);

            card.Child = panel;
            return card;
        }

        private void CreateOverlayLayers(Grid rootGrid)
        {
            _dropdownOverlay = new Grid
            {
                Background = Brushes.Transparent,
                Visibility = Visibility.Collapsed
            };

            Border backdrop = new Border { Background = Brushes.Transparent };
            backdrop.MouseLeftButtonDown += (s, e) => _dropdownOverlay.Visibility = Visibility.Collapsed;
            _dropdownOverlay.Children.Add(backdrop);

            ProfileDropdownMenu dropdownMenu = new ProfileDropdownMenu(_user)
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 70, 24, 0)
            };

            dropdownMenu.OnLogoutRequested += (s, e) =>
            {
                _dropdownOverlay.Visibility = Visibility.Collapsed;
                _membersMonitor.Stop();
                ConfigManager.CurrentConfig.SavedAccessToken = null;
                ConfigManager.SaveConfig(ConfigManager.CurrentConfig);
                OnLogoutRequested?.Invoke(this, EventArgs.Empty);
            };

            _dropdownOverlay.Children.Add(dropdownMenu);
            rootGrid.Children.Add(_dropdownOverlay);

            _notificationsDrawer = new Border
            {
                Width = 330,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#101018")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#222236")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 70, 80, 0),
                Visibility = Visibility.Collapsed
            };

            StackPanel updatesPanel = new StackPanel();
            TextBlock updatesHeader = new TextBlock
            {
                Text = "🚀 Últimas Actualizaciones",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 12)
            };
            updatesPanel.Children.Add(updatesHeader);

            updatesPanel.Children.Add(CreateUpdateItem("v3.2 - Autenticación Infalsificable", "Sistema de validación remota en servidor de Discord."));
            updatesPanel.Children.Add(CreateUpdateItem("v3.1 - Auto-Login Silencioso", "Persistencia de sesión sin iniciar sesión en navegador."));

            _notificationsDrawer.Child = updatesPanel;
            rootGrid.Children.Add(_notificationsDrawer);
        }

        private Border CreateUpdateItem(string title, string desc)
        {
            Border b = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#161622")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 8)
            };
            StackPanel p = new StackPanel();
            TextBlock t = new TextBlock { Text = title, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
            TextBlock d = new TextBlock { Text = desc, FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA")), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) };
            p.Children.Add(t);
            p.Children.Add(d);
            b.Child = p;
            return b;
        }

        private void ToggleProfileDropdown()
        {
            _notificationsDrawer.Visibility = Visibility.Collapsed;
            _dropdownOverlay.Visibility = _dropdownOverlay.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ToggleNotificationsDrawer()
        {
            _dropdownOverlay.Visibility = Visibility.Collapsed;
            _notificationsDrawer.Visibility = _notificationsDrawer.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private Border Create4LevelProfileBadge(DiscordUser user)
        {
            Color roleColor = (Color)ColorConverter.ConvertFromString(user.PrimaryRoleColor);

            Border cardBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#14141E")),
                BorderBrush = new SolidColorBrush(roleColor),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(8),
                Effect = new DropShadowEffect
                {
                    Color = roleColor,
                    BlurRadius = 14,
                    Opacity = 0.35,
                    ShadowDepth = 0
                }
            };

            StackPanel contentPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            Ellipse avatarEllipse = new Ellipse
            {
                Width = 46,
                Height = 46,
                Margin = new Thickness(0, 0, 10, 0)
            };

            try
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(user.AvatarUrl, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                avatarEllipse.Fill = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill };
            }
            catch
            {
                avatarEllipse.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2"));
            }

            contentPanel.Children.Add(avatarEllipse);

            StackPanel infoPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            TextBlock nickBlock = new TextBlock
            {
                Text = user.DisplayNick,
                FontFamily = new FontFamily("Segoe UI, sans-serif"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 1)
            };

            TextBlock handleBlock = new TextBlock
            {
                Text = user.Handle,
                FontFamily = new FontFamily("Segoe UI, sans-serif"),
                FontSize = 10,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8E9297")),
                Margin = new Thickness(0, 0, 0, 3)
            };

            StackPanel rolesPanel = new StackPanel { Orientation = Orientation.Horizontal };

            Border rolePill = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(45, roleColor.R, roleColor.G, roleColor.B)),
                BorderBrush = new SolidColorBrush(roleColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(5, 1, 5, 1),
                Margin = new Thickness(0, 0, 4, 0)
            };

            TextBlock roleBlock = new TextBlock
            {
                Text = user.PrimaryRole.ToUpperInvariant(),
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(roleColor)
            };
            rolePill.Child = roleBlock;
            rolesPanel.Children.Add(rolePill);

            if (user.IsPrivadito)
            {
                Border privaditoPill = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(50, 255, 215, 0)),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(5, 1, 5, 1)
                };
                TextBlock privaditoblock = new TextBlock
                {
                    Text = "🔒 PRIVADITO",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700"))
                };
                privaditoPill.Child = privaditoblock;
                rolesPanel.Children.Add(privaditoPill);
            }

            infoPanel.Children.Add(nickBlock);
            infoPanel.Children.Add(handleBlock);
            infoPanel.Children.Add(rolesPanel);

            contentPanel.Children.Add(infoPanel);
            cardBorder.Child = contentPanel;

            return cardBorder;
        }
    }
}

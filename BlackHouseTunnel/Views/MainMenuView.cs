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
        public event EventHandler? OnReloadRequested;

        private readonly DiscordUser _user;
        private readonly OnlineMembersMonitor _membersMonitor;
        private readonly DiscordApiService _apiService = new DiscordApiService();
        private HostConsoleView? _activeHostConsoleView = null;
        private JoinConsoleView? _activeJoinConsoleView = null;
        private string _pendingJoinAddress = "";
        private static readonly System.Net.Http.HttpClient AvatarHttpClient = new System.Net.Http.HttpClient();

        private bool IsLight => ConfigManager.CurrentConfig.ThemeMode == "Light";
        private SolidColorBrush MainBgBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(IsLight ? "#F4F5F8" : "#060609"));
        private SolidColorBrush CardBgBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(IsLight ? "#FFFFFF" : "#0D0D15"));
        private SolidColorBrush CardBorderBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(IsLight ? "#E5E7EB" : "#1F1F30"));
        private SolidColorBrush TextPrimaryBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(IsLight ? "#111827" : "#FFFFFF"));
        private SolidColorBrush TextMutedBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(IsLight ? "#4B5563" : "#AAAAAA"));
        private SolidColorBrush InputBgBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(IsLight ? "#FFFFFF" : "#12121A"));
        private SolidColorBrush InputBorderBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(IsLight ? "#D1D5DB" : "#2A2A3E"));

        private Grid _rootGrid = null!;
        private Grid _dropdownOverlay = null!;
        private Grid _modalOverlay = null!;
        private Border _notificationsDrawer = null!;
        private Border _notifBadge = null!;
        private Grid _contentHostGrid = null!;
        private StackPanel _friendsRowPanel = null!;
        private bool _autoConnectPending = false;

        private Button _btnHome = null!;
        private Button _btnHost = null!;
        private Button _btnJoin = null!;
        private Button _btnRbxm = null!;
        private Button _btnRsm = null!;
        private Button _btnEcho = null!;
        private Button _btnSettings = null!;

        public bool HasActiveHost => _activeHostConsoleView != null;
        public bool HasActiveJoin => _activeJoinConsoleView != null;

        public MainMenuView(DiscordUser user)
        {
            _user = user;
            if (!string.IsNullOrWhiteSpace(ConfigManager.CurrentConfig.SavedUsername))
            {
                _user.CustomNickname = ConfigManager.CurrentConfig.SavedUsername;
            }
            _membersMonitor = new OnlineMembersMonitor(ConfigManager.CurrentConfig, _user);
            InitializeComponent();
            _membersMonitor.OnMembersUpdated += MembersMonitor_OnMembersUpdated;
            _membersMonitor.Start();
            DiscordRpcService.Initialize();
            Task.Run(() => UpdateService.CheckForUpdatesAsync());
        }

        private void InitializeComponent()
        {
            _rootGrid = new Grid();

            Grid appLayoutGrid = new Grid
            {
                Background = MainBgBrush
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
            string hostSvg = "M4 4h16a2 2 0 0 1 2 2v4a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2zm0 10h16a2 2 0 0 1 2 2v4a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2v-4a2 2 0 0 1 2-2zm2-6h.01M6 18h.01M16 8h2M16 18h2";
            string joinSvg = "M6 12h12M12 6v12";
            string rbxmSvg = "M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z";
            string rsmSvg = "M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z";
            string echoSvg = "M5 12.55a11 11 0 0 1 14.08 0M1.42 9a16 16 0 0 1 21.16 0M8.53 16.11a6 6 0 0 1 6.95 0M12 20h.01";
            string settingsSvg = "M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.38a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2zM12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z";

            _btnHome = CreateSidebarNavButton(homeSvg, LocalizationService.Get("nav_home"), () => SwitchTab("Home"));
            _btnHost = CreateSidebarNavButton(hostSvg, LocalizationService.Get("nav_host"), () => SwitchTab("Host"));
            _btnJoin = CreateSidebarNavButton(joinSvg, LocalizationService.Get("nav_join"), () => SwitchTab("Join"));
            _btnRbxm = CreateSidebarNavButton(rbxmSvg, LocalizationService.Get("nav_rbxm"), () => SwitchTab("Rbxm"));
            _btnRsm = CreateSidebarNavButton(rsmSvg, LocalizationService.Get("nav_rsm"), () => SwitchTab("Rsm"));
            _btnEcho = CreateSidebarNavButton(echoSvg, LocalizationService.Get("nav_echo"), () => SwitchTab("Echo"));
            _btnSettings = CreateSidebarNavButton(settingsSvg, LocalizationService.Get("nav_settings"), () => SwitchTab("Settings"));

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
                    if (_activeJoinConsoleView != null)
                    {
                        _contentHostGrid.Children.Add(_activeJoinConsoleView);
                    }
                    else
                    {
                        _contentHostGrid.Children.Add(BuildJoinView());
                    }
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

            if (_activeHostConsoleView == null && _activeJoinConsoleView == null)
            {
                switch (tabName)
                {
                    case "Home":
                        DiscordRpcService.UpdatePresence("En el Menú Principal", "Inicio / Dashboard");
                        break;
                    case "Host":
                        DiscordRpcService.UpdatePresence("En la pestaña Crear Host", "Configurando Servidor");
                        break;
                    case "Join":
                        DiscordRpcService.UpdatePresence("En la pestaña Unirse a Túnel", "Buscando Servidores");
                        break;
                    case "Rbxm":
                        DiscordRpcService.UpdatePresence("En Asistente de Modelos (.rbxm)", "Modelos 3D");
                        break;
                    case "Rsm":
                        DiscordRpcService.UpdatePresence("En Asistente RSM", "Herramientas Studio");
                        break;
                    case "Echo":
                        DiscordRpcService.UpdatePresence("En Diagnóstico Echo", "Probando Red");
                        break;
                    case "Settings":
                        DiscordRpcService.UpdatePresence("En Configuraciones", "Ajustando Opciones");
                        break;
                }
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
                Margin = new Thickness(0, -2, -2, 0),
                Visibility = Visibility.Collapsed
            };
            TextBlock badgeTxt = new TextBlock
            {
                Text = "0",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            badge.Child = badgeTxt;

            _notifBadge = badge;

            bellContainer.Children.Add(bellBtn);
            bellContainer.Children.Add(badge);

            // Auto-Updater Pill Button (Appears when update is available)
            Button updatePillBtn = new Button
            {
                Content = "🚀 Actualización Disponible",
                Height = 34,
                Padding = new Thickness(14, 0, 14, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 12, 0),
                Visibility = UpdateService.IsUpdateAvailable ? Visibility.Visible : Visibility.Collapsed
            };
            SetButtonCornerRadius(updatePillBtn, 17);

            updatePillBtn.Click += (s, e) =>
            {
                ShowDownloadAndUpdateModal();
            };

            UpdateService.OnUpdateStatusChanged += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    updatePillBtn.Visibility = UpdateService.IsUpdateAvailable ? Visibility.Visible : Visibility.Collapsed;
                    updatePillBtn.Content = $"🚀 v{UpdateService.LatestVersion} Disponible (Instalar)";
                });
            };

            rightHeaderActions.Children.Add(updatePillBtn);
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
                Background = ThemeManager.CardBgBrush,
                BorderBrush = ThemeManager.CardBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(24),
                Margin = new Thickness(0, 0, 0, 24)
            };

            StackPanel bannerPanel = new StackPanel();
            TextBlock welcomeTitle = new TextBlock
            {
                Text = LocalizationService.Get("home_welcome", _user.DisplayNick),
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = ThemeManager.TextPrimaryBrush,
                Margin = new Thickness(0, 0, 0, 0)
            };

            bannerPanel.Children.Add(welcomeTitle);

            if (_activeHostConsoleView != null)
            {
                Button hostActiveBtn = new Button
                {
                    Content = "🟢 TÚNEL HOST EN EJECUCIÓN (SERVIDOR) — Haz clic aquí para ver la sesión activa",
                    Height = 38,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                    Foreground = Brushes.White,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Margin = new Thickness(0, 14, 0, 0)
                };
                SetButtonCornerRadius(hostActiveBtn, 8);
                hostActiveBtn.Click += (s, e) => SwitchTab("Host");
                bannerPanel.Children.Add(hostActiveBtn);
            }
            else if (_activeJoinConsoleView != null)
            {
                Button joinActiveBtn = new Button
                {
                    Content = "🟢 TÚNEL DE CONEXIÓN EN EJECUCIÓN (CLIENTE) — Haz clic aquí para ver la sesión activa",
                    Height = 38,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                    Foreground = Brushes.White,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Margin = new Thickness(0, 14, 0, 0)
                };
                SetButtonCornerRadius(joinActiveBtn, 8);
                joinActiveBtn.Click += (s, e) => SwitchTab("Join");
                bannerPanel.Children.Add(joinActiveBtn);
            }
            banner.Child = bannerPanel;
            body.Children.Add(banner);

            TextBlock friendsHeader = new TextBlock
            {
                Text = LocalizationService.Get("home_online_members"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = ThemeManager.TextPrimaryBrush,
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

            // Active Tunnels Header Row with Reload Button 🔄
            StackPanel tunnelsHeaderRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };

            TextBlock tunnelsHeader = new TextBlock
            {
                Text = LocalizationService.Get("home_active_tunnels"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = ThemeManager.TextPrimaryBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            tunnelsHeaderRow.Children.Add(tunnelsHeader);

            Button reloadBtn = new Button
            {
                Content = "🔄",
                FontSize = 14,
                Width = 32,
                Height = 32,
                Background = ThemeManager.InputBgBrush,
                Foreground = ThemeManager.TextPrimaryBrush,
                BorderBrush = ThemeManager.InputBorderBrush,
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(10, 0, 0, 0),
                ToolTip = "Recargar Lista de Túneles Activos"
            };
            SetButtonCornerRadius(reloadBtn, 8);

            WrapPanel tunnelGrid = new WrapPanel();

            Func<Task> loadTunnelsAction = async () =>
            {
                var activeTunnels = await ActiveTunnelRegistry.GetVisibleTunnelsForUserAsync(_user);
                Dispatcher.Invoke(() =>
                {
                    tunnelGrid.Children.Clear();
                    if (activeTunnels.Count == 0)
                    {
                        tunnelGrid.Children.Add(new TextBlock
                        {
                            Text = LocalizationService.Get("home_no_tunnels"),
                            FontSize = 13,
                            Foreground = ThemeManager.TextMutedBrush,
                            Margin = new Thickness(0, 10, 0, 20)
                        });
                    }
                    else
                    {
                        foreach (var t in activeTunnels)
                        {
                            tunnelGrid.Children.Add(CreateTunnelCard(t.ServerName, $"Host: {t.HostUsername}", "22 ms", visibilityMode: t.VisibilityMode, remoteAddress: t.RemoteAddress));
                        }
                    }
                });
            };

            reloadBtn.Click += async (s, e) =>
            {
                reloadBtn.IsEnabled = false;
                reloadBtn.Content = "⏳";
                await loadTunnelsAction();
                _membersMonitor.Stop();
                _membersMonitor.Start();
                reloadBtn.Content = "🔄";
                reloadBtn.IsEnabled = true;
            };

            tunnelsHeaderRow.Children.Add(reloadBtn);
            body.Children.Add(tunnelsHeaderRow);

            Task.Run(loadTunnelsAction);

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

            StackPanel panel = new StackPanel { Margin = new Thickness(32), MaxWidth = 1100, HorizontalAlignment = HorizontalAlignment.Left };

            TextBlock title = new TextBlock
            {
                Text = LocalizationService.Get("host_title"),
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = ThemeManager.TextPrimaryBrush,
                Margin = new Thickness(0, 0, 0, 6)
            };

            TextBlock sub = new TextBlock
            {
                Text = LocalizationService.Get("host_sub"),
                FontSize = 13,
                Foreground = ThemeManager.TextMutedBrush,
                Margin = new Thickness(0, 0, 0, 24)
            };

            panel.Children.Add(title);
            panel.Children.Add(sub);

            Border box = new Border
            {
                Background = ThemeManager.CardBgBrush,
                BorderBrush = ThemeManager.CardBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(24)
            };

            StackPanel boxPanel = new StackPanel();

            // 2-Column Form Grid Layout
            Grid formGrid = new Grid();
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 0
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 1
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 2
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 3

            // Col 0, Row 0: ID de Usuario (UID)
            StackPanel uidPanel = new StackPanel();
            uidPanel.Children.Add(CreateLabel(LocalizationService.Get("lbl_uid")));
            TextBox uidBox = CreateStyledTextBox(ConfigManager.CurrentConfig.SavedUserId);
            uidPanel.Children.Add(uidBox);
            Grid.SetRow(uidPanel, 0); Grid.SetColumn(uidPanel, 0);
            formGrid.Children.Add(uidPanel);

            // Col 2, Row 0: Apodo en el Servidor (Username)
            StackPanel userPanel = new StackPanel();
            userPanel.Children.Add(CreateLabel(LocalizationService.Get("lbl_username")));
            string defaultUser = !string.IsNullOrWhiteSpace(ConfigManager.CurrentConfig.SavedUsername) ? ConfigManager.CurrentConfig.SavedUsername : _user.DisplayNick;
            TextBox userBox = CreateStyledTextBox(defaultUser);
            userPanel.Children.Add(userBox);
            Grid.SetRow(userPanel, 0); Grid.SetColumn(userPanel, 2);
            formGrid.Children.Add(userPanel);

            // Col 0, Row 1: Nombre del Servidor Túnel
            StackPanel namePanel = new StackPanel();
            namePanel.Children.Add(CreateLabel(LocalizationService.Get("lbl_server_name")));
            TextBox nameBox = CreateStyledTextBox(ConfigManager.CurrentConfig.SavedServerName);
            namePanel.Children.Add(nameBox);
            Grid.SetRow(namePanel, 1); Grid.SetColumn(namePanel, 0);
            formGrid.Children.Add(namePanel);

            // Col 2, Row 1: Puerto Local UDP
            StackPanel portPanel = new StackPanel();
            portPanel.Children.Add(CreateLabel(LocalizationService.Get("lbl_port")));
            TextBox portBox = CreateStyledTextBox(ConfigManager.CurrentConfig.SavedUdpPort.ToString());
            portPanel.Children.Add(portBox);
            Grid.SetRow(portPanel, 1); Grid.SetColumn(portPanel, 2);
            formGrid.Children.Add(portPanel);

            // Col 0, Row 2: Dirección del Túnel Remoto
            StackPanel addrPanel = new StackPanel();
            addrPanel.Children.Add(CreateLabel(LocalizationService.Get("lbl_addr")));
            TextBox addrBox = CreateStyledTextBox(ConfigManager.CurrentConfig.SavedRemoteHostAddress);
            addrPanel.Children.Add(addrBox);
            Grid.SetRow(addrPanel, 2); Grid.SetColumn(addrPanel, 0);
            formGrid.Children.Add(addrPanel);

            // Col 2, Row 2: Visibilidad (solo ComboBox, sin Key al lado)
            StackPanel visPanel = new StackPanel();
            visPanel.Children.Add(CreateLabel(LocalizationService.Get("lbl_vis")));
            ComboBox visCombo = CreateStyledComboBox();
            visCombo.Items.Add(LocalizationService.Get("vis_option_0"));
            visCombo.Items.Add(LocalizationService.Get("vis_option_1"));
            visCombo.Items.Add(LocalizationService.Get("vis_option_2"));
            visCombo.SelectedIndex = Math.Clamp(ConfigManager.CurrentConfig.SavedVisibilityOptionIndex, 0, 2);
            visPanel.Children.Add(visCombo);
            Grid.SetRow(visPanel, 2); Grid.SetColumn(visPanel, 2);
            formGrid.Children.Add(visPanel);

            // Placeholder for keyBox (created later in the separate Key card)
            TextBox keyBox = CreateStyledTextBox(ConfigManager.CurrentConfig.SavedAccessKey);

            // Row 3: Archivo de Mapa Roblox (Full width across 2 columns)
            StackPanel mapPanel = new StackPanel();
            mapPanel.Children.Add(CreateLabel(LocalizationService.Get("lbl_map")));
            Grid mapGrid = new Grid();
            mapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBox mapBox = CreateStyledTextBox(ConfigManager.CurrentConfig.SavedMapPath);
            Grid.SetColumn(mapBox, 0);
            mapGrid.Children.Add(mapBox);

            Button browseBtn = new Button
            {
                Content = "📁 Examinar...",
                Height = 38,
                Padding = new Thickness(16, 0, 16, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F1F30")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(10, 0, 0, 10)
            };
            SetButtonCornerRadius(browseBtn, 10);
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
            mapPanel.Children.Add(mapGrid);

            Grid.SetRow(mapPanel, 4); Grid.SetColumn(mapPanel, 0); Grid.SetColumnSpan(mapPanel, 3);
            formGrid.Children.Add(mapPanel);

            boxPanel.Children.Add(formGrid);

            // === SEPARATE KEY CARD (Only visible when Exclusivo con Rol is selected) ===
            Border keyCard = new Border
            {
                Background = ThemeManager.CardBgBrush,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 16, 0, 0),
                Visibility = visCombo.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed
            };

            StackPanel keyCardPanel = new StackPanel();

            TextBlock keyCardTitle = new TextBlock
            {
                Text = LocalizationService.Get("lbl_key"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700")),
                Margin = new Thickness(0, 0, 0, 6)
            };
            keyCardPanel.Children.Add(keyCardTitle);

            TextBlock keyCardHint = new TextBlock
            {
                Text = LocalizationService.Get("lbl_key_hint"),
                FontSize = 12,
                Foreground = ThemeManager.TextMutedBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            };
            keyCardPanel.Children.Add(keyCardHint);

            keyCardPanel.Children.Add(keyBox);
            keyCard.Child = keyCardPanel;

            // Toggle key card visibility based on visCombo
            visCombo.SelectionChanged += (s, e) =>
            {
                keyCard.Visibility = visCombo.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
            };

            // Custom Styled Checkbox Switch
            CheckBox publishCheck = new CheckBox
            {
                Content = "📢 Publicar Túnel en la Pantalla de Inicio (Home)",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700")),
                Margin = new Thickness(0, 12, 0, 20),
                IsChecked = true,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            ControlTemplate chkTemplate = new ControlTemplate(typeof(CheckBox));
            FrameworkElementFactory chkGrid = new FrameworkElementFactory(typeof(Grid));
            chkGrid.SetValue(Grid.BackgroundProperty, Brushes.Transparent);

            FrameworkElementFactory chkBoxBorder = new FrameworkElementFactory(typeof(Border));
            chkBoxBorder.Name = "BoxBorder";
            chkBoxBorder.SetValue(Border.WidthProperty, 22.0);
            chkBoxBorder.SetValue(Border.HeightProperty, 22.0);
            chkBoxBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            chkBoxBorder.SetValue(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#181824")));
            chkBoxBorder.SetValue(Border.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700")));
            chkBoxBorder.SetValue(Border.BorderThicknessProperty, new Thickness(2));
            chkBoxBorder.SetValue(Border.MarginProperty, new Thickness(0, 0, 10, 0));

            FrameworkElementFactory chkMark = new FrameworkElementFactory(typeof(TextBlock));
            chkMark.Name = "CheckMark";
            chkMark.SetValue(TextBlock.TextProperty, "✓");
            chkMark.SetValue(TextBlock.FontSizeProperty, 14.0);
            chkMark.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            chkMark.SetValue(TextBlock.ForegroundProperty, Brushes.White);
            chkMark.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            chkMark.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            chkBoxBorder.AppendChild(chkMark);

            FrameworkElementFactory chkContent = new FrameworkElementFactory(typeof(ContentPresenter));
            chkContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            FrameworkElementFactory chkStack = new FrameworkElementFactory(typeof(StackPanel));
            chkStack.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            chkStack.AppendChild(chkBoxBorder);
            chkStack.AppendChild(chkContent);

            chkTemplate.VisualTree = chkStack;

            Trigger checkedTrigger = new Trigger { Property = CheckBox.IsCheckedProperty, Value = true };
            checkedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")), "BoxBorder"));
            checkedTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700")), "BoxBorder"));
            checkedTrigger.Setters.Add(new Setter(TextBlock.VisibilityProperty, Visibility.Visible, "CheckMark"));

            Trigger uncheckedTrigger = new Trigger { Property = CheckBox.IsCheckedProperty, Value = false };
            uncheckedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#141420")), "BoxBorder"));
            uncheckedTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3A54")), "BoxBorder"));
            uncheckedTrigger.Setters.Add(new Setter(TextBlock.VisibilityProperty, Visibility.Collapsed, "CheckMark"));

            chkTemplate.Triggers.Add(checkedTrigger);
            chkTemplate.Triggers.Add(uncheckedTrigger);
            publishCheck.Template = chkTemplate;

            bool isAuthorizedHost = _user.IsCanHostOrManage;
            if (!isAuthorizedHost)
            {
                namePanel.Visibility = Visibility.Collapsed;
                visPanel.Visibility = Visibility.Collapsed;
                publishCheck.Visibility = Visibility.Collapsed;
                publishCheck.IsChecked = false;
            }

            boxPanel.Children.Add(publishCheck);

            // Action Buttons Row (Import Scripts + Start Host)
            StackPanel btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };

            Button importBtn = new Button
            {
                Content = LocalizationService.Get("btn_import_scripts"),
                Height = 44,
                Padding = new Thickness(20, 0, 20, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E2E")),
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 14, 0)
            };
            SetButtonCornerRadius(importBtn, 10);

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
                Content = LocalizationService.Get("btn_start_host"),
                Height = 44,
                Padding = new Thickness(28, 0, 28, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            SetButtonCornerRadius(startHostBtn, 10);

            startHostBtn.Click += async (s, e) =>
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
                    string accessKey = keyBox.Text.Trim();

                    // Save all form values to config.json in BlackHouseTunnel folder
                    ConfigManager.CurrentConfig.SavedUserId = targetUid;
                    ConfigManager.CurrentConfig.SavedUsername = targetUsername;
                    ConfigManager.CurrentConfig.SavedServerName = targetServerName;
                    ConfigManager.CurrentConfig.SavedUdpPort = targetPort;
                    ConfigManager.CurrentConfig.SavedRemoteHostAddress = addr;
                    ConfigManager.CurrentConfig.SavedMapPath = mapPath;
                    ConfigManager.CurrentConfig.SavedAccessKey = accessKey;
                    int targetVisMode = isAuthorizedHost ? visCombo.SelectedIndex : 0;
                    ConfigManager.CurrentConfig.SavedVisibilityOptionIndex = targetVisMode;
                    ConfigManager.SaveConfig(ConfigManager.CurrentConfig);

                    PluginInstaller.EnsurePluginInstalled(out string pluginMsg);
                    RbxmBridgeServer.ActiveUsername = targetUsername;
                    RbxmBridgeServer.ActiveUid = targetUid;
                    RbxmBridgeServer.Start();

                    string? sentMsgId = null;
                    if (isAuthorizedHost && publishCheck.IsChecked == true)
                    {
                        sentMsgId = await ActiveTunnelRegistry.PublishTunnelAsync(new PublishedTunnel
                        {
                            ServerName = string.IsNullOrWhiteSpace(targetServerName) ? $"Servidor de {targetUsername}" : targetServerName,
                            HostUsername = targetUsername,
                            RemoteAddress = addr,
                            VisibilityMode = targetVisMode,
                            AccessKey = accessKey
                        });
                    }

                    HostConsoleView hostConsole = new HostConsoleView(studioPath, targetUid, targetPort.ToString(), addr, mapPath, targetUsername, sentMsgId, isAuthorizedHost && publishCheck.IsChecked == true);
                    _activeHostConsoleView = hostConsole;
                    string hostUserToUnpublish = targetUsername;
                    string? msgIdToDelete = sentMsgId;
                    hostConsole.OnStopHostRequested += (s2, e2) =>
                    {
                        UdpProxy.StopProxy();
                        RbxmBridgeServer.Stop();
                        RobloxStudioService.ForceKillAllStudioProcesses();
                        Task.Run(() => ActiveTunnelRegistry.UnpublishTunnelAsync(hostUserToUnpublish, msgIdToDelete));
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

            // Horizontal layout: Main form (left) + Key card (right, only when Exclusivo con Rol)
            Grid hostLayout = new Grid();
            hostLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hostLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(box, 0);
            hostLayout.Children.Add(box);

            // Key card to the right side
            keyCard.Width = 260;
            keyCard.Margin = new Thickness(16, 0, 0, 0);
            keyCard.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetColumn(keyCard, 1);
            hostLayout.Children.Add(keyCard);

            panel.Children.Add(hostLayout);

            scroll.Content = panel;
            return scroll;
        }

        // TAB 3: JOIN TUNNEL VIEW
        private string _activeTargetAddress = "";

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
                Text = LocalizationService.Get("join_title"),
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = ThemeManager.TextPrimaryBrush,
                Margin = new Thickness(0, 0, 0, 6)
            };

            TextBlock sub = new TextBlock
            {
                Text = LocalizationService.Get("join_sub"),
                FontSize = 13,
                Foreground = ThemeManager.TextMutedBrush,
                Margin = new Thickness(0, 0, 0, 24)
            };

            panel.Children.Add(title);
            panel.Children.Add(sub);

            Border box = new Border
            {
                Background = ThemeManager.CardBgBrush,
                BorderBrush = ThemeManager.CardBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(24)
            };

            StackPanel boxPanel = new StackPanel();

            boxPanel.Children.Add(CreateLabel("Mi Apodo en Roblox (Username)"));
            var currentCfg = ConfigManager.CurrentConfig;
            string defaultUser = !string.IsNullOrWhiteSpace(currentCfg.SavedUsername) ? currentCfg.SavedUsername : _user.DisplayNick;
            TextBox userBox = CreateStyledTextBox(defaultUser);
            userBox.TextChanged += (s, e) =>
            {
                string val = userBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    var cfg = ConfigManager.CurrentConfig;
                    cfg.SavedUsername = val;
                    ConfigManager.SaveConfig(cfg);
                    _user.ServerNick = val;
                    _user.GlobalName = val;
                }
            };
            boxPanel.Children.Add(userBox);

            boxPanel.Children.Add(CreateLabel("Dirección del Túnel (Entrada Manual Opcional)"));
            if (!string.IsNullOrEmpty(_pendingJoinAddress))
            {
                _activeTargetAddress = _pendingJoinAddress;
                _pendingJoinAddress = "";
            }

            string displayAddr = !string.IsNullOrEmpty(_activeTargetAddress) ? "🔒 Túnel Protegido (Servidor Seleccionado)" : "";
            TextBox addrBox = CreateStyledTextBox(displayAddr);
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
            SetButtonCornerRadius(connectBtn, 10);

            connectBtn.Click += (s, e) =>
            {
                try
                {
                    string studioPath = RobloxStudioService.GetStudioPath();
                    if (string.IsNullOrEmpty(studioPath))
                    {
                        DarkMessageBox.Show("No se encontró una instalación ejecutable de Roblox Studio en tu sistema.", "Error Roblox Studio", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string inputAddr = addrBox.Text.Trim();
                    string targetAddressToUse = "";

                    if (inputAddr.Contains("🔒") || string.IsNullOrEmpty(inputAddr))
                    {
                        targetAddressToUse = _activeTargetAddress;
                    }
                    else
                    {
                        targetAddressToUse = inputAddr;
                    }

                    if (string.IsNullOrEmpty(targetAddressToUse))
                    {
                        DarkMessageBox.Show("Por favor ingresa o selecciona una dirección de túnel válida.", "Error Dirección", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var publishedTunnels = ActiveTunnelRegistry.GetVisibleTunnelsForUser(_user);
                    var matchingTunnel = publishedTunnels.FirstOrDefault(t => t.RemoteAddress.Equals(targetAddressToUse, StringComparison.OrdinalIgnoreCase));
                    if (matchingTunnel != null)
                    {
                        if (matchingTunnel.VisibilityMode == 1 && !_user.IsMemberOfGuild && !_user.IsStaffOrAdmin)
                        {
                            DarkMessageBox.Show("🔒 Túnel Cerrado: Este host está configurado con acceso 'Servidor'. Solo los miembros del Servidor de Discord de BlackHouse pueden unirse.", "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        if (matchingTunnel.VisibilityMode == 2 && !_user.IsPrivadito && !_user.IsStaffOrAdmin)
                        {
                            DarkMessageBox.Show("🔒 Túnel Cerrado: Este host está configurado con acceso 'Privadito'. Solo los miembros con el Rol Privadito pueden unirse.", "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    var parts = targetAddressToUse.Split(':');
                    string dstHost = parts[0];
                    int dstPort = parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : 55555;
                    string username = string.IsNullOrWhiteSpace(userBox.Text) ? _user.DisplayNick : userBox.Text.Trim();

                    JoinConsoleView joinConsole = new JoinConsoleView(studioPath, dstHost, dstPort, username);
                    _activeJoinConsoleView = joinConsole;
                    joinConsole.OnDisconnectRequested += (s2, e2) =>
                    {
                        _activeJoinConsoleView = null;
                        SwitchTab("Join");
                    };

                    _contentHostGrid.Children.Clear();
                    _contentHostGrid.Children.Add(joinConsole);
                }
                catch (Exception ex)
                {
                    DarkMessageBox.Show($"Error al conectar al Túnel: {ex.Message}", "Error Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            boxPanel.Children.Add(connectBtn);
            box.Child = boxPanel;
            panel.Children.Add(box);

            if (_autoConnectPending)
            {
                _autoConnectPending = false;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    connectBtn.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }

            scroll.Content = panel;
            return scroll;
        }

        // TAB 4: SETTINGS VIEW
        // TAB 7: SYSTEM SETTINGS VIEW
        private UIElement BuildSettingsView()
        {
            var config = ConfigManager.CurrentConfig;

            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(32), MaxWidth = 650, HorizontalAlignment = HorizontalAlignment.Left };

            TextBlock title = new TextBlock
            {
                Text = LocalizationService.Get("settings_title"),
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = TextPrimaryBrush,
                Margin = new Thickness(0, 0, 0, 24)
            };

            panel.Children.Add(title);

            Border box = new Border
            {
                Background = CardBgBrush,
                BorderBrush = CardBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(24)
            };

            StackPanel boxPanel = new StackPanel();

            // Section 1: Language & Theme
            boxPanel.Children.Add(CreateSectionHeader(LocalizationService.Get("settings_lang_theme")));

            boxPanel.Children.Add(CreateLabel(LocalizationService.Get("settings_lang_lbl")));
            ComboBox langCombo = CreateStyledComboBox();
            langCombo.Items.Add("🇪🇸 Español (Spanish)");
            langCombo.Items.Add("🇺🇸 English (Inglés)");
            langCombo.Items.Add("🇧🇷 Português (Portugués)");
            langCombo.SelectedIndex = config.Language.ToLowerInvariant() switch
            {
                "en" => 1,
                "pt" => 2,
                _ => 0
            };
            langCombo.SelectionChanged += (s, e) =>
            {
                string newLang = langCombo.SelectedIndex switch
                {
                    1 => "en",
                    2 => "pt",
                    _ => "es"
                };
                if (config.Language != newLang)
                {
                    config.Language = newLang;
                    LocalizationService.CurrentLanguage = newLang;
                    ConfigManager.SaveConfig(config);
                    OnReloadRequested?.Invoke(this, EventArgs.Empty);
                }
            };
            boxPanel.Children.Add(langCombo);

            boxPanel.Children.Add(CreateLabel(LocalizationService.Get("settings_theme_lbl")));
            ComboBox themeCombo = CreateStyledComboBox();
            themeCombo.Items.Add(LocalizationService.Get("settings_theme_dark"));
            themeCombo.Items.Add(LocalizationService.Get("settings_theme_light"));
            themeCombo.SelectedIndex = config.ThemeMode == "Light" ? 1 : 0;
            themeCombo.SelectionChanged += (s, e) =>
            {
                string newTheme = themeCombo.SelectedIndex == 1 ? "Light" : "Dark";
                if (config.ThemeMode != newTheme)
                {
                    config.ThemeMode = newTheme;
                    ConfigManager.SaveConfig(config);
                    OnReloadRequested?.Invoke(this, EventArgs.Empty);
                }
            };
            boxPanel.Children.Add(themeCombo);

            // Section 2: Discord Rich Presence
            boxPanel.Children.Add(CreateSectionHeader(LocalizationService.Get("settings_discord_sec")));

            Border rpcToggle = CreateModernToggleSwitch(
                LocalizationService.Get("settings_rpc_toggle"),
                config.EnableDiscordRpc,
                (enabled) =>
                {
                    config.EnableDiscordRpc = enabled;
                    ConfigManager.SaveConfig(config);
                    if (enabled)
                    {
                        DiscordRpcService.SetPresenceInMenu();
                    }
                    else
                    {
                        DiscordRpcService.ClearPresence();
                    }
                });
            boxPanel.Children.Add(rpcToggle);

            // Section 3: Roblox Studio Maintenance
            boxPanel.Children.Add(CreateSectionHeader(LocalizationService.Get("settings_maint_sec")));

            boxPanel.Children.Add(CreateLabel(LocalizationService.Get("settings_studio_path")));
            string currentStudio = !string.IsNullOrEmpty(config.SelectedStudioPath) && File.Exists(config.SelectedStudioPath)
                ? config.SelectedStudioPath
                : (RobloxStudioService.GetStudioPath() ?? "No detectado");

            TextBox studioBox = CreateStyledTextBox(currentStudio);
            studioBox.IsReadOnly = true;
            studioBox.Background = InputBgBrush;
            studioBox.Foreground = TextMutedBrush;
            boxPanel.Children.Add(studioBox);

            WrapPanel studioBtnsRow = new WrapPanel { Margin = new Thickness(0, 0, 0, 24) };

            Button scanStudioBtn = new Button
            {
                Content = LocalizationService.Get("btn_scan_studio"),
                Height = 40,
                Padding = new Thickness(14, 0, 14, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 10, 10)
            };
            SetButtonCornerRadius(scanStudioBtn, 10);

            scanStudioBtn.Click += (s, e) =>
            {
                StudioSelectorModal modal = new StudioSelectorModal(studioBox.Text);
                modal.OnStudioSelected += (s2, path) =>
                {
                    studioBox.Text = path;
                    config.SelectedStudioPath = path;
                    ConfigManager.SaveConfig(config);
                    _modalOverlay.Children.Clear();
                    _modalOverlay.Visibility = Visibility.Collapsed;
                };
                modal.OnCloseRequested += (s2, e2) =>
                {
                    _modalOverlay.Children.Clear();
                    _modalOverlay.Visibility = Visibility.Collapsed;
                };
                _modalOverlay.Children.Clear();
                _modalOverlay.Children.Add(modal);
                _modalOverlay.Visibility = Visibility.Visible;
            };
            studioBtnsRow.Children.Add(scanStudioBtn);

            Button browseStudioBtn = new Button
            {
                Content = LocalizationService.Get("btn_browse_studio"),
                Height = 40,
                Padding = new Thickness(14, 0, 14, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F1F32")),
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 10, 10)
            };
            SetButtonCornerRadius(browseStudioBtn, 10);

            browseStudioBtn.Click += (s, e) =>
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Seleccionar Ejecutable de Roblox Studio",
                    Filter = "Roblox Studio (*.exe)|*.exe|Todos los archivos (*.*)|*.*"
                };
                if (dlg.ShowDialog() == true)
                {
                    studioBox.Text = dlg.FileName;
                    config.SelectedStudioPath = dlg.FileName;
                    ConfigManager.SaveConfig(config);
                }
            };
            studioBtnsRow.Children.Add(browseStudioBtn);

            Button reinstallStudioBtn = new Button
            {
                Content = LocalizationService.Get("btn_reinstall_studio"),
                Height = 40,
                Padding = new Thickness(14, 0, 14, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 0, 10)
            };
            SetButtonCornerRadius(reinstallStudioBtn, 10);

            reinstallStudioBtn.Click += async (s, e) =>
            {
                try
                {
                    reinstallStudioBtn.IsEnabled = false;
                    reinstallStudioBtn.Content = "⏳ Descargando Instalador de GitHub...";

                    string tempInstaller = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RobloxStudioInstaller.exe");
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        byte[] data = await client.GetByteArrayAsync("https://raw.githubusercontent.com/Carlitos999yt/BlackHouseTunnel/main/RobloxInstaller/RobloxStudioInstaller.exe");
                        await System.IO.File.WriteAllBytesAsync(tempInstaller, data);
                    }

                    reinstallStudioBtn.Content = "🚀 Ejecutando Instalador...";
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(tempInstaller) { UseShellExecute = true });
                    DarkMessageBox.Show("¡Instalador de Roblox Studio descargado desde GitHub y ejecutado en pantalla!", "Instalador Iniciado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[ReinstallStudio] {ex.Message}");
                    DarkMessageBox.Show($"Error al descargar el instalador desde GitHub: {ex.Message}", "Error de Instalación", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    reinstallStudioBtn.IsEnabled = true;
                    reinstallStudioBtn.Content = LocalizationService.Get("btn_reinstall_studio");
                }
            };
            studioBtnsRow.Children.Add(reinstallStudioBtn);
            boxPanel.Children.Add(studioBtnsRow);

            // Section 4: Auto-Updater
            boxPanel.Children.Add(CreateSectionHeader(LocalizationService.Get("settings_updates_sec")));
            boxPanel.Children.Add(CreateLabel($"Versión Actual de la Aplicación: v{UpdateService.CurrentVersion}"));

            TextBlock updateStatusLbl = new TextBlock
            {
                Text = UpdateService.IsUpdateAvailable ? $"¡Nueva versión v{UpdateService.LatestVersion} disponible para instalar!" : "Tu aplicación está actualizada.",
                FontSize = 13,
                Foreground = UpdateService.IsUpdateAvailable ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")) : ThemeManager.TextMutedBrush,
                Margin = new Thickness(0, 0, 0, 12)
            };
            boxPanel.Children.Add(updateStatusLbl);

            Button checkUpdateBtn = new Button
            {
                Content = LocalizationService.Get("btn_check_updates"),
                Height = 40,
                Padding = new Thickness(14, 0, 14, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            SetButtonCornerRadius(checkUpdateBtn, 10);

            checkUpdateBtn.Click += async (s, e) =>
            {
                checkUpdateBtn.IsEnabled = false;
                checkUpdateBtn.Content = "⏳ Buscando en GitHub...";
                updateStatusLbl.Text = "Verificando versiones en GitHub...";

                await UpdateService.CheckForUpdatesAsync();

                if (UpdateService.IsUpdateAvailable)
                {
                    updateStatusLbl.Text = $"¡Actualización v{UpdateService.LatestVersion} encontrada!";
                    updateStatusLbl.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                    ShowDownloadAndUpdateModal();
                }
                else
                {
                    updateStatusLbl.Text = $"¡Tienes la versión más reciente (v{UpdateService.CurrentVersion})!";
                    updateStatusLbl.Foreground = ThemeManager.TextMutedBrush;
                }
                checkUpdateBtn.IsEnabled = true;
                checkUpdateBtn.Content = LocalizationService.Get("btn_check_updates");
            };
            boxPanel.Children.Add(checkUpdateBtn);

            box.Child = boxPanel;
            panel.Children.Add(box);

            scroll.Content = panel;
            return scroll;
        }

        private void ShowDownloadAndUpdateModal()
        {
            Grid modalRoot = new Grid { Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)) };
            Border card = new Border
            {
                Width = 440,
                Background = ThemeManager.CardBgBrush,
                BorderBrush = ThemeManager.CardBorderBrush,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(24),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            StackPanel stack = new StackPanel();

            TextBlock title = new TextBlock
            {
                Text = $"📦 Actualización Disponible (v{UpdateService.LatestVersion})",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = ThemeManager.TextPrimaryBrush,
                Margin = new Thickness(0, 0, 0, 8)
            };
            stack.Children.Add(title);

            TextBlock sub = new TextBlock
            {
                Text = "Presiona 'Descargar e Instalar' para obtener la versión oficial desde GitHub con el progreso en tiempo real.",
                FontSize = 13,
                Foreground = ThemeManager.TextMutedBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20)
            };
            stack.Children.Add(sub);

            ProgressBar progressBar = new ProgressBar
            {
                Height = 12,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Background = ThemeManager.InputBgBrush,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                Margin = new Thickness(0, 0, 0, 10)
            };
            stack.Children.Add(progressBar);

            TextBlock progressLbl = new TextBlock
            {
                Text = "Listo para iniciar descarga.",
                FontSize = 12,
                Foreground = ThemeManager.TextMutedBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            stack.Children.Add(progressLbl);

            StackPanel btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            Button cancelBtn = new Button
            {
                Content = LocalizationService.Get("btn_cancel"),
                Height = 38,
                Padding = new Thickness(16, 0, 16, 0),
                Background = ThemeManager.InputBgBrush,
                Foreground = ThemeManager.TextPrimaryBrush,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 10, 0)
            };
            SetButtonCornerRadius(cancelBtn, 8);
            cancelBtn.Click += (s, e) =>
            {
                _modalOverlay.Children.Clear();
                _modalOverlay.Visibility = Visibility.Collapsed;
            };
            btns.Children.Add(cancelBtn);

            Button actionBtn = new Button
            {
                Content = "⬇️ Descargar e Instalar",
                Height = 38,
                Padding = new Thickness(20, 0, 20, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            SetButtonCornerRadius(actionBtn, 8);

            bool isDownloaded = false;

            actionBtn.Click += async (s, e) =>
            {
                if (isDownloaded)
                {
                    actionBtn.IsEnabled = false;
                    actionBtn.Content = "⚡ Reiniciando...";
                    UpdateService.ApplyUpdateAndRestart();
                    return;
                }

                actionBtn.IsEnabled = false;
                cancelBtn.IsEnabled = false;
                actionBtn.Content = "⏳ Descargando...";

                bool success = await UpdateService.DownloadUpdateWithProgressAsync((read, total, percent) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        progressBar.Value = percent;
                        if (total > 0)
                        {
                            double mbRead = (double)read / (1024 * 1024);
                            double mbTotal = (double)total / (1024 * 1024);
                            progressLbl.Text = $"Descargando: {mbRead:F1} MB / {mbTotal:F1} MB ({percent:F0}%)";
                        }
                        else
                        {
                            progressLbl.Text = $"Descargando: {read / (1024 * 1024):F1} MB";
                        }
                    });
                });

                if (success)
                {
                    isDownloaded = true;
                    progressBar.Value = 100;
                    progressLbl.Text = "✅ ¡Descarga completada con éxito!";
                    progressLbl.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                    actionBtn.Content = "⚡ Reiniciar e Instalar Ahora";
                    actionBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                    actionBtn.IsEnabled = true;
                    cancelBtn.IsEnabled = true;
                }
                else
                {
                    progressLbl.Text = "❌ Error en la descarga. Por favor intenta de nuevo.";
                    progressLbl.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ED4245"));
                    actionBtn.Content = "🔄 Reintentar Descarga";
                    actionBtn.IsEnabled = true;
                    cancelBtn.IsEnabled = true;
                }
            };

            btns.Children.Add(actionBtn);
            stack.Children.Add(btns);

            card.Child = stack;
            modalRoot.Children.Add(card);

            _modalOverlay.Children.Clear();
            _modalOverlay.Children.Add(modalRoot);
            _modalOverlay.Visibility = Visibility.Visible;
        }

        private void SetButtonCornerRadius(Button btn, double radius = 8)
        {
            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
            FrameworkElementFactory presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);
            template.VisualTree = borderFactory;
            btn.Template = template;
        }

        private ComboBox CreateStyledComboBox()
        {
            ComboBox combo = new ComboBox
            {
                Height = 38,
                Background = ThemeManager.InputBgBrush,
                Foreground = ThemeManager.TextPrimaryBrush,
                BorderBrush = ThemeManager.InputBorderBrush,
                BorderThickness = new Thickness(1),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 16),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            // Round the popup dropdown dynamically when it opens
            combo.DropDownOpened += (s, e) =>
            {
                if (s is ComboBox cb)
                {
                    // Find the Popup in the visual tree and round its child border
                    var popup = FindVisualChild<System.Windows.Controls.Primitives.Popup>(cb);
                    if (popup?.Child is Border popupBorder)
                    {
                        popupBorder.CornerRadius = new CornerRadius(10);
                        popupBorder.Background = ThemeManager.CardBgBrush;
                        popupBorder.BorderBrush = ThemeManager.InputBorderBrush;
                        popupBorder.BorderThickness = new Thickness(1);
                        popupBorder.Padding = new Thickness(4);
                    }
                }
            };

            // Item container style (each dropdown item with rounded corners)
            Style itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, ThemeManager.CardBgBrush));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, ThemeManager.TextPrimaryBrush));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.PaddingProperty, new Thickness(12, 8, 12, 8)));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.CursorProperty, System.Windows.Input.Cursors.Hand));

            // Create item template with rounded corners per item
            ControlTemplate itemTemplate = new ControlTemplate(typeof(ComboBoxItem));
            FrameworkElementFactory itemBorder = new FrameworkElementFactory(typeof(Border));
            itemBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(ComboBoxItem.BackgroundProperty));
            itemBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            itemBorder.SetValue(Border.PaddingProperty, new TemplateBindingExtension(ComboBoxItem.PaddingProperty));
            FrameworkElementFactory itemContent = new FrameworkElementFactory(typeof(ContentPresenter));
            itemBorder.AppendChild(itemContent);
            itemTemplate.VisualTree = itemBorder;
            itemStyle.Setters.Add(new Setter(ComboBoxItem.TemplateProperty, itemTemplate));

            Trigger hoverTrigger = new Trigger { Property = ComboBoxItem.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, ThemeManager.IsLight ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#202030"))));
            hoverTrigger.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, ThemeManager.TextPrimaryBrush));
            itemStyle.Triggers.Add(hoverTrigger);

            combo.ItemContainerStyle = itemStyle;
            return combo;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T found) return found;
                T? result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        private Border CreateModernToggleSwitch(string labelText, bool initialValue, Action<bool> onChanged)
        {
            bool isChecked = initialValue;

            StackPanel mainPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            Border track = new Border
            {
                Width = 46,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = isChecked ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2B2D31")),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 12, 0),
                Padding = new Thickness(3)
            };

            Ellipse thumb = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = Brushes.White,
                HorizontalAlignment = isChecked ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            track.Child = thumb;

            TextBlock label = new TextBlock
            {
                Text = labelText,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            mainPanel.Children.Add(track);
            mainPanel.Children.Add(label);

            Border wrapper = new Border { Child = mainPanel, Margin = new Thickness(0, 4, 0, 20), Cursor = System.Windows.Input.Cursors.Hand };

            wrapper.MouseLeftButtonDown += (s, e) =>
            {
                isChecked = !isChecked;
                track.Background = isChecked ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2B2D31"));
                thumb.HorizontalAlignment = isChecked ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                onChanged?.Invoke(isChecked);
            };

            return wrapper;
        }

        private void ShowEditNicknameModal()
        {
            Grid modalRoot = new Grid { Background = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)) };
            Border card = new Border
            {
                Width = 360,
                Background = ThemeManager.CardBgBrush,
                BorderBrush = ThemeManager.CardBorderBrush,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(24),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            StackPanel stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("modal_nick_title"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = ThemeManager.TextPrimaryBrush,
                Margin = new Thickness(0, 0, 0, 16)
            });

            stack.Children.Add(CreateLabel(LocalizationService.Get("modal_nick_sub")));
            TextBox nickBox = CreateStyledTextBox(_user.DisplayNick);
            stack.Children.Add(nickBox);

            StackPanel btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };

            Button cancelBtn = new Button
            {
                Content = LocalizationService.Get("btn_cancel"),
                Height = 36,
                Padding = new Thickness(16, 0, 16, 0),
                Background = ThemeManager.InputBgBrush,
                Foreground = ThemeManager.TextPrimaryBrush,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 10, 0)
            };
            SetButtonCornerRadius(cancelBtn, 8);
            cancelBtn.Click += (s, e) =>
            {
                _modalOverlay.Children.Clear();
                _modalOverlay.Visibility = Visibility.Collapsed;
            };
            btns.Children.Add(cancelBtn);

            Button saveBtn = new Button
            {
                Content = LocalizationService.Get("btn_save"),
                Height = 36,
                Padding = new Thickness(16, 0, 16, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            SetButtonCornerRadius(saveBtn, 8);
            saveBtn.Click += async (s, e) =>
            {
                string newNick = nickBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(newNick))
                {
                    saveBtn.IsEnabled = false;
                    saveBtn.Content = "⏳ Guardando...";
                    _user.CustomNickname = newNick;
                    ConfigManager.CurrentConfig.SavedUsername = newNick;
                    ConfigManager.SaveConfig(ConfigManager.CurrentConfig);

                    // Update nickname in Discord Server automatically via Discord API
                    var (success, errorMsg) = await _apiService.UpdateGuildMemberNicknameAsync(
                        ConfigManager.CurrentConfig.SavedAccessToken ?? "",
                        "1529015986135502951",
                        _user.Id,
                        newNick
                    );

                    _modalOverlay.Children.Clear();
                    _modalOverlay.Visibility = Visibility.Collapsed;
                    OnReloadRequested?.Invoke(this, EventArgs.Empty);

                    if (!success)
                    {
                        DarkMessageBox.Show($"Tu apodo fue guardado localmente en la app como '{newNick}'.\n\nSin embargo, no se pudo sincronizar en el Servidor de Discord.\n\nMotivo:\n{errorMsg}", "Aviso de Apodo en Discord", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        DarkMessageBox.Show($"¡Tu apodo ha sido actualizado correctamente a '{newNick}' tanto en la aplicación como en el Servidor de Discord!", "Apodo Sincronizado", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            };
            btns.Children.Add(saveBtn);

            stack.Children.Add(btns);
            card.Child = stack;
            modalRoot.Children.Add(card);

            _modalOverlay.Children.Clear();
            _modalOverlay.Children.Add(modalRoot);
            _modalOverlay.Visibility = Visibility.Visible;
        }

        private TextBlock CreateSectionHeader(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                Margin = new Thickness(0, 16, 0, 10)
            };
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
            TextBox tb = new TextBox
            {
                Text = defaultText,
                Height = 38,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#12121A")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A3E")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 6, 10, 6),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10),
                VerticalContentAlignment = VerticalAlignment.Center
            };

            ControlTemplate template = new ControlTemplate(typeof(TextBox));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "border";
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(TextBox.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(TextBox.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(TextBox.BorderThicknessProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));

            FrameworkElementFactory scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewer.Name = "PART_ContentHost";
            scrollViewer.SetValue(ScrollViewer.MarginProperty, new Thickness(4, 0, 4, 0));
            scrollViewer.SetValue(ScrollViewer.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(scrollViewer);
            template.VisualTree = border;
            tb.Template = template;

            return tb;
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

        private Border CreateTunnelCard(string title, string host, string ping, int visibilityMode = 0, string remoteAddress = "", string accessKey = "")
        {
            bool isPrivadito = visibilityMode == 2;
            bool isServidor = visibilityMode == 1;
            bool reqKey = !string.IsNullOrWhiteSpace(accessKey);

            string borderHex = isPrivadito ? "#FFD700" : (isServidor ? "#38BDF8" : "#4B5563");
            string badgeBgHex = isPrivadito ? "#3A2E00" : (isServidor ? "#0C4A6E" : "#1F2937");
            string badgeBorderHex = isPrivadito ? "#FFD700" : (isServidor ? "#38BDF8" : "#4B5563");
            string badgeTextHex = isPrivadito ? "#FFD700" : (isServidor ? "#38BDF8" : "#9CA3AF");
            string badgeLabel = isPrivadito ? (reqKey ? "🔑 Privadito (Requiere Llave)" : "🔒 Privadito (Rol Exclusivo)") : (isServidor ? "🛡️ Host Servidor (Miembros)" : "🌐 Host Público (Global)");
            string btnBgHex = isPrivadito ? "#F59E0B" : (isServidor ? "#0284C7" : "#4B5563");
            string btnText = isPrivadito ? (reqKey ? "🔑 Conectarse con Llave" : "🔒 Conectarse (Privadito)") : (isServidor ? "🛡️ Conectarse (Servidor)" : "🔌 Conectarse al Túnel");

            Border card = new Border
            {
                Width = 290,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D0D14")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderHex)),
                BorderThickness = new Thickness(isPrivadito || isServidor ? 1.8 : 1.2),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 16, 16)
            };

            StackPanel panel = new StackPanel();

            // Scope Tag Badge at top
            Border badge = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(badgeBgHex)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(badgeBorderHex)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 0, 8)
            };
            TextBlock badgeTxt = new TextBlock
            {
                Text = badgeLabel,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(badgeTextHex))
            };
            badge.Child = badgeTxt;
            panel.Children.Add(badge);

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
                Content = btnText,
                Height = 40,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(btnBgHex)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            SetButtonCornerRadius(connectBtn, 10);

            string targetAddr = remoteAddress;
            connectBtn.Click += (s, e) =>
            {
                if (isPrivadito && !(_user.IsPrivadito || _user.IsStaffOrAdmin))
                {
                    DarkMessageBox.Show("⛔ ACCESO RESTRICTO Y DENEGADO\n\nEste túnel está reservado exclusivamente para usuarios con el rol 'Privadito' en nuestro servidor de Discord.\n\nRoblox Studio NO se ejecutará.", "Acceso Restringido", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }

                if (isServidor && !(_user.IsMemberOfGuild || _user.IsStaffOrAdmin))
                {
                    DarkMessageBox.Show("⛔ ACCESO RESTRICTO Y DENEGADO\n\nEste túnel está configurado como 'Host para el Servidor'. Para conectarte debes ser miembro verificado de nuestro servidor de Discord.\n\nRoblox Studio NO se ejecutará.", "Acceso Restringido", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }

                if (reqKey)
                {
                    string inputKey = PromptPasswordDialog("🔐 Llave de Acceso Requerida", $"El host '{title}' requiere una Llave de Acceso privada para ingresar.\n\nPor favor ingresa la clave asignada por el Host:");
                    if (string.IsNullOrWhiteSpace(inputKey) || !inputKey.Equals(accessKey, StringComparison.OrdinalIgnoreCase))
                    {
                        DarkMessageBox.Show("⛔ LLAVE DE ACCESO INCORRECTA\n\nLa clave ingresada es incorrecta o está vacía. Acceso Denegado.", "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                if (_activeJoinConsoleView != null)
                {
                    var result = DarkMessageBox.Show("Ya tienes un túnel de conexión activo. ¿Deseas desconectarte del actual para unirte a este nuevo túnel?", "Túnel Activo", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        UdpProxy.StopProxy();
                        RobloxStudioService.ForceKillAllStudioProcesses();
                        _activeJoinConsoleView = null;
                    }
                    else
                    {
                        SwitchTab("Join");
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(targetAddr))
                {
                    _pendingJoinAddress = targetAddr;
                    _autoConnectPending = true;
                }
                SwitchTab("Join");
            };

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

            dropdownMenu.OnEditNickRequested += (s, e) =>
            {
                _dropdownOverlay.Visibility = Visibility.Collapsed;
                ShowEditNicknameModal();
            };

            _dropdownOverlay.Children.Add(dropdownMenu);
            rootGrid.Children.Add(_dropdownOverlay);

            _modalOverlay = new Grid
            {
                Background = Brushes.Transparent,
                Visibility = Visibility.Collapsed
            };
            rootGrid.Children.Add(_modalOverlay);

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
                Text = "🔔 Notificaciones del Túnel",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 12)
            };
            updatesPanel.Children.Add(updatesHeader);

            if (_user.IsPrivadito || _user.IsStaffOrAdmin)
            {
                updatesPanel.Children.Add(CreateUpdateItem("🔒 Túnel Privadito Disponible", "Tienes acceso a los servidores exclusivos del Rol Privadito y del Servidor de Discord."));
            }
            else
            {
                updatesPanel.Children.Add(CreateUpdateItem("🌐 Túneles de Servidor Activos", "Tienes acceso a los túneles abiertos del Servidor de Discord de BlackHouse."));
            }

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
            _notifBadge.Visibility = Visibility.Collapsed;
            _notificationsDrawer.Visibility = _notificationsDrawer.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private Border Create4LevelProfileBadge(DiscordUser user)
        {
            Color roleColor = (Color)ColorConverter.ConvertFromString(user.PrimaryRoleColor);

            Border cardBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#14141E")),
                BorderBrush = new SolidColorBrush(roleColor),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(8),
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };
            TextOptions.SetTextFormattingMode(cardBorder, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(cardBorder, TextRenderingMode.ClearType);
            RenderOptions.SetBitmapScalingMode(cardBorder, BitmapScalingMode.HighQuality);

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

        private string PromptPasswordDialog(string title, string message)
        {
            string enteredKey = "";
            Window dialog = new Window
            {
                Title = title,
                Width = 420,
                Height = 230,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent
            };

            Border border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D0D16")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20)
            };

            StackPanel panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 8) });
            panel.Children.Add(new TextBlock { Text = message, FontSize = 12, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA")), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 14) });

            PasswordBox passBox = new PasswordBox
            {
                Height = 36,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#141422")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28283E")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 6, 8, 6),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 16)
            };
            panel.Children.Add(passBox);

            StackPanel btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Button cancelBtn = new Button { Content = "Cancelar", Height = 34, Padding = new Thickness(14, 0, 14, 0), Margin = new Thickness(0, 0, 8, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#222234")), Foreground = Brushes.White, FontWeight = FontWeights.Bold, Cursor = System.Windows.Input.Cursors.Hand };
            SetButtonCornerRadius(cancelBtn, 8);
            cancelBtn.Click += (s, e) => dialog.Close();

            Button okBtn = new Button { Content = "🔐 Confirmar", Height = 34, Padding = new Thickness(16, 0, 16, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")), Foreground = Brushes.White, FontWeight = FontWeights.Bold, Cursor = System.Windows.Input.Cursors.Hand };
            SetButtonCornerRadius(okBtn, 8);
            okBtn.Click += (s, e) => { enteredKey = passBox.Password; dialog.Close(); };

            btnRow.Children.Add(cancelBtn);
            btnRow.Children.Add(okBtn);
            panel.Children.Add(btnRow);

            border.Child = panel;
            dialog.Content = border;
            dialog.ShowDialog();

            return enteredKey;
        }
    }
}

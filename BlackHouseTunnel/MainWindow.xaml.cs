using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BlackHouseTunnel.Models;
using BlackHouseTunnel.Services;
using BlackHouseTunnel.Views;

namespace BlackHouseTunnel
{
    public partial class MainWindow : Window
    {
        private WelcomeView? _welcomeView;
        private MainMenuView? _mainMenuView;
        private readonly DiscordApiService _apiService;

        public MainWindow()
        {
            InitializeComponent();
            _apiService = new DiscordApiService();

            // Set MaxHeight/MaxWidth to WorkArea to avoid covering the Windows Taskbar when maximized
            this.MaxHeight = SystemParameters.WorkArea.Height;
            this.MaxWidth = SystemParameters.WorkArea.Width;

            this.Loaded += MainWindow_Loaded;
            DiscordRpcService.Initialize();
            DiscordRpcService.SetPresenceInMenu();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var config = ConfigManager.CurrentConfig;
            if (!string.IsNullOrWhiteSpace(config.SavedAccessToken))
            {
                var user = await _apiService.GetUserProfileAndGuildMemberAsync(config.SavedAccessToken, config.GuildId, config.BotToken);
                if (user != null)
                {
                    if (user.IsMemberOfGuild)
                    {
                        ShowMainMenuView(user);
                        return;
                    }
                    else
                    {
                        ShowAccessDeniedView(user);
                        return;
                    }
                }
            }

            ShowWelcomeView();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
            }
            else
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Normal;
                    TxtMaximizeIcon.Text = "🗖";
                }
                this.DragMove();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_mainMenuView?.HasActiveHost == true)
            {
                var result = DarkMessageBox.Show("Hay un servidor Host en ejecución. Si cierras la aplicación se detendrá el túnel y se forzará el cierre de Roblox Studio.\n\n¿Estás seguro de que deseas salir?", "Confirmar Salida", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

                RobloxStudioService.StopAllStudioProcesses();
                UdpProxy.StopProxy();
                RbxmBridgeServer.Stop();
            }
            base.OnClosing(e);
        }

        private void ToggleMaximize()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                TxtMaximizeIcon.Text = "🗖";
            }
            else
            {
                // Respect WorkArea (Taskbar area) when maximizing
                this.MaxHeight = SystemParameters.WorkArea.Height;
                this.MaxWidth = SystemParameters.WorkArea.Width;
                this.WindowState = WindowState.Maximized;
                TxtMaximizeIcon.Text = "🗗";
            }
        }

        private void ShowWelcomeView()
        {
            _welcomeView = new WelcomeView();
            _welcomeView.OnLoginRequested += WelcomeView_OnLoginRequested;
            MainContainer.Children.Clear();
            MainContainer.Children.Add(_welcomeView);
        }

        private async void WelcomeView_OnLoginRequested(object? sender, EventArgs e)
        {
            var config = ConfigManager.CurrentConfig;
            var authService = new DiscordAuthService(config);

            string? accessToken = await authService.AuthenticateAsync();

            if (string.IsNullOrEmpty(accessToken))
            {
                _welcomeView?.SetLoadingState(false, "No se pudo completar el inicio de sesión.");
                return;
            }

            config.SavedAccessToken = accessToken;
            ConfigManager.SaveConfig(config);

            var user = await _apiService.GetUserProfileAndGuildMemberAsync(accessToken, config.GuildId, config.BotToken);

            if (user == null)
            {
                // Fallback for Offline Mode / No Internet Connection
                user = new DiscordUser
                {
                    Id = "000000000000000000",
                    Username = "offline_user",
                    GlobalName = "Modo Offline (Sin Internet)",
                    IsMemberOfGuild = true,
                    PrimaryRole = "Offline Mode",
                    PrimaryRoleColor = "#F59E0B"
                };
                DarkMessageBox.Show("🌐 Modo Offline Detectado:\n\nNo se detectó conexión a Internet activa o los servicios de Discord no responden. Se ha iniciado el programa en Modo Offline para permitir el uso local y túneles en red.", "Modo Offline", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (user.IsMemberOfGuild)
            {
                ShowMainMenuView(user);
            }
            else
            {
                ShowAccessDeniedView(user);
            }
        }

        private void ShowMainMenuView(DiscordUser user)
        {
            var mainMenuView = new MainMenuView(user);
            _mainMenuView = mainMenuView;
            mainMenuView.OnLogoutRequested += (s, e) => ShowWelcomeView();
            mainMenuView.OnReloadRequested += (s, e) => ShowMainMenuView(user);
            MainContainer.Children.Clear();
            MainContainer.Children.Add(mainMenuView);
        }

        private void ShowAccessDeniedView(DiscordUser user)
        {
            var accessDeniedView = new AccessDeniedView(user);
            accessDeniedView.OnRetryRequested += (s, e) => ShowWelcomeView();
            MainContainer.Children.Clear();
            MainContainer.Children.Add(accessDeniedView);
        }
    }
}

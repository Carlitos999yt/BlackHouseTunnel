using System;
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
        private readonly DiscordApiService _apiService;

        public MainWindow()
        {
            InitializeComponent();
            _apiService = new DiscordApiService();

            // Set MaxHeight/MaxWidth to WorkArea to avoid covering the Windows Taskbar when maximized
            this.MaxHeight = SystemParameters.WorkArea.Height;
            this.MaxWidth = SystemParameters.WorkArea.Width;

            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var config = ConfigManager.CurrentConfig;
            if (!string.IsNullOrWhiteSpace(config.SavedAccessToken))
            {
                var user = await _apiService.GetUserProfileAndGuildMemberAsync(config.SavedAccessToken, config.GuildId, config.BotToken);
                if (user != null && user.IsMemberOfGuild)
                {
                    ShowMainMenuView(user);
                    return;
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
                _welcomeView?.SetLoadingState(false, "Error obteniendo datos del perfil de Discord.");
                return;
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
            mainMenuView.OnLogoutRequested += (s, e) => ShowWelcomeView();
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

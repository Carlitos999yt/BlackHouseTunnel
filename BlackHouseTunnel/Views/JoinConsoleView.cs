using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackHouseTunnel.Services;

namespace BlackHouseTunnel.Views
{
    public class JoinConsoleView : UserControl
    {
        public event EventHandler? OnDisconnectRequested;

        private readonly LiveLogConsoleView _console;

        public JoinConsoleView(string studioPath, string dstHost, int dstPort, string username)
        {
            Grid mainGrid = new Grid { Margin = new Thickness(28) };
            StackPanel panel = new StackPanel { MaxWidth = 780, HorizontalAlignment = HorizontalAlignment.Left };

            TextBlock title = new TextBlock
            {
                Text = "⚡ Consola de Ejecución en Vivo — Cliente Conectado",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            };

            TextBlock sub = new TextBlock
            {
                Text = $"Conectado como '{username}' al túnel {dstHost}:{dstPort} vía Proxy Local 127.0.0.1:{UdpProxy.PROXY_PORT}",
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA")),
                Margin = new Thickness(0, 0, 0, 16)
            };

            panel.Children.Add(title);
            panel.Children.Add(sub);

            // Action Bar
            StackPanel btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };

            Button testBtn = CreateActionButton("⚡ Probar Conectividad", "#5865F2");
            testBtn.Click += (s, e) =>
            {
                Task.Run(() => ConnectivityTester.RunConnectivityTestAsync(dstHost, dstPort, (m, t) => _console?.AppendLog(m, t), isHostSide: false));
            };
            btnRow.Children.Add(testBtn);

            Button discBtn = CreateActionButton("⏹ Desconectar Túnel", "#ED4245");
            discBtn.Click += (s, e) =>
            {
                var result = DarkMessageBox.Show("¿Estás seguro de desconectarte del túnel activo?", "Desconectar", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    UdpProxy.StopProxy();
                    RobloxStudioService.ForceKillAllStudioProcesses();
                    OnDisconnectRequested?.Invoke(this, EventArgs.Empty);
                }
            };
            btnRow.Children.Add(discBtn);

            panel.Children.Add(btnRow);

            // Live Log Console
            _console = new LiveLogConsoleView(280);
            panel.Children.Add(_console);

            mainGrid.Children.Add(panel);
            this.Content = mainGrid;

            // Start Join Initialization
            Task.Run(async () =>
            {
                string parentGuid = Guid.NewGuid().ToString().ToUpper();
                string playGuid = Guid.NewGuid().ToString().ToUpper();

                _console.AppendLog($"Destino Túnel: {dstHost}:{dstPort}", "info");
                _console.AppendLog($"Proxy Local UDP: 127.0.0.1:{UdpProxy.ActiveProxyPort}");
                _console.AppendLog("Iniciando motor de Proxy UDP y estableciendo túnel...");

                bool ok = UdpProxy.StartProxy(dstHost, dstPort);
                if (!ok)
                {
                    _console.AppendLog($"Error al enlazar el puerto UDP {UdpProxy.ActiveProxyPort}. ¿Hay otra sesión activa?", "err");
                    return;
                }

                _console.AppendLog($"Proxy UDP activo en 127.0.0.1:{UdpProxy.ActiveProxyPort}", "ok");
                _console.AppendLog("⏳ Pre-conectando al túnel remoto y estableciendo ruta UDP...", "warn");

                int warmed = UdpProxy.WarmTunnel(dstHost, dstPort, UdpProxy.ActiveProxyPort, nickname: username);
                if (warmed > 0)
                {
                    _console.AppendLog($"✓ Ruta de túnel establecida y calentada ({warmed} ráfagas enviadas)", "ok");
                }

                // Wait 1.5 seconds for Playit UDP tunnel stabilization before launching Studio
                _console.AppendLog("⌛ Esperando estabilización del túnel (1.5s)...", "warn");
                await Task.Delay(1500);

                _console.AppendLog($"Parent GUID: {parentGuid}", "dim");
                _console.AppendLog($"Play   GUID: {playGuid}", "dim");
                _console.AppendLog("✓ Túnel 100% activo. Ejecutando cliente Roblox Studio...", "ok");

                try
                {
                    RobloxStudioService.LaunchClient(studioPath, "127.0.0.1", UdpProxy.ActiveProxyPort.ToString(), parentGuid, playGuid, "StudioPlayer_Proxy", username);
                    _console.AppendLog("● CONECTADO EXITOSAMENTE — Cliente en ejecución", "ok");
                }
                catch (Exception ex)
                {
                    _console.AppendLog($"Error al lanzar Roblox Studio: {ex.Message}", "err");
                    UdpProxy.StopProxy();
                    RobloxStudioService.ForceKillAllStudioProcesses();
                }
            });
        }

        private Button CreateActionButton(string title, string hexBg)
        {
            Button btn = new Button
            {
                Content = title,
                Height = 38,
                Padding = new Thickness(16, 0, 16, 0),
                Margin = new Thickness(0, 0, 10, 0),
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

using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackHouseTunnel.Services;

namespace BlackHouseTunnel.Views
{
    public class HostConsoleView : UserControl
    {
        public event EventHandler? OnStopHostRequested;

        private readonly LiveLogConsoleView _console;

        public HostConsoleView(string studioPath, string uid, string port, string addr, string mapPath, string username, string? discordMsgId = null, bool publishRequested = false)
        {
            string parentGuid = Guid.NewGuid().ToString().ToUpper();
            string playGuid = Guid.NewGuid().ToString().ToUpper();

            Grid mainGrid = new Grid { Margin = new Thickness(28) };
            StackPanel panel = new StackPanel { MaxWidth = 780, HorizontalAlignment = HorizontalAlignment.Left };

            TextBlock title = new TextBlock
            {
                Text = "🖥️ Consola de Ejecución en Vivo — Servidor Host",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            };

            TextBlock sub = new TextBlock
            {
                Text = $"Servidor Host activo como '{username}' | Puerto UDP: {port} | Dirección: {addr}",
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA")),
                Margin = new Thickness(0, 0, 0, 16)
            };

            panel.Children.Add(title);
            panel.Children.Add(sub);

            // Action Bar
            StackPanel btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };

            Button joinLocalBtn = CreateActionButton("🎮 Unirse al Host Local", "#10B981");
            joinLocalBtn.IsEnabled = false;
            joinLocalBtn.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(studioPath) && File.Exists(studioPath))
                {
                    RobloxStudioService.LaunchClient(studioPath, port, uid, parentGuid, playGuid, username);
                }
            };
            btnRow.Children.Add(joinLocalBtn);

            Button testBtn = CreateActionButton("⚡ Probar Conectividad", "#5865F2");
            testBtn.Click += (s, e) =>
            {
                string h = addr.Contains(':') ? addr.Split(':')[0] : addr;
                int tp = addr.Contains(':') && int.TryParse(addr.Split(':')[1], out int pVal) ? pVal : int.Parse(port);
                Task.Run(() => ConnectivityTester.RunConnectivityTestAsync(h, tp, (m, t) => _console?.AppendLog(m, t), isHostSide: true, localServerPort: int.Parse(port)));
            };
            btnRow.Children.Add(testBtn);

            Button stopBtn = CreateActionButton("⏹ Detener Host", "#ED4245");
            stopBtn.Click += (s, e) =>
            {
                var result = DarkMessageBox.Show("¿Estás seguro de detener el Servidor Host activo?\n\n(Se cerrará el túnel y se forzará el cierre de Roblox Studio automáticamente).", "Detener Host", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    RobloxStudioService.ForceKillAllStudioProcesses();
                    OnStopHostRequested?.Invoke(this, EventArgs.Empty);
                }
            };
            btnRow.Children.Add(stopBtn);

            panel.Children.Add(btnRow);

            // Live Log Console
            _console = new LiveLogConsoleView(280);
            panel.Children.Add(_console);

            mainGrid.Children.Add(panel);
            this.Content = mainGrid;

            // Start Host Initialization
            Task.Run(async () =>
            {
                _console.AppendLog($"Parent GUID: {parentGuid}", "dim");
                _console.AppendLog($"Play   GUID: {playGuid}", "dim");
                _console.AppendLog($"Puerto UDP : {port}", "info");
                _console.AppendLog($"Túnel Remoto: {addr}", "info");

                if (!string.IsNullOrEmpty(discordMsgId))
                {
                    _console.AppendLog($"📢 Anuncio de Host publicado con ÉXITO en Discord (ID Mensaje: {discordMsgId})", "ok");
                }
                else if (publishRequested)
                {
                    _console.AppendLog("⚠️ ADVERTENCIA: No se pudo enviar el anuncio a Discord. Revisa que el Bot tenga permisos de 'Enviar Mensajes' e 'Insertar Enlaces' en el canal seleccionado.", "err");
                }

                if (!string.IsNullOrEmpty(mapPath) && File.Exists(mapPath))
                {
                    _console.AppendLog($"Inyectando mapa: {Path.GetFileName(mapPath)}...", "warn");
                    var (success, message) = ScriptInjector.InjectScriptIntoMap(mapPath, $"print('BlackHouse Server session started for {username}')");
                    if (success)
                    {
                        _console.AppendLog(message, "ok");
                    }
                }

                _console.AppendLog("Iniciando proceso de Roblox Studio Server...");
                try
                {
                    RobloxStudioService.LaunchServer(studioPath, port, uid, parentGuid, playGuid, username);
                    _console.AppendLog("¡Proceso de Servidor iniciado! Esperando 5 segundos...", "ok");
                    await Task.Delay(5000);
                    _console.AppendLog("● SERVIDOR EN VIVO Y LISTO PARA CONEXIONES", "ok");

                    Dispatcher.Invoke(() =>
                    {
                        joinLocalBtn.IsEnabled = true;
                        try { Clipboard.SetText(addr); } catch { }
                    });
                }
                catch (Exception ex)
                {
                    _console.AppendLog($"ERROR al iniciar el servidor: {ex.Message}", "err");
                }
            });
        }

        private Button CreateActionButton(string title, string hexBg)
        {
            Button btn = new Button
            {
                Content = title,
                Height = 38,
                Padding = new Thickness(14, 0, 14, 0),
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
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(12, 4, 12, 4));
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

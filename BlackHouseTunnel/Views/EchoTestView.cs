using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackHouseTunnel.Services;

namespace BlackHouseTunnel.Views
{
    public class EchoTestView : UserControl
    {
        private readonly EchoServer _echoServer = new();
        private readonly LiveLogConsoleView _console;

        public EchoTestView()
        {
            Grid mainGrid = new Grid { Margin = new Thickness(28) };

            StackPanel panel = new StackPanel { MaxWidth = 700, HorizontalAlignment = HorizontalAlignment.Left };

            TextBlock title = new TextBlock
            {
                Text = LocalizationService.Get("echo_title"),
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = ThemeManager.TextPrimaryBrush,
                Margin = new Thickness(0, 0, 0, 4)
            };

            TextBlock sub = new TextBlock
            {
                Text = LocalizationService.Get("echo_sub"),
                FontSize = 13,
                Foreground = ThemeManager.TextMutedBrush,
                Margin = new Thickness(0, 0, 0, 16)
            };

            panel.Children.Add(title);
            panel.Children.Add(sub);

            // Controls Card
            Border card = new Border
            {
                Background = ThemeManager.CardBgBrush,
                BorderBrush = ThemeManager.CardBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 16)
            };

            StackPanel cardStack = new StackPanel();

            Grid fieldsGrid = new Grid();
            fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel portStack = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            portStack.Children.Add(CreateLabel(LocalizationService.Get("lbl_port")));
            TextBox portBox = CreateStyledTextBox(ConfigManager.CurrentConfig.SavedUdpPort.ToString());
            portStack.Children.Add(portBox);
            Grid.SetColumn(portStack, 0);
            fieldsGrid.Children.Add(portStack);

            StackPanel addrStack = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            addrStack.Children.Add(CreateLabel(LocalizationService.Get("lbl_addr")));
            TextBox addrBox = CreateStyledTextBox(ConfigManager.CurrentConfig.SavedRemoteHostAddress);
            addrStack.Children.Add(addrBox);
            Grid.SetColumn(addrStack, 1);
            fieldsGrid.Children.Add(addrStack);

            cardStack.Children.Add(fieldsGrid);

            // Action Buttons
            StackPanel btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };

            Button echoServerBtn = new Button
            {
                Content = "▶ Iniciar Servidor Eco",
                Height = 38,
                Padding = new Thickness(16, 0, 16, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 12, 0)
            };
            SetButtonTemplate(echoServerBtn);

            echoServerBtn.Click += (s, e) =>
            {
                if (_echoServer.IsRunning)
                {
                    _echoServer.Stop();
                    echoServerBtn.Content = "▶ Iniciar Servidor Eco";
                    echoServerBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                    _console?.AppendLog("Servidor Eco detenido.", "dim");
                }
                else
                {
                    if (int.TryParse(portBox.Text.Trim(), out int port))
                    {
                        if (_echoServer.Start(port, (msg, tag) => _console?.AppendLog(msg, tag)))
                        {
                            echoServerBtn.Content = "⏹ Detener Servidor Eco";
                            echoServerBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ED4245"));
                            _console?.AppendLog($"✓ Servidor Eco ACTIVO en el puerto UDP {port}.", "ok");
                            _console?.AppendLog("Esperando paquetes de prueba de clientes en línea...", "warn");
                        }
                    }
                }
            };
            btnRow.Children.Add(echoServerBtn);

            Button echoClientBtn = new Button
            {
                Content = LocalizationService.Get("btn_run_echo"),
                Height = 38,
                Padding = new Thickness(16, 0, 16, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            SetButtonTemplate(echoClientBtn);

            echoClientBtn.Click += (s, e) =>
            {
                string rawAddr = addrBox.Text.Trim();
                if (string.IsNullOrEmpty(rawAddr) || !rawAddr.Contains(":"))
                {
                    _console?.AppendLog("Por favor ingresa una dirección de túnel válida (ej: host:puerto).", "err");
                    return;
                }

                var parts = rawAddr.Split(':');
                if (int.TryParse(parts[1], out int targetPort))
                {
                    Task.Run(() => EchoClient.RunEchoTestAsync(parts[0], targetPort, (msg, tag) => _console?.AppendLog(msg, tag)));
                }
            };
            btnRow.Children.Add(echoClientBtn);

            cardStack.Children.Add(btnRow);
            card.Child = cardStack;
            panel.Children.Add(card);

            // Log Console
            _console = new LiveLogConsoleView(240);
            panel.Children.Add(_console);

            _console.AppendLog("--- Herramienta de Diagnóstico y Latencia UDP BlackHouseTunnel ---", "info");
            _console.AppendLog("Modo Host: Pulsa 'Iniciar Servidor Eco' para responder a paquetes de clientes.", "dim");
            _console.AppendLog("Modo Cliente: Pulsa 'Ejecutar Prueba de Latencia' para medir RTT y % de pérdida de paquetes.", "dim");

            mainGrid.Children.Add(panel);
            this.Content = mainGrid;
        }

        private TextBlock CreateLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = ThemeManager.TextMutedBrush,
                Margin = new Thickness(0, 4, 0, 4)
            };
        }

        private TextBox CreateStyledTextBox(string defaultText)
        {
            return new TextBox
            {
                Text = defaultText,
                Height = 36,
                Background = ThemeManager.InputBgBrush,
                Foreground = ThemeManager.TextPrimaryBrush,
                BorderBrush = ThemeManager.InputBorderBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 6, 8, 6),
                FontSize = 13
            };
        }

        private void SetButtonTemplate(Button btn)
        {
            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(16, 6, 16, 6));
            FrameworkElementFactory presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);
            template.VisualTree = borderFactory;
            btn.Template = template;
        }
    }
}

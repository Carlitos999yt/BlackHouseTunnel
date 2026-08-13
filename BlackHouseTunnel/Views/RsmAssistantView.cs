using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackHouseTunnel.Services;

namespace BlackHouseTunnel.Views
{
    public class RsmAssistantView : UserControl
    {
        private readonly LiveLogConsoleView _console;

        public RsmAssistantView()
        {
            Grid mainGrid = new Grid { Margin = new Thickness(28) };
            StackPanel panel = new StackPanel { MaxWidth = 750, HorizontalAlignment = HorizontalAlignment.Left };

            TextBlock title = new TextBlock
            {
                Text = "🛠️ Asistente e Instalador de RSM Mod Manager",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = ThemeManager.TextPrimaryBrush,
                Margin = new Thickness(0, 0, 0, 4)
            };

            TextBlock sub = new TextBlock
            {
                Text = "Gestiona, instala, repara desde GitHub o limpia la instalación de Roblox Studio Mod Manager.",
                FontSize = 13,
                Foreground = ThemeManager.TextMutedBrush,
                Margin = new Thickness(0, 0, 0, 16)
            };

            panel.Children.Add(title);
            panel.Children.Add(sub);

            // Status Card
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string rsmExePath = Path.Combine(localAppData, "Roblox Studio", "RobloxStudioBeta.exe");
            string rsmFolder = Path.Combine(localAppData, "Roblox Studio");
            string rsmManagerFolder = Path.Combine(localAppData, "Roblox Studio Mod Manager");
            bool isRsmInstalled = File.Exists(rsmExePath);

            Border statusCard = new Border
            {
                Background = ThemeManager.CardBgBrush,
                BorderBrush = ThemeManager.CardBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 16)
            };

            StackPanel statusStack = new StackPanel();
            TextBlock statusHeader = new TextBlock
            {
                Text = isRsmInstalled ? "✓ RSM Mod Manager Instalado y Listo" : "⚠ RSM Mod Manager No Detectado",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isRsmInstalled ? "#10B981" : "#F59E0B"))
            };
            statusStack.Children.Add(statusHeader);

            if (isRsmInstalled)
            {
                statusStack.Children.Add(new TextBlock
                {
                    Text = $"Ruta: {rsmExePath}",
                    FontSize = 11,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8E9297")),
                    Margin = new Thickness(0, 4, 0, 0)
                });
            }

            statusCard.Child = statusStack;
            panel.Children.Add(statusCard);

            // Action Grid
            System.Windows.Controls.Primitives.UniformGrid actionsGrid = new System.Windows.Controls.Primitives.UniformGrid
            {
                Columns = 2,
                Rows = 2,
                Margin = new Thickness(0, 0, 0, 16)
            };

            Button installBtn = CreateActionButton("📥 Instalar / Iniciar RSM", "#5865F2");
            installBtn.Click += (s, e) =>
            {
                Task.Run(async () =>
                {
                    _console?.AppendLog("Iniciando instalador oficial de RSM Mod Manager...", "info");
                    bool ok = await RsmInstallerService.LaunchOfficialRsmBootstrapperAsync((msg, tag) => _console?.AppendLog(msg, tag));
                    if (ok)
                    {
                        _console?.AppendLog("✓ RSM Mod Manager lanzado exitosamente.", "ok");
                    }
                    else
                    {
                        _console?.AppendLog("⚠ Error o cancelación al lanzar RSM.", "err");
                    }
                });
            };
            actionsGrid.Children.Add(installBtn);

            Button repairBtn = CreateActionButton("🛠️ Reparar desde GitHub", "#F59E0B");
            repairBtn.Click += (s, e) =>
            {
                Task.Run(async () =>
                {
                    _console?.AppendLog("Reparando instalación de RSM desde repositorio GitHub...", "warn");
                    bool ok = await RsmInstallerService.RepairFromGitHubRepoAsync((msg, tag) => _console?.AppendLog(msg, tag), pct => { });
                    if (ok)
                    {
                        _console?.AppendLog("✓ Reparación de RSM desde GitHub completada exitosamente.", "ok");
                    }
                    else
                    {
                        _console?.AppendLog("⚠ Fallo en la reparación.", "err");
                    }
                });
            };
            actionsGrid.Children.Add(repairBtn);

            Button folderBtn = CreateActionButton("📁 Abrir Carpeta RSM", "#3B82F6");
            folderBtn.Click += (s, e) =>
            {
                try
                {
                    string target = Directory.Exists(rsmFolder) ? rsmFolder : localAppData;
                    Process.Start("explorer.exe", target);
                }
                catch (Exception ex)
                {
                    _console?.AppendLog($"Error al abrir carpeta: {ex.Message}", "err");
                }
            };
            actionsGrid.Children.Add(folderBtn);

            Button deleteBtn = CreateActionButton("🗑️ Desinstalar / Limpiar RSM", "#ED4245");
            deleteBtn.Click += (s, e) =>
            {
                var result = DarkMessageBox.Show("¿Estás seguro de eliminar RSM y restaurar las entradas de registro de Windows por completo?", "Confirmar Desinstalación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        RsmInstallerService.CleanRsmRegistryAndProtocols();
                        _console?.AppendLog("✓ RSM eliminado por completo y registro de Windows restaurado.", "warn");
                    }
                    catch (Exception ex)
                    {
                        _console?.AppendLog($"Error al limpiar registro: {ex.Message}", "err");
                    }
                }
            };
            actionsGrid.Children.Add(deleteBtn);

            panel.Children.Add(actionsGrid);

            // Log Console
            _console = new LiveLogConsoleView(220);
            panel.Children.Add(_console);

            _console.AppendLog("--- Asistente de Gestión de Roblox Studio Mod Manager ---", "info");

            mainGrid.Children.Add(panel);
            this.Content = mainGrid;
        }

        private Button CreateActionButton(string title, string hexBg)
        {
            Button btn = new Button
            {
                Content = title,
                Height = 44,
                Margin = new Thickness(4),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexBg)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
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
            btn.Template = template;

            return btn;
        }
    }
}

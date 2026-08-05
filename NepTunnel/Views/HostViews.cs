using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using NepTunnel.Services;

namespace NepTunnel.Views
{
    public static class HostViews
    {
        public static FrameworkElement CreateConfigView(
            NepConfig cfg,
            string studioPath,
            Action onBackClick,
            Action onTutorialClick,
            Action<string, string, string, string, string> onLaunchServerClick,
            Func<string, object> findResource)
        {
            var grid = new Grid { Background = (SolidColorBrush)findResource("BgBrush") };
            var stack = new StackPanel { Margin = new Thickness(24, 12, 24, 12) };

            stack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("host_title"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)findResource("TextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            stack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("host_sub"),
                FontSize = 13,
                Foreground = (SolidColorBrush)findResource("MuteBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 12)
            });

            var card = new Border
            {
                Background = (SolidColorBrush)findResource("CardBrush"),
                BorderBrush = (SolidColorBrush)findResource("BordBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16)
            };

            var cardGrid = new Grid();
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // User ID
            var uidLbl = new TextBlock { Text = LocalizationService.Get("lbl_uid"), FontSize = 14, Foreground = (SolidColorBrush)findResource("MuteBrush"), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(uidLbl, 0); Grid.SetColumn(uidLbl, 0); cardGrid.Children.Add(uidLbl);
            var uidTb = new TextBox { Text = cfg.Uid, Style = (Style)findResource("NepTextBoxStyle"), Margin = new Thickness(0, 3, 0, 3) };
            Grid.SetRow(uidTb, 0); Grid.SetColumn(uidTb, 1); cardGrid.Children.Add(uidTb);

            // My Username / Nickname
            var userLbl = new TextBlock { Text = LocalizationService.Get("lbl_username"), FontSize = 14, Foreground = (SolidColorBrush)findResource("MuteBrush"), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(userLbl, 1); Grid.SetColumn(userLbl, 0); cardGrid.Children.Add(userLbl);
            var userTb = new TextBox { Text = !string.IsNullOrEmpty(cfg.Username) ? cfg.Username : "Carlitos", Style = (Style)findResource("NepTextBoxStyle"), Margin = new Thickness(0, 3, 0, 3) };
            Grid.SetRow(userTb, 1); Grid.SetColumn(userTb, 1); cardGrid.Children.Add(userTb);

            // Server Local Port
            var portLbl = new TextBlock { Text = LocalizationService.Get("lbl_server_port"), FontSize = 14, Foreground = (SolidColorBrush)findResource("MuteBrush"), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(portLbl, 2); Grid.SetColumn(portLbl, 0); cardGrid.Children.Add(portLbl);
            var portTb = new TextBox { Text = cfg.Port, Style = (Style)findResource("NepTextBoxStyle"), Margin = new Thickness(0, 3, 0, 3) };
            Grid.SetRow(portTb, 2); Grid.SetColumn(portTb, 1); cardGrid.Children.Add(portTb);

            // Tunnel Address
            var addrLbl = new TextBlock { Text = LocalizationService.Get("lbl_tunnel_addr"), FontSize = 14, Foreground = (SolidColorBrush)findResource("MuteBrush"), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(addrLbl, 3); Grid.SetColumn(addrLbl, 0); cardGrid.Children.Add(addrLbl);
            var addrTb = new TextBox { Text = cfg.Addr, Style = (Style)findResource("NepTextBoxStyle"), Margin = new Thickness(0, 3, 0, 3) };
            Grid.SetRow(addrTb, 3); Grid.SetColumn(addrTb, 1); cardGrid.Children.Add(addrTb);

            // Map File (Optional)
            var mapLbl = new TextBlock { Text = LocalizationService.Get("lbl_map_file"), FontSize = 14, Foreground = (SolidColorBrush)findResource("MuteBrush"), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(mapLbl, 4); Grid.SetColumn(mapLbl, 0); cardGrid.Children.Add(mapLbl);

            var mapStack = new Grid();
            mapStack.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mapStack.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var mapTb = new TextBox { Text = cfg.Map, Style = (Style)findResource("NepTextBoxStyle"), Margin = new Thickness(0, 3, 6, 3) };
            Grid.SetColumn(mapTb, 0); mapStack.Children.Add(mapTb);

            var mapBrowseBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("folder", LocalizationService.Get("browse"), 14),
                Background = (SolidColorBrush)findResource("Card2Brush"),
                Style = (Style)findResource("NepButtonStyle"),
                Padding = new Thickness(10, 4, 10, 4)
            };
            mapBrowseBtn.Click += (s, e) =>
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Select Roblox Map",
                    Filter = "Roblox Place (*.rbxl;*.rbxlx)|*.rbxl;*.rbxlx|All files (*.*)|*.*"
                };
                if (dlg.ShowDialog() == true)
                {
                    mapTb.Text = dlg.FileName;
                }
            };
            Grid.SetColumn(mapBrowseBtn, 1); mapStack.Children.Add(mapBrowseBtn);
            Grid.SetRow(mapStack, 4); Grid.SetColumn(mapStack, 1); cardGrid.Children.Add(mapStack);

            card.Child = cardGrid;
            stack.Children.Add(card);

            // Action Buttons Bar at Bottom
            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 16, 0, 0)
            };

            var backBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("back", LocalizationService.Get("back"), 14),
                Background = (SolidColorBrush)findResource("CardBrush"),
                Style = (Style)findResource("NepButtonStyle"),
                Margin = new Thickness(6, 0, 6, 0)
            };
            backBtn.Click += (s, e) =>
            {
                cfg.Uid = uidTb.Text.Trim();
                cfg.Port = portTb.Text.Trim();
                cfg.Addr = addrTb.Text.Trim();
                cfg.Map = mapTb.Text.Trim();
                cfg.Studio = studioPath;
                ConfigManager.SaveConfig(cfg);
                onBackClick();
            };
            btnRow.Children.Add(backBtn);

            var tutBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("test", LocalizationService.Get("btn_tutorial"), 14),
                Background = (SolidColorBrush)findResource("Card2Brush"),
                Style = (Style)findResource("NepButtonStyle"),
                Margin = new Thickness(6, 0, 6, 0)
            };
            tutBtn.Click += (s, e) => onTutorialClick();
            btnRow.Children.Add(tutBtn);

            var importScriptsBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("file-code", "Importar / Actualizar Scripts", 14),
                Background = (SolidColorBrush)findResource("Card2Brush"),
                Style = (Style)findResource("NepButtonStyle"),
                Margin = new Thickness(6, 0, 6, 0)
            };
            importScriptsBtn.Click += (s, e) =>
            {
                try
                {
                    RbxmBridgeServer.ForceScriptImport = true;
                    RbxmBridgeServer.ScriptsImported = true;
                    PluginInstaller.EnsurePluginInstalled(out string _);
                    MessageBox.Show("✓ Scripts importados/actualizados correctamente en Roblox Studio.\n\nLos nuevos scripts oficiales del tabulador han sido insertados.", "✓ Importación de Scripts", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se pudieron importar los scripts: " + ex.Message, "✗ Error de Importación", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            btnRow.Children.Add(importScriptsBtn);

            var launchBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("play", LocalizationService.Get("btn_launch_server"), 16),
                Background = (SolidColorBrush)findResource("AccBrush"),
                Style = (Style)findResource("NepButtonStyle"),
                Margin = new Thickness(6, 0, 6, 0)
            };
            launchBtn.Click += (s, e) =>
            {
                string uid = uidTb.Text.Trim();
                string port = portTb.Text.Trim();
                string addr = addrTb.Text.Trim();
                string mapPath = mapTb.Text.Trim();

                if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(port) || string.IsNullOrEmpty(addr))
                {
                    MessageBox.Show("All fields are required.", "Missing Fields", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!int.TryParse(port, out _))
                {
                    MessageBox.Show("Port must be a number.", "Invalid Port", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrEmpty(studioPath))
                {
                    string osName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
                                   RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" : "Linux";
                    MessageBox.Show($"Roblox Studio was not found on {osName}.\nPlease ensure Roblox Studio is installed.", "Studio Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string username = userTb.Text.Trim();
                if (string.IsNullOrWhiteSpace(username)) username = "Carlitos";

                cfg.Uid = uid;
                cfg.Username = username;
                cfg.Port = port;
                cfg.Addr = addr;
                cfg.Map = mapPath;
                cfg.Studio = studioPath;
                ConfigManager.SaveConfig(cfg);

                onLaunchServerClick(uid, port, addr, mapPath, username);
            };
            btnRow.Children.Add(launchBtn);
            stack.Children.Add(btnRow);

            grid.Children.Add(stack);
            return grid;
        }
    }
}

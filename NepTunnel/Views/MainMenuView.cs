using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using NepTunnel.Services;

namespace NepTunnel.Views
{
    public static class MainMenuView
    {
        public static FrameworkElement Create(
            NepConfig cfg,
            string studioPath,
            Action onHostClick,
            Action onJoinClick,
            Action onEchoClick,
            Action onRbxmClick,
            Action onRsmClick,
            Action<string> onStudioChanged,
            Action<TextBlock> onShowStudioSelector,
            Action<string, SolidColorBrush> setStatus,
            Func<string, object> findResource)
        {
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(0)
            };

            var stack = new StackPanel { Margin = new Thickness(24, 8, 24, 16) };

            var card = new Border
            {
                Background = (SolidColorBrush)findResource("CardBrush"),
                BorderBrush = (SolidColorBrush)findResource("BordBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var cardStack = new StackPanel();

            cardStack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("main_title"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)findResource("TextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            cardStack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("main_subtitle"),
                FontSize = 13,
                Foreground = (SolidColorBrush)findResource("MuteBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 16)
            });

            // Row 1 Action Buttons
            var row1 = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var hostBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("host", LocalizationService.Get("btn_host"), 18),
                Background = (SolidColorBrush)findResource("AccBrush"),
                Style = (Style)findResource("NepButtonStyle"),
                Margin = new Thickness(8, 0, 8, 0)
            };
            hostBtn.Click += (s, e) => onHostClick();
            row1.Children.Add(hostBtn);

            var joinBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("join", LocalizationService.Get("btn_join"), 18),
                Background = (SolidColorBrush)findResource("BlueBrush"),
                Style = (Style)findResource("NepButtonStyle"),
                Margin = new Thickness(8, 0, 8, 0)
            };
            joinBtn.Click += (s, e) => onJoinClick();
            row1.Children.Add(joinBtn);

            cardStack.Children.Add(row1);

            // Row 2 Action Buttons
            var row2 = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };

            var echoBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("echo", LocalizationService.Get("btn_echo"), 18),
                Background = (SolidColorBrush)findResource("TealBrush"),
                Style = (Style)findResource("NepButtonStyle"),
                Margin = new Thickness(8, 0, 8, 0)
            };
            echoBtn.Click += (s, e) => onEchoClick();
            row2.Children.Add(echoBtn);

            var rbxmBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("map", LocalizationService.Get("btn_rbxm"), 18),
                Background = new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED)),
                Style = (Style)findResource("NepButtonStyle"),
                Margin = new Thickness(8, 0, 8, 0)
            };
            rbxmBtn.Click += (s, e) => onRbxmClick();
            row2.Children.Add(rbxmBtn);

            var rsmBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("folder", LocalizationService.Get("btn_rsm_assistant"), 18),
                Background = new SolidColorBrush(Color.FromRgb(0xD9, 0x46, 0xEF)),
                Style = (Style)findResource("NepButtonStyle"),
                Margin = new Thickness(8, 0, 8, 0)
            };
            rsmBtn.Click += (s, e) => onRsmClick();
            row2.Children.Add(rsmBtn);

            cardStack.Children.Add(row2);

            card.Child = cardStack;
            stack.Children.Add(card);

            // Divider Line
            var divider = new Border
            {
                BorderBrush = (SolidColorBrush)findResource("BordBrush"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Margin = new Thickness(0, 4, 0, 16)
            };
            stack.Children.Add(divider);

            // Info Section
            var infoStack = new StackPanel { Margin = new Thickness(4, 0, 4, 0) };

            // Studio Path Row
            var studioRow = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            studioRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
            studioRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            studioRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var studioKey = new TextBlock { Text = LocalizationService.Get("lbl_studio"), FontSize = 15, FontWeight = FontWeights.Bold, Foreground = (SolidColorBrush)findResource("MuteBrush"), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(studioKey, 0); studioRow.Children.Add(studioKey);

            var studioLbl = new TextBlock
            {
                Text = !string.IsNullOrEmpty(studioPath) ? studioPath : "Not found",
                FontSize = 14,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = !string.IsNullOrEmpty(studioPath) ? (SolidColorBrush)findResource("GlowBrush") : (SolidColorBrush)findResource("ErrBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            Grid.SetColumn(studioLbl, 1); studioRow.Children.Add(studioLbl);

            var actionBtnsStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var browseBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("folder", LocalizationService.Get("browse"), 14),
                Background = (SolidColorBrush)findResource("Card2Brush"),
                Style = (Style)findResource("NepButtonStyle"),
                Padding = new Thickness(10, 4, 10, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            browseBtn.Click += (s, e) =>
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select RobloxStudioBeta.exe",
                    Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*"
                };
                if (dialog.ShowDialog() == true)
                {
                    onStudioChanged(dialog.FileName);
                    studioLbl.Text = dialog.FileName;
                    studioLbl.Foreground = (SolidColorBrush)findResource("GlowBrush");
                    setStatus($"Studio set  ·  {dialog.FileName}", (SolidColorBrush)findResource("OkBrush"));
                }
            };
            actionBtnsStack.Children.Add(browseBtn);

            var dotsBtn = new Button
            {
                Content = "•••",
                Background = (SolidColorBrush)findResource("Card2Brush"),
                Style = (Style)findResource("NepButtonStyle"),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(6, 0, 0, 0),
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Instalaciones de Roblox Studio Detectadas"
            };
            dotsBtn.Click += (s, e) => onShowStudioSelector(studioLbl);
            actionBtnsStack.Children.Add(dotsBtn);

            Grid.SetColumn(actionBtnsStack, 2); studioRow.Children.Add(actionBtnsStack);
            infoStack.Children.Add(studioRow);

            // Detail Rows
            string osName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
                           RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" : "Linux";

            var details = new[]
            {
                (LocalizationService.Get("lbl_username"), !string.IsNullOrEmpty(cfg.Username) ? cfg.Username : "Carlitos"),
                (LocalizationService.Get("lbl_tunnel_addr"), !string.IsNullOrEmpty(cfg.HostAddr) ? cfg.HostAddr : (cfg.JoinAddr ?? "")),
                (LocalizationService.Get("lbl_server_port"), cfg.Port),
                (LocalizationService.Get("lbl_uid"), cfg.Uid),
                (LocalizationService.Get("lbl_proxy_port"), UdpProxy.PROXY_PORT.ToString()),
                (LocalizationService.Get("lbl_platform"), osName)
            };

            foreach (var (lbl, val) in details)
            {
                var r = new Grid { Margin = new Thickness(0, 4, 0, 4) };
                r.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
                r.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var k = new TextBlock { Text = lbl, FontSize = 15, FontWeight = FontWeights.Bold, Foreground = (SolidColorBrush)findResource("MuteBrush"), VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(k, 0); r.Children.Add(k);

                var v = new TextBlock { Text = val, FontSize = 14, Foreground = (SolidColorBrush)findResource("GlowBrush"), VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(v, 1); r.Children.Add(v);

                infoStack.Children.Add(r);
            }

            // Studio Bridge Status Row
            var bridgeRow = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            bridgeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
            bridgeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var bridgeKey = new TextBlock { Text = LocalizationService.Get("lbl_bridge"), FontSize = 15, FontWeight = FontWeights.Bold, Foreground = (SolidColorBrush)findResource("MuteBrush"), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(bridgeKey, 0); bridgeRow.Children.Add(bridgeKey);

            var bridgeVal = new TextBlock
            {
                Text = RbxmBridgeServer.IsRunning ? $"● port {RbxmBridgeServer.BRIDGE_PORT}" : "✗ failed to start",
                FontSize = 14,
                Foreground = RbxmBridgeServer.IsRunning ? (SolidColorBrush)findResource("OkBrush") : (SolidColorBrush)findResource("ErrBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(bridgeVal, 1); bridgeRow.Children.Add(bridgeVal);
            infoStack.Children.Add(bridgeRow);

            stack.Children.Add(infoStack);
            scroll.Content = stack;
            return scroll;
        }
    }
}

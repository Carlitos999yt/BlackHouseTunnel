using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NepTunnel.Services;

namespace NepTunnel.Views
{
    public static class JoinViews
    {
        public static FrameworkElement CreateConfigView(
            NepConfig cfg,
            Action onBackClick,
            Action<string, string> onConnectClick,
            Func<string, object> findResource)
        {
            var grid = new Grid { Background = (SolidColorBrush)findResource("BgBrush") };
            var stack = new StackPanel { Margin = new Thickness(24, 12, 24, 12) };

            stack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("join_title"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)findResource("TextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            stack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("join_sub"),
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

            var cardStack = new StackPanel();

            // My Username / Nick Field
            cardStack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("lbl_username"),
                FontSize = 14,
                Foreground = (SolidColorBrush)findResource("MuteBrush"),
                Margin = new Thickness(0, 0, 0, 4)
            });

            var userTb = new TextBox { Text = !string.IsNullOrEmpty(cfg.Username) ? cfg.Username : "Carlitos", Style = (Style)findResource("NepTextBoxStyle"), Margin = new Thickness(0, 0, 0, 8) };
            cardStack.Children.Add(userTb);

            cardStack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("lbl_tunnel_input"),
                FontSize = 14,
                Foreground = (SolidColorBrush)findResource("MuteBrush"),
                Margin = new Thickness(0, 0, 0, 4)
            });

            var initialJoinAddr = !string.IsNullOrEmpty(cfg.JoinAddr) ? cfg.JoinAddr : cfg.HostAddr;
            var addrTb = new TextBox { Text = initialJoinAddr, Style = (Style)findResource("NepTextBoxStyle"), Margin = new Thickness(0, 0, 0, 6) };
            cardStack.Children.Add(addrTb);

            cardStack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("lbl_proxy_hint"),
                FontSize = 12,
                Foreground = (SolidColorBrush)findResource("MuteBrush")
            });

            var errLbl = new TextBlock
            {
                Text = "",
                FontSize = 12,
                Foreground = (SolidColorBrush)findResource("ErrBrush"),
                Margin = new Thickness(0, 4, 0, 0)
            };
            cardStack.Children.Add(errLbl);

            card.Child = cardStack;
            stack.Children.Add(card);

            // Action buttons
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
            backBtn.Click += (s, e) => onBackClick();
            btnRow.Children.Add(backBtn);

            var connectBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("join", LocalizationService.Get("btn_connect_launch"), 16),
                Background = (SolidColorBrush)findResource("BlueBrush"),
                Style = (Style)findResource("NepButtonStyle"),
                Margin = new Thickness(6, 0, 6, 0)
            };

            connectBtn.Click += (s, e) =>
            {
                string username = userTb.Text.Trim();
                if (string.IsNullOrWhiteSpace(username)) username = "Carlitos";
                string addr = addrTb.Text.Trim();
                if (string.IsNullOrEmpty(addr) || !addr.Contains(':'))
                {
                    errLbl.Text = "Format must be host:port";
                    return;
                }
                var parts = addr.Split(':', 2);
                if (!int.TryParse(parts[1], out _))
                {
                    errLbl.Text = "Port must be a number";
                    return;
                }
                errLbl.Text = "";

                cfg.Username = username;
                cfg.JoinAddr = addr;
                ConfigManager.SaveConfig(cfg);

                onConnectClick(username, addr);
            };
            btnRow.Children.Add(connectBtn);

            stack.Children.Add(btnRow);
            grid.Children.Add(stack);

            return grid;
        }
    }
}

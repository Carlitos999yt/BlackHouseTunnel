using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;
using Microsoft.Win32;
using NepTunnel.Services;
using NepTunnel.Views;

namespace NepTunnel
{
    public partial class MainWindow : Window
    {
        private string _studioPath = "";
        private UIElement? _currentView = null;
        private bool _isNavigating = false;
        private bool _isHostActive = false;
        private bool _isJoinActive = false;

        private readonly EchoServer _echoServer = new();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var cfg = ConfigManager.LoadConfig();
            if (!string.IsNullOrEmpty(cfg.Language) && (cfg.Language == "es" || cfg.Language == "pt" || cfg.Language == "en"))
            {
                LocalizationService.CurrentLanguage = cfg.Language;
            }
            else
            {
                string detected = LocalizationService.DetectDefaultSystemLanguage();
                LocalizationService.CurrentLanguage = detected;
                cfg.Language = detected;
                ConfigManager.SaveConfig(cfg);
            }
            UpdateLangBtnText();

            // Load Banner image asynchronously
            _ = Task.Run(async () =>
            {
                var bannerBmp = await BannerService.GetBannerImageAsync();
                if (bannerBmp != null)
                {
                    Dispatcher.Invoke(() => BannerImage.Source = bannerBmp);
                }
            });

            _ = Task.Run(() =>
            {
                RbxmBridgeServer.Start();
                string path = RobloxStudioService.GetStudioPath();
                Dispatcher.Invoke(() => OnStudioFound(path));
            });

            ShowBootView();
        }

        #region Language Switcher
        private void LangBtn_Click(object sender, RoutedEventArgs e)
        {
            LocalizationService.CurrentLanguage = LocalizationService.CurrentLanguage switch
            {
                "en" => "es",
                "es" => "pt",
                _ => "en"
            };

            UpdateLangBtnText();

            var cfg = ConfigManager.LoadConfig();
            cfg.Language = LocalizationService.CurrentLanguage;
            ConfigManager.SaveConfig(cfg);

            if (!_isHostActive && !_isJoinActive)
            {
                ShowMainMenuView("left");
            }
        }

        private void UpdateLangBtnText()
        {
            LangBtn.Content = LocalizationService.CurrentLanguage switch
            {
                "es" => "🌐 Español",
                "pt" => "🌐 Português",
                _ => "🌐 English"
            };
        }
        #endregion

        #region Custom Caption Button Event Handlers
        private void WinMinBtn_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void WinMaxBtn_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(this);
                WinMaxBtn.Content = "🗖";
            }
            else
            {
                SystemCommands.MaximizeWindow(this);
                WinMaxBtn.Content = "🗗";
            }
        }

        private void WinCloseBtn_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }
        #endregion

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isHostActive || _isJoinActive)
            {
                e.Cancel = true;
                string actionStr = _isHostActive ? LocalizationService.Get("alert_stop_host_msg") : LocalizationService.Get("alert_disc_msg");
                ShowConfirmationAlert(
                    LocalizationService.Get("alert_stop_host_title"),
                    actionStr,
                    LocalizationService.Get("alert_stop_host_btn"),
                    () =>
                    {
                        _isHostActive = false;
                        _isJoinActive = false;
                        _echoServer.Stop();
                        UdpProxy.StopProxy(wait: false);
                        RbxmBridgeServer.Stop();
                        RobloxStudioService.StopAllStudioProcesses();
                        Application.Current.Shutdown();
                    }
                );
                return;
            }

            _echoServer.Stop();
            UdpProxy.StopProxy(wait: false);
            RbxmBridgeServer.Stop();
            RobloxStudioService.StopAllStudioProcesses();
        }

        private void SetStatus(string msg, SolidColorBrush? color = null)
        {
            StatusLabel.Text = msg;
            StatusLabel.Foreground = color ?? (SolidColorBrush)FindResource("MuteBrush");
        }

        private void OnStudioFound(string path)
        {
            _studioPath = path;
            UpdateStudioStatusText();
            ShowMainMenuView();
        }

        private void UpdateStudioStatusText()
        {
            if (_studioPath == RobloxStudioService.VINEGAR)
            {
                SetStatus("Studio found  ·  Vinegar (Flatpak) — Linux", (SolidColorBrush)FindResource("OkBrush"));
            }
            else if (!string.IsNullOrEmpty(_studioPath))
            {
                string shortPath = _studioPath.Length > 55 ? _studioPath.Substring(0, 52) + "…" : _studioPath;
                SetStatus($"Studio found  ·  {shortPath}", (SolidColorBrush)FindResource("OkBrush"));
            }
            else
            {
                var cfg = ConfigManager.LoadConfig();
                if (!string.IsNullOrEmpty(cfg.Studio) && File.Exists(cfg.Studio))
                {
                    _studioPath = cfg.Studio;
                    string shortPath = cfg.Studio.Length > 55 ? cfg.Studio.Substring(0, 52) + "…" : cfg.Studio;
                    SetStatus($"Studio loaded from config  ·  {shortPath}", (SolidColorBrush)FindResource("OkBrush"));
                }
                else
                {
                    string osName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
                                   RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" : "Linux";
                    SetStatus($"Studio not found on {osName} — use Browse to locate it", (SolidColorBrush)FindResource("ErrBrush"));
                }
            }
        }

        #region Custom Dark Confirmation Alert Box Helper
        private void ShowConfirmationAlert(string title, string message, string confirmBtnText, Action onConfirm)
        {
            AlertTitleTxt.Text = title;
            AlertMessageTxt.Text = message;
            AlertConfirmBtn.Content = confirmBtnText;
            AlertCancelBtn.Content = LocalizationService.Get("alert_cancel");

            RoutedEventHandler? cancelHandler = null;
            RoutedEventHandler? confirmHandler = null;

            cancelHandler = (s, e) =>
            {
                AlertCancelBtn.Click -= cancelHandler;
                AlertConfirmBtn.Click -= confirmHandler;
                AlertOverlayGrid.Visibility = Visibility.Collapsed;
            };

            confirmHandler = (s, e) =>
            {
                AlertCancelBtn.Click -= cancelHandler;
                AlertConfirmBtn.Click -= confirmHandler;
                AlertOverlayGrid.Visibility = Visibility.Collapsed;
                onConfirm?.Invoke();
            };

            AlertCancelBtn.Click += cancelHandler;
            AlertConfirmBtn.Click += confirmHandler;

            AlertOverlayGrid.Visibility = Visibility.Visible;
        }
        #endregion

        #region Studio Selection Modal Helper
        private void ShowStudioSelectorModal(TextBlock studioLbl)
        {
            var installations = RobloxStudioService.GetDetectedStudioInstallations();
            if (!string.IsNullOrEmpty(_studioPath) && File.Exists(_studioPath) &&
                !installations.Any(i => i.Path.Equals(_studioPath, StringComparison.OrdinalIgnoreCase)))
            {
                installations.Insert(0, new RobloxStudioService.StudioInstallation("Roblox Studio (Ruta Activa)", _studioPath, "RSM", true));
            }

            var overlayGrid = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x0A, 0x0A, 0x14))
            };

            var modalBorder = new Border
            {
                Width = 540,
                MaxHeight = 440,
                Background = (SolidColorBrush)FindResource("CardBrush"),
                BorderBrush = (SolidColorBrush)FindResource("AccBrush"),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var modalStack = new StackPanel();

            modalStack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("modal_studio_title"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)FindResource("TextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            });

            modalStack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("modal_studio_sub"),
                FontSize = 12,
                Foreground = (SolidColorBrush)FindResource("MuteBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 14)
            });

            var listStack = new StackPanel();

            if (installations.Count == 0)
            {
                listStack.Children.Add(new TextBlock
                {
                    Text = LocalizationService.Get("modal_studio_empty"),
                    FontSize = 13,
                    Foreground = (SolidColorBrush)FindResource("WarnBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 10)
                });
            }
            else
            {
                foreach (var inst in installations)
                {
                    bool isSelected = !string.IsNullOrEmpty(_studioPath) &&
                                       inst.Path.Equals(_studioPath, StringComparison.OrdinalIgnoreCase);

                    var itemCard = new Border
                    {
                        Background = isSelected
                            ? new SolidColorBrush(Color.FromArgb(0x35, 0x8B, 0x5C, 0xF6))
                            : (SolidColorBrush)FindResource("Card2Brush"),
                        BorderBrush = isSelected
                            ? (SolidColorBrush)FindResource("AccBrush")
                            : (SolidColorBrush)FindResource("BordBrush"),
                        BorderThickness = new Thickness(isSelected ? 2 : 1),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(12, 10, 12, 10),
                        Margin = new Thickness(0, 0, 0, 8),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };

                    var itemStack = new StackPanel();

                    var headerGrid = new Grid();
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var titleTxt = new TextBlock
                    {
                        Text = inst.Name,
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = isSelected ? (SolidColorBrush)FindResource("GlowBrush") : (SolidColorBrush)FindResource("TextBrush")
                    };
                    Grid.SetColumn(titleTxt, 0); headerGrid.Children.Add(titleTxt);

                    var badgePanel = new StackPanel { Orientation = Orientation.Horizontal };

                    if (isSelected)
                    {
                        var activeTag = new Border
                        {
                            Background = (SolidColorBrush)FindResource("AccBrush"),
                            CornerRadius = new CornerRadius(4),
                            Padding = new Thickness(6, 2, 6, 2),
                            Margin = new Thickness(4, 0, 0, 0)
                        };
                        activeTag.Child = new TextBlock
                        {
                            Text = LocalizationService.Get("modal_studio_active"),
                            FontSize = 10,
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.White
                        };
                        badgePanel.Children.Add(activeTag);
                    }

                    if (inst.IsRecommended)
                    {
                        var recTag = new Border
                        {
                            Background = isSelected ? new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED)) : (SolidColorBrush)FindResource("AccBrush"),
                            CornerRadius = new CornerRadius(4),
                            Padding = new Thickness(6, 2, 6, 2),
                            Margin = new Thickness(4, 0, 0, 0)
                        };
                        recTag.Child = new TextBlock
                        {
                            Text = LocalizationService.Get("modal_studio_recommended"),
                            FontSize = 10,
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.White
                        };
                        badgePanel.Children.Add(recTag);
                    }
                    else if (inst.Type == "Bloxstrap")
                    {
                        var tagBorder = new Border
                        {
                            Background = (SolidColorBrush)FindResource("TealBrush"),
                            CornerRadius = new CornerRadius(4),
                            Padding = new Thickness(6, 2, 6, 2),
                            Margin = new Thickness(4, 0, 0, 0)
                        };
                        tagBorder.Child = new TextBlock
                        {
                            Text = "BLOXSTRAP",
                            FontSize = 10,
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.White
                        };
                        badgePanel.Children.Add(tagBorder);
                    }
                    else if (inst.Type == "Oficial")
                    {
                        var tagBorder = new Border
                        {
                            Background = (SolidColorBrush)FindResource("BlueBrush"),
                            CornerRadius = new CornerRadius(4),
                            Padding = new Thickness(6, 2, 6, 2),
                            Margin = new Thickness(4, 0, 0, 0)
                        };
                        tagBorder.Child = new TextBlock
                        {
                            Text = "OFICIAL",
                            FontSize = 10,
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.White
                        };
                        badgePanel.Children.Add(tagBorder);
                    }

                    Grid.SetColumn(badgePanel, 1); headerGrid.Children.Add(badgePanel);
                    itemStack.Children.Add(headerGrid);

                    itemStack.Children.Add(new TextBlock
                    {
                        Text = inst.Path,
                        FontSize = 11,
                        Foreground = (SolidColorBrush)FindResource("MuteBrush"),
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        Margin = new Thickness(0, 4, 0, 0)
                    });

                    itemCard.Child = itemStack;

                    string selectedPath = inst.Path;
                    itemCard.MouseLeftButtonDown += (s, e) =>
                    {
                        _studioPath = selectedPath;
                        studioLbl.Text = _studioPath;
                        studioLbl.Foreground = (SolidColorBrush)FindResource("GlowBrush");
                        SetStatus($"Ruta Studio seleccionada: {inst.Name}", (SolidColorBrush)FindResource("OkBrush"));
                        RootMainGrid.Children.Remove(overlayGrid);
                    };

                    listStack.Children.Add(itemCard);
                }
            }

            var listScroll = new ScrollViewer
            {
                MaxHeight = 250,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = listStack,
                Margin = new Thickness(0, 0, 0, 14)
            };
            modalStack.Children.Add(listScroll);

            var closeBtn = new Button
            {
                Content = LocalizationService.Get("modal_studio_close"),
                Background = (SolidColorBrush)FindResource("Card2Brush"),
                Style = (Style)FindResource("NepButtonStyle"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(16, 6, 16, 6)
            };
            closeBtn.Click += (s, e) => RootMainGrid.Children.Remove(overlayGrid);
            modalStack.Children.Add(closeBtn);

            modalBorder.Child = modalStack;
            overlayGrid.Children.Add(modalBorder);
            Grid.SetRowSpan(overlayGrid, 10);
            Grid.SetColumnSpan(overlayGrid, 10);
            Panel.SetZIndex(overlayGrid, 9999);

            RootMainGrid.Children.Add(overlayGrid);
        }
        #endregion

        #region Navigation Engine
        private void NavigateTo(UIElement newView, string direction = "left")
        {
            if (_isNavigating) return;
            if (_currentView == null)
            {
                ViewContainer.Children.Clear();
                ViewContainer.Children.Add(newView);
                _currentView = newView;
                return;
            }

            _isNavigating = true;
            double width = ViewContainer.ActualWidth > 0 ? ViewContainer.ActualWidth : 720;

            var oldView = _currentView;
            ViewContainer.Children.Add(newView);

            var oldTt = new TranslateTransform();
            var newTt = new TranslateTransform();

            oldView.RenderTransform = oldTt;
            newView.RenderTransform = newTt;

            double startNewX = direction == "left" ? width : -width;
            double endOldX = direction == "left" ? -width : width;

            newTt.X = startNewX;

            var animDuration = TimeSpan.FromMilliseconds(260);
            var cubicEase = new CubicEase { EasingMode = EasingMode.EaseOut };

            var oldAnim = new DoubleAnimation(0, endOldX, animDuration) { EasingFunction = cubicEase };
            var newAnim = new DoubleAnimation(startNewX, 0, animDuration) { EasingFunction = cubicEase };

            newAnim.Completed += (s, e) =>
            {
                ViewContainer.Children.Remove(oldView);
                _currentView = newView;
                _isNavigating = false;
            };

            oldTt.BeginAnimation(TranslateTransform.XProperty, oldAnim);
            newTt.BeginAnimation(TranslateTransform.XProperty, newAnim);
        }
        #endregion

        #region View 1: Boot / Splash View
        private void ShowBootView()
        {
            var grid = new Grid { Background = (SolidColorBrush)FindResource("BgBrush") };
            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var canvas = new Canvas
            {
                Width = 96,
                Height = 96,
                Margin = new Thickness(0, 0, 0, 16)
            };

            double cx = 48, cy = 48, r = 30;

            for (int i = 4; i >= 1; i--)
            {
                double gr = r + i * 6;
                var ring = new Ellipse
                {
                    Width = gr * 2,
                    Height = gr * 2,
                    Stroke = (SolidColorBrush)FindResource("AccBrush"),
                    StrokeThickness = 1,
                    Opacity = 0.3 + (i * 0.1)
                };
                Canvas.SetLeft(ring, cx - gr);
                Canvas.SetTop(ring, cy - gr);
                canvas.Children.Add(ring);
            }

            var moon = new Ellipse
            {
                Width = r * 2,
                Height = r * 2,
                Fill = (SolidColorBrush)FindResource("MoonBrush"),
                Stroke = (SolidColorBrush)FindResource("GlowBrush"),
                StrokeThickness = 1
            };
            Canvas.SetLeft(moon, cx - r);
            Canvas.SetTop(moon, cy - r);
            canvas.Children.Add(moon);

            double so = r * 0.55, sr = r * 1.07;
            var shadow = new Ellipse
            {
                Width = sr * 2,
                Height = sr * 2,
                Fill = (SolidColorBrush)FindResource("BgBrush")
            };
            Canvas.SetLeft(shadow, cx + so - sr);
            Canvas.SetTop(shadow, cy - sr);
            canvas.Children.Add(shadow);

            stack.Children.Add(canvas);

            var title = new TextBlock
            {
                Text = "NEP TUNNEL",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)FindResource("TextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stack.Children.Add(title);

            var subtitle = new TextBlock
            {
                Text = "Locating Roblox Studio…",
                FontSize = 13,
                Foreground = (SolidColorBrush)FindResource("MuteBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 8)
            };
            stack.Children.Add(subtitle);

            var spinner = new TextBlock
            {
                Text = "●  ○  ○",
                FontSize = 11,
                Foreground = (SolidColorBrush)FindResource("AccBrush"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stack.Children.Add(spinner);

            grid.Children.Add(stack);
            NavigateTo(grid);
        }
        #endregion

        #region View 2: Main Menu View
        private void ShowMainMenuView(string direction = "left")
        {
            _isHostActive = false;
            _isJoinActive = false;
            _echoServer.Stop();
            UdpProxy.StopProxy(wait: false);
            RobloxStudioService.StopAllStudioProcesses();
            UpdateStudioStatusText();

            var cfg = ConfigManager.LoadConfig();
            var mainGrid = new Grid { Background = (SolidColorBrush)FindResource("BgBrush") };

            var menuElement = MainMenuView.Create(
                cfg,
                _studioPath,
                onHostClick: () => ShowHostConfigView(),
                onJoinClick: () => ShowJoinConfigView(),
                onEchoClick: () => ShowEchoTestView(),
                onRbxmClick: () => ShowRbxmImporterView(),
                onRsmClick: () => ShowRsmAssistantView(),
                onStudioChanged: (newPath) => { _studioPath = newPath; cfg.Studio = newPath; ConfigManager.SaveConfig(cfg); },
                onShowStudioSelector: (lbl) => ShowStudioSelectorModal(lbl),
                setStatus: (msg, brush) => SetStatus(msg, brush),
                findResource: (resKey) => FindResource(resKey)
            );

            mainGrid.Children.Add(menuElement);
            NavigateTo(mainGrid, direction);
        }
        #endregion

        private void OpenImageModal(BitmapImage bitmap)
        {
            ZoomedImageControl.Source = bitmap;
            ImageModalOverlay.Visibility = Visibility.Visible;
        }

        private void CloseImageModal_Click(object sender, RoutedEventArgs e)
        {
            ImageModalOverlay.Visibility = Visibility.Collapsed;
        }

        #region View 3: Tutorial & Help View
        private void ShowTutorialView()
        {
            var tutElement = ToolViews.CreateTutorialView(
                onBackClick: () => ShowMainMenuView("right"),
                onOpenImageModal: (bitmap) => OpenImageModal(bitmap),
                findResource: (resKey) => FindResource(resKey)
            );

            NavigateTo((UIElement)tutElement, "left");
        }
        #endregion

        #region View 4: RBXM Importer View
        private void ShowRbxmImporterView()
        {
            var cfg = ConfigManager.LoadConfig();
            var savedMaps = new List<string>(cfg.SavedMaps);

            var grid = new Grid { Background = (SolidColorBrush)FindResource("BgBrush") };
            var stack = new StackPanel { Margin = new Thickness(20, 16, 20, 16) };

            stack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("rbxm_title"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)FindResource("TextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            stack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("rbxm_sub"),
                FontSize = 13,
                Foreground = (SolidColorBrush)FindResource("MuteBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 12)
            });

            var mapStatusLbl = new TextBlock
            {
                Text = "",
                FontSize = 12,
                Foreground = (SolidColorBrush)FindResource("OkBrush"),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 4, 4)
            };
            stack.Children.Add(mapStatusLbl);

            var listBorder = new Border
            {
                Background = (SolidColorBrush)FindResource("CardBrush"),
                BorderBrush = (SolidColorBrush)FindResource("BordBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Height = 180,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var listScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var listStack = new StackPanel();

            Action refreshList = null!;
            refreshList = () =>
            {
                listStack.Children.Clear();
                if (savedMaps.Count == 0)
                {
                    listStack.Children.Add(new TextBlock
                    {
                        Text = LocalizationService.Get("rbxm_empty"),
                        FontSize = 13,
                        Foreground = (SolidColorBrush)FindResource("MuteBrush"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 40, 0, 0)
                    });
                    return;
                }

                for (int i = 0; i < savedMaps.Count; i++)
                {
                    int index = i;
                    string p = savedMaps[i];
                    bool exists = File.Exists(p);

                    var itemRow = new Grid
                    {
                        Background = i % 2 == 0 ? (SolidColorBrush)FindResource("Card2Brush") : (SolidColorBrush)FindResource("CardBrush"),
                        Margin = new Thickness(0, 1, 0, 1)
                    };
                    itemRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    itemRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var leftStack = new StackPanel { Margin = new Thickness(10, 6, 10, 6) };
                    leftStack.Children.Add(new TextBlock
                    {
                        Text = Path.GetFileName(p),
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = exists ? (SolidColorBrush)FindResource("TextBrush") : (SolidColorBrush)FindResource("MuteBrush")
                    });

                    string shortP = p.Length > 65 ? p.Substring(0, 62) + "…" : p;
                    leftStack.Children.Add(new TextBlock
                    {
                        Text = exists ? shortP : $"⚠ missing {shortP}",
                        FontSize = 11,
                        Foreground = exists ? (SolidColorBrush)FindResource("MuteBrush") : (SolidColorBrush)FindResource("ErrBrush")
                    });

                    Grid.SetColumn(leftStack, 0);
                    itemRow.Children.Add(leftStack);

                    var rightStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 4, 8, 4) };

                    var sendBtn = new Button
                    {
                        Content = IconFactory.CreateButtonContent("send", LocalizationService.Get("btn_send_studio"), 14),
                        Background = (SolidColorBrush)FindResource("AccBrush"),
                        Style = (Style)FindResource("NepButtonStyle"),
                        Padding = new Thickness(10, 4, 10, 4),
                        IsEnabled = exists,
                        Margin = new Thickness(0, 0, 6, 0)
                    };
                    sendBtn.Click += (s, e) =>
                    {
                        var (ok, resMsg) = RbxmBridgeServer.QueueRbxm(p);
                        if (ok)
                        {
                            mapStatusLbl.Text = $"✓ \"{resMsg}\" queued — click ▶ Listen in Studio plugin";
                            mapStatusLbl.Foreground = (SolidColorBrush)FindResource("OkBrush");
                            SetStatus($"Map queued: {resMsg}", (SolidColorBrush)FindResource("OkBrush"));
                        }
                        else
                        {
                            mapStatusLbl.Text = $"✗ {resMsg}";
                            mapStatusLbl.Foreground = (SolidColorBrush)FindResource("ErrBrush");
                        }
                    };
                    rightStack.Children.Add(sendBtn);

                    var removeBtn = new Button
                    {
                        Content = IconFactory.CreateButtonContent("trash", "", 14),
                        Background = (SolidColorBrush)FindResource("CardBrush"),
                        Style = (Style)FindResource("NepButtonStyle"),
                        Padding = new Thickness(8, 4, 8, 4)
                    };
                    removeBtn.Click += (s, e) =>
                    {
                        savedMaps.RemoveAt(index);
                        var cfg2 = ConfigManager.LoadConfig();
                        cfg2.SavedMaps = savedMaps;
                        ConfigManager.SaveConfig(cfg2);
                        mapStatusLbl.Text = "Removed.";
                        mapStatusLbl.Foreground = (SolidColorBrush)FindResource("MuteBrush");
                        refreshList();
                    };
                    rightStack.Children.Add(removeBtn);

                    Grid.SetColumn(rightStack, 1);
                    itemRow.Children.Add(rightStack);

                    listStack.Children.Add(itemRow);
                }
            };

            refreshList();
            listScroll.Content = listStack;
            listBorder.Child = listScroll;
            stack.Children.Add(listBorder);

            // Bottom Buttons
            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var backBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("back", LocalizationService.Get("back"), 14),
                Background = (SolidColorBrush)FindResource("CardBrush"),
                Style = (Style)FindResource("NepButtonStyle"),
                Margin = new Thickness(6, 0, 6, 0)
            };
            backBtn.Click += (s, e) => ShowMainMenuView("right");
            btnRow.Children.Add(backBtn);

            var addBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("folder", LocalizationService.Get("btn_add_rbxm"), 14),
                Background = new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED)),
                Style = (Style)FindResource("NepButtonStyle"),
                Margin = new Thickness(6, 0, 6, 0)
            };
            addBtn.Click += (s, e) =>
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Select .rbxm map file(s)",
                    Filter = "Roblox Model (*.rbxm;*.rbxmx)|*.rbxm;*.rbxmx|All files (*.*)|*.*",
                    Multiselect = true
                };
                if (dlg.ShowDialog() == true)
                {
                    int added = 0;
                    foreach (var fn in dlg.FileNames)
                    {
                        string fullP = Path.GetFullPath(fn);
                        if (!savedMaps.Contains(fullP))
                        {
                            savedMaps.Add(fullP);
                            added++;
                        }
                    }
                    if (added > 0)
                    {
                        var cfg2 = ConfigManager.LoadConfig();
                        cfg2.SavedMaps = savedMaps;
                        ConfigManager.SaveConfig(cfg2);
                        mapStatusLbl.Text = $"Added {added} map(s).";
                        mapStatusLbl.Foreground = (SolidColorBrush)FindResource("OkBrush");
                        refreshList();
                    }
                }
            };
            btnRow.Children.Add(addBtn);
            stack.Children.Add(btnRow);

            // Instructions Hint Box
            var hintBox = new Border
            {
                Background = (SolidColorBrush)FindResource("Card2Brush"),
                BorderBrush = (SolidColorBrush)FindResource("BordBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12)
            };
            var hintStack = new StackPanel();
            hintStack.Children.Add(new TextBlock { Text = LocalizationService.Get("rbxm_how_works_title"), FontSize = 12, FontWeight = FontWeights.Bold, Foreground = (SolidColorBrush)FindResource("GlowBrush"), Margin = new Thickness(0, 0, 0, 4) });
            hintStack.Children.Add(new TextBlock { Text = LocalizationService.Get("rbxm_how_works_1"), FontSize = 11, Foreground = (SolidColorBrush)FindResource("MuteBrush") });
            hintStack.Children.Add(new TextBlock { Text = LocalizationService.Get("rbxm_how_works_2"), FontSize = 11, Foreground = (SolidColorBrush)FindResource("MuteBrush") });
            hintStack.Children.Add(new TextBlock { Text = LocalizationService.Get("rbxm_how_works_3"), FontSize = 11, Foreground = (SolidColorBrush)FindResource("MuteBrush") });
            hintBox.Child = hintStack;

            stack.Children.Add(hintBox);
            grid.Children.Add(stack);

            NavigateTo(grid, "left");
        }
        #endregion



        #region View: RSM Assistant View
        private void ShowRsmAssistantView()
        {
            var grid = new Grid { Background = (SolidColorBrush)FindResource("BgBrush") };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var stack = new StackPanel { Margin = new Thickness(24, 12, 24, 12) };

            stack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("rsm_title"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)FindResource("TextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            stack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("rsm_sub"),
                FontSize = 13,
                Foreground = (SolidColorBrush)FindResource("MuteBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 12)
            });

            // Status Card
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string rsmExePath = Path.Combine(localAppData, "Roblox Studio", "RobloxStudioBeta.exe");
            string rsmFolder = Path.Combine(localAppData, "Roblox Studio");
            string rsmManagerFolder = Path.Combine(localAppData, "Roblox Studio Mod Manager");
            bool isRsmInstalled = File.Exists(rsmExePath);

            var statusCard = new Border
            {
                Background = (SolidColorBrush)FindResource("CardBrush"),
                BorderBrush = (SolidColorBrush)FindResource("BordBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var statusStack = new StackPanel();
            var statusHeader = new TextBlock
            {
                Text = isRsmInstalled
                    ? LocalizationService.Get("rsm_status_installed")
                    : LocalizationService.Get("rsm_status_not_installed"),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = isRsmInstalled ? (SolidColorBrush)FindResource("OkBrush") : (SolidColorBrush)FindResource("MuteBrush")
            };
            statusStack.Children.Add(statusHeader);

            if (isRsmInstalled)
            {
                statusStack.Children.Add(new TextBlock
                {
                    Text = rsmExePath,
                    FontSize = 12,
                    Foreground = (SolidColorBrush)FindResource("GlowBrush"),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
            }

            statusCard.Child = statusStack;
            stack.Children.Add(statusCard);

            // Error notice label
            var errNoticeLbl = new TextBlock
            {
                Text = "",
                FontSize = 12,
                Foreground = (SolidColorBrush)FindResource("ErrBrush"),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stack.Children.Add(errNoticeLbl);

            // Action Buttons Card (2x2 UniformGrid Layout)
            var actionsCard = new Border
            {
                Background = (SolidColorBrush)FindResource("CardBrush"),
                BorderBrush = (SolidColorBrush)FindResource("BordBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var actionsGrid = new System.Windows.Controls.Primitives.UniformGrid
            {
                Columns = 2,
                Rows = 2
            };

            // Live Progress Bar & Log Box for Installation
            var installProgress = new ProgressBar
            {
                Height = 6,
                Minimum = 0,
                Maximum = 1,
                Value = 0,
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 0, 0, 8)
            };
            stack.Children.Add(installProgress);

            var (logBorder, logBox) = CreateLogBox(height: 120);
            logBorder.Visibility = Visibility.Collapsed;
            logBorder.Margin = new Thickness(0, 0, 0, 10);
            stack.Children.Add(logBorder);

            // 1. Install / Reinstall RSM
            var installBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("folder", LocalizationService.Get("btn_rsm_install"), 14),
                Background = (SolidColorBrush)FindResource("AccBrush"),
                Style = (Style)FindResource("NepButtonStyle"),
                Margin = new Thickness(4)
            };
            installBtn.Click += (s, e) =>
            {
                errNoticeLbl.Text = "";
                installBtn.IsEnabled = false;
                logBorder.Visibility = Visibility.Visible;

                Task.Run(async () =>
                {
                    try
                    {
                        bool success = await RsmInstallerService.LaunchOfficialRsmBootstrapperAsync(
                            (msg, tag) => Dispatcher.Invoke(() => LogAppend(logBox, msg, tag))
                        );

                        Dispatcher.Invoke(() =>
                        {
                            installBtn.IsEnabled = true;
                            if (success)
                            {
                                SetStatus("RSM Mod Manager iniciado para v0.729.0.7290838", (SolidColorBrush)FindResource("OkBrush"));
                            }
                            else
                            {
                                errNoticeLbl.Text = LocalizationService.Get("rsm_error_notice");
                                SetStatus("RSM launch failed", (SolidColorBrush)FindResource("ErrBrush"));
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            installBtn.IsEnabled = true;
                            errNoticeLbl.Text = LocalizationService.Get("rsm_error_notice");
                            LogAppend(logBox, $"Exception: {ex.Message}", "err");
                            SetStatus("Installation error occurred", (SolidColorBrush)FindResource("ErrBrush"));
                        });
                    }
                });
            };
            actionsGrid.Children.Add(installBtn);

            // 2. Repair RSM
            var repairBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("test", LocalizationService.Get("btn_rsm_repair"), 14),
                Background = (SolidColorBrush)FindResource("WarnBrush"),
                Style = (Style)FindResource("NepButtonStyle"),
                Margin = new Thickness(4)
            };
            repairBtn.Click += (s, e) =>
            {
                errNoticeLbl.Text = "";
                repairBtn.IsEnabled = false;
                logBorder.Visibility = Visibility.Visible;

                Task.Run(async () =>
                {
                    try
                    {
                        bool success = await RsmInstallerService.RepairFromGitHubRepoAsync(
                            (msg, tag) => Dispatcher.Invoke(() => LogAppend(logBox, msg, tag)),
                            (pct) => { }
                        );

                        Dispatcher.Invoke(() =>
                        {
                            repairBtn.IsEnabled = true;
                            _studioPath = RobloxStudioService.GetStudioPath();
                            SetStatus("Reparación desde GitHub completada.", (SolidColorBrush)FindResource("OkBrush"));
                            ShowRsmAssistantView();
                        });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            repairBtn.IsEnabled = true;
                            errNoticeLbl.Text = LocalizationService.Get("rsm_error_notice");
                            LogAppend(logBox, $"Repair error: {ex.Message}", "err");
                            SetStatus("Repair error occurred", (SolidColorBrush)FindResource("ErrBrush"));
                        });
                    }
                });
            };
            actionsGrid.Children.Add(repairBtn);

            // 3. Open Studio Folder (%LOCALAPPDATA%\Roblox Studio)
            var openFolderBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("folder", LocalizationService.Get("btn_rsm_open_folder"), 14),
                Background = (SolidColorBrush)FindResource("BlueBrush"),
                Style = (Style)FindResource("NepButtonStyle"),
                Margin = new Thickness(4)
            };
            openFolderBtn.Click += (s, e) =>
            {
                try
                {
                    string targetFolder = Directory.Exists(rsmFolder) ? rsmFolder : localAppData;
                    Process.Start("explorer.exe", targetFolder);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open folder: {ex.Message}", "Folder Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            actionsGrid.Children.Add(openFolderBtn);

            // 4. Uninstall / Delete RSM
            var deleteBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("trash", LocalizationService.Get("btn_rsm_delete"), 14),
                Background = (SolidColorBrush)FindResource("ErrBrush"),
                Style = (Style)FindResource("NepButtonStyle"),
                Margin = new Thickness(4)
            };
            deleteBtn.Click += (s, e) =>
            {
                ShowConfirmationAlert(
                    LocalizationService.Get("rsm_alert_delete_title"),
                    LocalizationService.Get("rsm_alert_delete_msg"),
                    LocalizationService.Get("btn_rsm_delete"),
                    () =>
                    {
                        try
                        {
                            ForceDeleteDirectory(rsmFolder);
                            ForceDeleteDirectory(rsmManagerFolder);
                            RsmInstallerService.CleanRsmRegistryAndProtocols();
                            _studioPath = RobloxStudioService.GetStudioPath();
                            SetStatus("RSM eliminado por completo. Registro de Windows y navegador restaurados.", (SolidColorBrush)FindResource("WarnBrush"));
                            ShowRsmAssistantView();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error al eliminar RSM: {ex.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                );
            };
            actionsGrid.Children.Add(deleteBtn);

            actionsCard.Child = actionsGrid;
            stack.Children.Add(actionsCard);

            // Back button
            var backBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("back", LocalizationService.Get("back"), 14),
                Background = (SolidColorBrush)FindResource("CardBrush"),
                Style = (Style)FindResource("NepButtonStyle"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 16)
            };
            backBtn.Click += (s, e) => ShowMainMenuView("right");
            stack.Children.Add(backBtn);

            scrollViewer.Content = stack;
            grid.Children.Add(scrollViewer);

            NavigateTo(grid, "left");
        }
        #endregion

        #region Directory Force Deletion Helper
        private static void ForceDeleteDirectory(string targetDir)
        {
            if (!Directory.Exists(targetDir)) return;

            try
            {
                var processes = Process.GetProcesses()
                    .Where(p => p.ProcessName.Contains("RobloxStudio", StringComparison.OrdinalIgnoreCase));
                foreach (var proc in processes)
                {
                    try { proc.Kill(); proc.WaitForExit(1000); } catch { }
                }
            }
            catch { }

            try
            {
                var dirInfo = new DirectoryInfo(targetDir);
                foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        if (file.IsReadOnly) file.IsReadOnly = false;
                        file.Attributes = FileAttributes.Normal;
                    }
                    catch { }
                }

                foreach (var dir in dirInfo.GetDirectories("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        dir.Attributes = FileAttributes.Normal;
                    }
                    catch { }
                }
            }
            catch { }

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    Directory.Delete(targetDir, true);
                    break;
                }
                catch
                {
                    if (attempt == 3) throw;
                    System.Threading.Thread.Sleep(300);
                }
            }
        }
        #endregion

        #region View 5: Echo Test View
        private void ShowEchoTestView()
        {
            var cfg = ConfigManager.LoadConfig();

            var grid = new Grid { Background = (SolidColorBrush)FindResource("BgBrush") };
            var stack = new StackPanel { Margin = new Thickness(18, 12, 18, 12) };

            stack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("echo_title"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)FindResource("TextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            stack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("echo_sub"),
                FontSize = 13,
                Foreground = (SolidColorBrush)FindResource("MuteBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 8)
            });

            // Entry Fields Row
            var fieldsRow = new Grid { Margin = new Thickness(6, 0, 6, 8) };
            fieldsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            fieldsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var portStack = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            portStack.Children.Add(new TextBlock { Text = LocalizationService.Get("lbl_studio_port_host"), FontSize = 11, Foreground = (SolidColorBrush)FindResource("MuteBrush") });
            var portTb = new TextBox { Text = cfg.Port, Style = (Style)FindResource("NepTextBoxStyle"), Margin = new Thickness(0, 2, 0, 0) };
            portStack.Children.Add(portTb);
            Grid.SetColumn(portStack, 0);
            fieldsRow.Children.Add(portStack);

            var addrStack = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            addrStack.Children.Add(new TextBlock { Text = LocalizationService.Get("lbl_tunnel_addr_joiner"), FontSize = 11, Foreground = (SolidColorBrush)FindResource("MuteBrush") });
            var addrTb = new TextBox { Text = cfg.HostAddr, Style = (Style)FindResource("NepTextBoxStyle"), Margin = new Thickness(0, 2, 0, 0) };
            addrStack.Children.Add(addrTb);
            Grid.SetColumn(addrStack, 1);
            fieldsRow.Children.Add(addrStack);

            stack.Children.Add(fieldsRow);

            // Log Console Box
            var logBox = CreateLogBox(height: 180);
            stack.Children.Add(logBox.Border);

            // Action Controls Row
            var ctrlsRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            };

            var backBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("back", LocalizationService.Get("back"), 14),
                Background = (SolidColorBrush)FindResource("CardBrush"),
                Style = (Style)FindResource("NepButtonStyle"),
                Margin = new Thickness(6, 0, 6, 0)
            };
            backBtn.Click += (s, e) =>
            {
                _echoServer.Stop();
                ShowMainMenuView("right");
            };
            ctrlsRow.Children.Add(backBtn);

            var echoHostBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("echo", LocalizationService.Get("btn_host_start_echo"), 16),
                Background = (SolidColorBrush)FindResource("TealBrush"),
                Style = (Style)FindResource("NepButtonStyle"),
                Margin = new Thickness(6, 0, 6, 0)
            };

            echoHostBtn.Click += (s, e) =>
            {
                if (_echoServer.IsRunning)
                {
                    _echoServer.Stop();
                    echoHostBtn.Content = IconFactory.CreateButtonContent("echo", LocalizationService.Get("btn_host_start_echo"), 16);
                    echoHostBtn.Background = (SolidColorBrush)FindResource("TealBrush");
                    LogAppend(logBox.RichText, $"Echo server stopped ({_echoServer.EchoedCount} total echoed)", "dim");
                    SetStatus("Echo server stopped", (SolidColorBrush)FindResource("MuteBrush"));
                }
                else
                {
                    if (!int.TryParse(portTb.Text.Trim(), out int p))
                    {
                        LogAppend(logBox.RichText, "Port must be a number", "err");
                        return;
                    }
                    if (_echoServer.Start(p, (m, t) => LogAppend(logBox.RichText, m, t)))
                    {
                        echoHostBtn.Content = IconFactory.CreateButtonContent("stop", LocalizationService.Get("btn_host_stop_echo"), 16);
                        echoHostBtn.Background = (SolidColorBrush)FindResource("ErrBrush");
                        LogAppend(logBox.RichText, $"✓ Echo server ACTIVE on 0.0.0.0:{p}", "ok");
                        LogAppend(logBox.RichText, "Waiting for joiner to send probe packets...", "warn");
                        SetStatus($"Echo server listening on port {p}", (SolidColorBrush)FindResource("OkBrush"));
                    }
                }
            };
            ctrlsRow.Children.Add(echoHostBtn);

            var joinEchoBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("echo", LocalizationService.Get("btn_join_run_echo"), 16),
                Background = (SolidColorBrush)FindResource("BlueBrush"),
                Style = (Style)FindResource("NepButtonStyle"),
                Margin = new Thickness(6, 0, 6, 0)
            };
            joinEchoBtn.Click += (s, e) =>
            {
                string addr = addrTb.Text.Trim();
                if (string.IsNullOrEmpty(addr) || !addr.Contains(':'))
                {
                    LogAppend(logBox.RichText, "Enter a tunnel address (host:port)", "err");
                    return;
                }
                var parts = addr.Split(':', 2);
                if (!int.TryParse(parts[1], out int rp))
                {
                    LogAppend(logBox.RichText, "Invalid tunnel port", "err");
                    return;
                }
                Task.Run(() => EchoClient.RunEchoTestAsync((m, t) => Dispatcher.Invoke(() => LogAppend(logBox.RichText, m, t)), parts[0], rp));
            };
            ctrlsRow.Children.Add(joinEchoBtn);

            stack.Children.Add(ctrlsRow);

            string[] howToLines = LocalizationService.Get("echo_how_to_use").Split('\n');
            foreach (var line in howToLines)
            {
                string tag = line.StartsWith("  HOST") || line.StartsWith("  UNIRSE") || line.StartsWith("  JOINER") || line.StartsWith("  ANFITRIÃO") ? "ok" :
                             line.StartsWith("CÓMO") || line.StartsWith("HOW") || line.StartsWith("COMO") ? "info" : "dim";
                LogAppend(logBox.RichText, line, tag);
            }
            LogAppend(logBox.RichText, "───────────────────────────────────────", "dim");

            grid.Children.Add(stack);
            NavigateTo(grid, "left");
        }
        #endregion

        #region View 6: Host Config View
        private void ShowHostConfigView()
        {
            var cfg = ConfigManager.LoadConfig();

            var hostElement = HostViews.CreateConfigView(
                cfg,
                _studioPath,
                onBackClick: () => ShowMainMenuView("right"),
                onTutorialClick: () => ShowTutorialView(),
                onLaunchServerClick: (uid, port, addr, mapPath, username) => ShowHostRunningView(uid, port, addr, mapPath, username),
                findResource: (resKey) => FindResource(resKey)
            );

            NavigateTo((UIElement)hostElement, "left");
        }
        #endregion

        #region View 7: Host Running View
        private void ShowHostRunningView(string uid, string port, string addr, string mapPath, string username = "Carlitos")
        {
            _isHostActive = true;
            string pg = Guid.NewGuid().ToString().ToUpper();
            string tg = Guid.NewGuid().ToString().ToUpper();

            var grid = new Grid { Background = (SolidColorBrush)FindResource("BgBrush") };
            var stack = new StackPanel { Margin = new Thickness(18, 12, 18, 12) };

            stack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("host_console_title"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)FindResource("TextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 4)
            });

            var logBox = CreateLogBox(height: 200);
            stack.Children.Add(logBox.Border);

            RobloxStudioService.OnStudioError = (msg, tag) =>
            {
                Dispatcher.Invoke(() => LogAppend(logBox.RichText, msg, tag));
            };

            var ctrlsRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var joinLocalBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("join", LocalizationService.Get("btn_join_locally"), 16),
                Background = (SolidColorBrush)FindResource("WarnBrush"),
                Style = (Style)FindResource("NepButtonStyle"),
                IsEnabled = false,
                Margin = new Thickness(6, 0, 6, 0)
            };
            joinLocalBtn.Click += (s, e) =>
            {
                try
                {
                    RobloxStudioService.LaunchClient(_studioPath, "127.0.0.1", port, pg, tg, "StudioPlayer_Host", username);
                    LogAppend(logBox.RichText, $"Local client launched as '{username}'.", "info");
                }
                catch (Exception ex)
                {
                    LogAppend(logBox.RichText, $"Launch error: {ex.Message}", "err");
                }
            };
            ctrlsRow.Children.Add(joinLocalBtn);

            var stopBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("stop", LocalizationService.Get("btn_stop_back"), 16),
                Background = (SolidColorBrush)FindResource("ErrBrush"),
                Style = (Style)FindResource("NepButtonStyle"),
                Margin = new Thickness(6, 0, 6, 0)
            };
            stopBtn.Click += (s, e) =>
            {
                ShowConfirmationAlert(
                    LocalizationService.Get("alert_stop_host_title"),
                    LocalizationService.Get("alert_stop_host_msg"),
                    LocalizationService.Get("alert_stop_host_btn"),
                    () => ShowMainMenuView("right")
                );
            };
            ctrlsRow.Children.Add(stopBtn);

            var testBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("test", LocalizationService.Get("test"), 14),
                Background = (SolidColorBrush)FindResource("Card2Brush"),
                Style = (Style)FindResource("NepButtonStyle"),
                Margin = new Thickness(6, 0, 6, 0)
            };
            testBtn.Click += (s, e) =>
            {
                string h = addr.Contains(':') ? addr.Split(':', 2)[0] : addr;
                int tp = addr.Contains(':') && int.TryParse(addr.Split(':', 2)[1], out int pVal) ? pVal : int.Parse(port);
                Task.Run(() => ConnectivityTester.RunConnectivityTestAsync(h, tp, (m, t) => Dispatcher.Invoke(() => LogAppend(logBox.RichText, m, t)), isHostSide: true, localServerPort: int.Parse(port)));
            };
            ctrlsRow.Children.Add(testBtn);

            stack.Children.Add(ctrlsRow);
            grid.Children.Add(stack);

            NavigateTo(grid, "left");

            Task.Run(async () =>
            {
                Dispatcher.Invoke(() =>
                {
                    LogAppend(logBox.RichText, $"Parent GUID: {pg}", "dim");
                    LogAppend(logBox.RichText, $"Play  GUID : {tg}", "dim");
                    LogAppend(logBox.RichText, $"Port       : {port}");
                    LogAppend(logBox.RichText, $"Address    : {addr}", "info");
                });

                if (!string.IsNullOrEmpty(mapPath) && File.Exists(mapPath))
                {
                    Dispatcher.Invoke(() => LogAppend(logBox.RichText, $"Injecting map: {Path.GetFileName(mapPath)}", "warn"));
                    if (MapInjector.InjectMap(mapPath))
                    {
                        Dispatcher.Invoke(() => LogAppend(logBox.RichText, "✓ Map copied to Roblox runtime cache", "ok"));
                    }
                    else
                    {
                        Dispatcher.Invoke(() => LogAppend(logBox.RichText, "✗ Failed to inject map. Studio might load default cache.", "err"));
                    }
                }

                Dispatcher.Invoke(() => LogAppend(logBox.RichText, "Launching Studio server process…"));
                try
                {
                    RobloxStudioService.LaunchServer(_studioPath, port, uid, pg, tg, username);
                    Dispatcher.Invoke(() => LogAppend(logBox.RichText, "Server started! Waiting 5 s for Studio init…", "ok"));
                    ConfigManager.WriteSessionLog(pg, tg, addr, port, uid);
                    await Task.Delay(5000);
                    Dispatcher.Invoke(() =>
                    {
                        LogAppend(logBox.RichText, "● SERVER IS LIVE", "ok");
                        try { Clipboard.SetText(addr); } catch { }
                        joinLocalBtn.IsEnabled = true;
                        SetStatus(LocalizationService.Get("status_live"), (SolidColorBrush)FindResource("OkBrush"));
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        LogAppend(logBox.RichText, $"ERROR: {ex.Message}", "err");
                        SetStatus("Server launch failed", (SolidColorBrush)FindResource("ErrBrush"));
                    });
                }
            });
        }
        #endregion

        #region View 8: Join Config View
        private void ShowJoinConfigView()
        {
            var cfg = ConfigManager.LoadConfig();

            var joinElement = JoinViews.CreateConfigView(
                cfg,
                onBackClick: () => ShowMainMenuView("right"),
                onConnectClick: (username, addr) =>
                {
                    var parts = addr.Split(':', 2);
                    int rp = int.Parse(parts[1]);
                    ShowJoinRunningView(parts[0], rp, username);
                },
                findResource: (resKey) => FindResource(resKey)
            );

            NavigateTo((UIElement)joinElement, "left");
        }
        #endregion

        #region View 9: Join Running View
        private void ShowJoinRunningView(string dstHost, int dstPort, string username = "Carlitos")
        {
            _isJoinActive = true;
            var grid = new Grid { Background = (SolidColorBrush)FindResource("BgBrush") };
            var stack = new StackPanel { Margin = new Thickness(18, 12, 18, 12) };

            stack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("join_console_title"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)FindResource("TextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 4)
            });

            var logBox = CreateLogBox(height: 200);
            stack.Children.Add(logBox.Border);

            RobloxStudioService.OnStudioError = (msg, tag) =>
            {
                Dispatcher.Invoke(() => LogAppend(logBox.RichText, msg, tag));
            };

            var ctrlsRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            Action disconnect = () =>
            {
                LogAppend(logBox.RichText, "Stopping proxy…", "warn");
                UdpProxy.StopProxy();
                SetStatus(LocalizationService.Get("status_disconnected"), (SolidColorBrush)FindResource("MuteBrush"));
                Task.Delay(400).ContinueWith(_ => Dispatcher.Invoke(() => ShowMainMenuView("right")));
            };

            var discBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("stop", LocalizationService.Get("btn_disc_back"), 16),
                Background = (SolidColorBrush)FindResource("ErrBrush"),
                Style = (Style)FindResource("NepButtonStyle"),
                Margin = new Thickness(6, 0, 6, 0)
            };
            discBtn.Click += (s, e) =>
            {
                ShowConfirmationAlert(
                    LocalizationService.Get("alert_disc_title"),
                    LocalizationService.Get("alert_disc_msg"),
                    LocalizationService.Get("alert_disc_btn"),
                    () => disconnect()
                );
            };
            ctrlsRow.Children.Add(discBtn);

            var testBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("test", LocalizationService.Get("test"), 14),
                Background = (SolidColorBrush)FindResource("Card2Brush"),
                Style = (Style)FindResource("NepButtonStyle"),
                Margin = new Thickness(6, 0, 6, 0)
            };
            testBtn.Click += (s, e) =>
            {
                Task.Run(() => ConnectivityTester.RunConnectivityTestAsync(dstHost, dstPort, (m, t) => Dispatcher.Invoke(() => LogAppend(logBox.RichText, m, t)), isHostSide: false));
            };
            ctrlsRow.Children.Add(testBtn);

            stack.Children.Add(ctrlsRow);
            grid.Children.Add(stack);

            NavigateTo(grid, "left");

            Task.Run(async () =>
            {
                string pg = Guid.NewGuid().ToString().ToUpper();
                string tg = Guid.NewGuid().ToString().ToUpper();

                Dispatcher.Invoke(() =>
                {
                    LogAppend(logBox.RichText, $"Target     : {dstHost}:{dstPort}", "info");
                    LogAppend(logBox.RichText, $"Local proxy: 127.0.0.1:{UdpProxy.PROXY_PORT}");
                    LogAppend(logBox.RichText, "Starting UDP proxy…");
                });

                bool ok = UdpProxy.StartProxy(dstHost, dstPort);
                if (!ok)
                {
                    Dispatcher.Invoke(() =>
                    {
                        LogAppend(logBox.RichText, $"Failed to bind port {UdpProxy.PROXY_PORT}. Is another session running?", "err");
                        SetStatus($"Proxy failed — port {UdpProxy.PROXY_PORT} busy?", (SolidColorBrush)FindResource("ErrBrush"));
                    });
                    return;
                }

                Dispatcher.Invoke(() =>
                {
                    LogAppend(logBox.RichText, $"Proxy active on 127.0.0.1:{UdpProxy.PROXY_PORT}", "ok");
                    LogAppend(logBox.RichText, $"Warming tunnel ({UdpProxy.WARM_PACKETS} probes)…", "warn");
                });

                int warmed = UdpProxy.WarmTunnel(dstHost, dstPort);
                Dispatcher.Invoke(() =>
                {
                    if (warmed > 0)
                    {
                        LogAppend(logBox.RichText, $"✓ Tunnel warmed ({warmed}/{UdpProxy.WARM_PACKETS} sent)", "ok");
                    }
                    else
                    {
                        LogAppend(logBox.RichText, "Warm-up skipped (proxy stopped early)", "dim");
                    }
                });

                await Task.Delay(250);

                Dispatcher.Invoke(() =>
                {
                    LogAppend(logBox.RichText, $"Parent GUID: {pg}", "dim");
                    LogAppend(logBox.RichText, $"Play  GUID : {tg}", "dim");
                    LogAppend(logBox.RichText, "Launching Studio client…");
                });

                try
                {
                    RobloxStudioService.LaunchClient(_studioPath, "127.0.0.1", UdpProxy.PROXY_PORT.ToString(), pg, tg, "StudioPlayer_Proxy", username);
                    Dispatcher.Invoke(() =>
                    {
                        LogAppend(logBox.RichText, "● CONNECTED — Studio launched", "ok");
                        SetStatus(LocalizationService.Get("status_connected"), (SolidColorBrush)FindResource("OkBrush"));
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        LogAppend(logBox.RichText, $"Studio launch error: {ex.Message}", "err");
                        UdpProxy.StopProxy();
                        SetStatus("Studio launch failed", (SolidColorBrush)FindResource("ErrBrush"));
                    });
                }
            });
        }
        #endregion

        #region UI Log Box Helper
        private (Border Border, RichTextBox RichText) CreateLogBox(double height = 180)
        {
            var rtb = new RichTextBox
            {
                Background = new SolidColorBrush(Color.FromRgb(0x06, 0x02, 0x10)),
                Foreground = new SolidColorBrush(Color.FromRgb(0xB3, 0x9D, 0xDB)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6)
            };

            var border = new Border
            {
                BorderBrush = (SolidColorBrush)FindResource("BordBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Height = height,
                Child = rtb
            };

            return (border, rtb);
        }

        private void LogAppend(RichTextBox rtb, string msg, string tag = "")
        {
            var p = new Paragraph { Margin = new Thickness(0) };

            string timeStr = $"[{DateTime.Now:HH:mm:ss}]  ";
            var timeRun = new Run(timeStr)
            {
                Foreground = (SolidColorBrush)FindResource("MuteBrush")
            };
            p.Inlines.Add(timeRun);

            SolidColorBrush textColor = tag switch
            {
                "ok" => (SolidColorBrush)FindResource("OkBrush"),
                "err" => (SolidColorBrush)FindResource("ErrBrush"),
                "warn" => (SolidColorBrush)FindResource("WarnBrush"),
                "info" => (SolidColorBrush)FindResource("GlowBrush"),
                "dim" => (SolidColorBrush)FindResource("MuteBrush"),
                _ => (SolidColorBrush)FindResource("TextBrush")
            };

            var msgRun = new Run(msg)
            {
                Foreground = textColor
            };
            p.Inlines.Add(msgRun);

            rtb.Document.Blocks.Add(p);
            rtb.ScrollToEnd();
        }
        #endregion
    }
}

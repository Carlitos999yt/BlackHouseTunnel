using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using NepTunnel.Services;

namespace NepTunnel;

public class MainWindow : Window, IComponentConnector
{
	private string _studioPath = "";

	private UIElement? _currentView;

	private bool _isNavigating;

	private bool _isHostActive;

	private bool _isJoinActive;

	private readonly EchoServer _echoServer = new EchoServer();

	internal Grid RootMainGrid;

	internal Button WinMinBtn;

	internal Button WinMaxBtn;

	internal Button WinCloseBtn;

	internal Image BannerImage;

	internal Grid ViewContainer;

	internal TextBlock StatusLabel;

	internal Button LangBtn;

	internal Grid AlertOverlayGrid;

	internal TextBlock AlertTitleTxt;

	internal TextBlock AlertMessageTxt;

	internal Button AlertCancelBtn;

	internal Button AlertConfirmBtn;

	internal Grid ImageModalOverlay;

	internal Image ZoomedImageControl;

	private bool _contentLoaded;

	public MainWindow()
	{
		InitializeComponent();
		base.Loaded += MainWindow_Loaded;
		base.Closing += MainWindow_Closing;
	}

	private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
	{
		NepConfig nepConfig = ConfigManager.LoadConfig();
		if (!string.IsNullOrEmpty(nepConfig.Language) && (nepConfig.Language == "es" || nepConfig.Language == "pt" || nepConfig.Language == "en"))
		{
			LocalizationService.CurrentLanguage = nepConfig.Language;
		}
		else
		{
			string language = (LocalizationService.CurrentLanguage = LocalizationService.DetectDefaultSystemLanguage());
			nepConfig.Language = language;
			ConfigManager.SaveConfig(nepConfig);
		}
		UpdateLangBtnText();
		Task.Run(async delegate
		{
			BitmapImage bannerBmp = await BannerService.GetBannerImageAsync();
			if (bannerBmp != null)
			{
				base.Dispatcher.Invoke(() => BannerImage.Source = bannerBmp);
			}
		});
		Task.Run(delegate
		{
			RbxmBridgeServer.Start();
			string path = RobloxStudioService.GetStudioPath();
			base.Dispatcher.Invoke(delegate
			{
				OnStudioFound(path);
			});
		});
		ShowBootView();
	}

	private void LangBtn_Click(object sender, RoutedEventArgs e)
	{
		string currentLanguage = LocalizationService.CurrentLanguage;
		string currentLanguage2 = ((currentLanguage == "en") ? "es" : ((!(currentLanguage == "es")) ? "en" : "pt"));
		LocalizationService.CurrentLanguage = currentLanguage2;
		UpdateLangBtnText();
		NepConfig nepConfig = ConfigManager.LoadConfig();
		nepConfig.Language = LocalizationService.CurrentLanguage;
		ConfigManager.SaveConfig(nepConfig);
		if (!_isHostActive && !_isJoinActive)
		{
			ShowMainMenuView();
		}
	}

	private void UpdateLangBtnText()
	{
		Button langBtn = LangBtn;
		string currentLanguage = LocalizationService.CurrentLanguage;
		string content = ((currentLanguage == "es") ? "\ud83c\udf10 Español" : ((!(currentLanguage == "pt")) ? "\ud83c\udf10 English" : "\ud83c\udf10 Português"));
		langBtn.Content = content;
	}

	private void WinMinBtn_Click(object sender, RoutedEventArgs e)
	{
		SystemCommands.MinimizeWindow(this);
	}

	private void WinMaxBtn_Click(object sender, RoutedEventArgs e)
	{
		if (base.WindowState == WindowState.Maximized)
		{
			SystemCommands.RestoreWindow(this);
			WinMaxBtn.Content = "\ud83d\uddd6";
		}
		else
		{
			SystemCommands.MaximizeWindow(this);
			WinMaxBtn.Content = "\ud83d\uddd7";
		}
	}

	private void WinCloseBtn_Click(object sender, RoutedEventArgs e)
	{
		SystemCommands.CloseWindow(this);
	}

	private void MainWindow_Closing(object? sender, CancelEventArgs e)
	{
		if (_isHostActive || _isJoinActive)
		{
			e.Cancel = true;
			string message = (_isHostActive ? LocalizationService.Get("alert_stop_host_msg") : LocalizationService.Get("alert_disc_msg"));
			ShowConfirmationAlert(LocalizationService.Get("alert_stop_host_title"), message, LocalizationService.Get("alert_stop_host_btn"), delegate
			{
				_isHostActive = false;
				_isJoinActive = false;
				_echoServer.Stop();
				UdpProxy.StopProxy(wait: false);
				RbxmBridgeServer.Stop();
				RobloxStudioService.StopAllStudioProcesses();
				Application.Current.Shutdown();
			});
		}
		else
		{
			_echoServer.Stop();
			UdpProxy.StopProxy(wait: false);
			RbxmBridgeServer.Stop();
			RobloxStudioService.StopAllStudioProcesses();
		}
	}

	private void SetStatus(string msg, SolidColorBrush? color = null)
	{
		StatusLabel.Text = msg;
		StatusLabel.Foreground = color ?? ((SolidColorBrush)FindResource("MuteBrush"));
	}

	private void OnStudioFound(string path)
	{
		NepConfig nepConfig = ConfigManager.LoadConfig();
		if (!string.IsNullOrEmpty(nepConfig.Studio) && File.Exists(nepConfig.Studio))
		{
			_studioPath = nepConfig.Studio;
		}
		else
		{
			_studioPath = path;
			if (!string.IsNullOrEmpty(_studioPath))
			{
				nepConfig.Studio = _studioPath;
				ConfigManager.SaveConfig(nepConfig);
			}
		}
		UpdateStudioStatusText();
		ShowMainMenuView();
	}

	private void UpdateStudioStatusText()
	{
		if (_studioPath == "__VINEGAR__")
		{
			SetStatus("Studio found  ·  Vinegar (Flatpak) — Linux", (SolidColorBrush)FindResource("OkBrush"));
			return;
		}
		if (!string.IsNullOrEmpty(_studioPath))
		{
			string text = ((_studioPath.Length > 55) ? (_studioPath.Substring(0, 52) + "…") : _studioPath);
			SetStatus("Studio found  ·  " + text, (SolidColorBrush)FindResource("OkBrush"));
			return;
		}
		NepConfig nepConfig = ConfigManager.LoadConfig();
		if (!string.IsNullOrEmpty(nepConfig.Studio) && File.Exists(nepConfig.Studio))
		{
			_studioPath = nepConfig.Studio;
			string text2 = ((nepConfig.Studio.Length > 55) ? (nepConfig.Studio.Substring(0, 52) + "…") : nepConfig.Studio);
			SetStatus("Studio loaded from config  ·  " + text2, (SolidColorBrush)FindResource("OkBrush"));
		}
		else
		{
			string text3 = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" : "Linux"));
			SetStatus("Studio not found on " + text3 + " — use Browse to locate it", (SolidColorBrush)FindResource("ErrBrush"));
		}
	}

	private void ShowConfirmationAlert(string title, string message, string confirmBtnText, Action onConfirm)
	{
		AlertTitleTxt.Text = title;
		AlertMessageTxt.Text = message;
		AlertConfirmBtn.Content = confirmBtnText;
		AlertCancelBtn.Content = LocalizationService.Get("alert_cancel");
		RoutedEventHandler cancelHandler = null;
		RoutedEventHandler confirmHandler = null;
		cancelHandler = delegate
		{
			AlertCancelBtn.Click -= cancelHandler;
			AlertConfirmBtn.Click -= confirmHandler;
			AlertOverlayGrid.Visibility = Visibility.Collapsed;
		};
		confirmHandler = delegate
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

	private void ShowSuccessAlert(string title, string message)
	{
		AlertTitleTxt.Text = title;
		AlertMessageTxt.Text = message;
		AlertConfirmBtn.Content = "Aceptar";
		AlertCancelBtn.Visibility = Visibility.Collapsed;
		RoutedEventHandler confirmHandler = null;
		confirmHandler = delegate
		{
			AlertConfirmBtn.Click -= confirmHandler;
			AlertCancelBtn.Visibility = Visibility.Visible;
			AlertOverlayGrid.Visibility = Visibility.Collapsed;
		};
		AlertConfirmBtn.Click += confirmHandler;
		AlertOverlayGrid.Visibility = Visibility.Visible;
	}

	private void ShowStudioSelectorModal(TextBlock studioLbl)
	{
		List<RobloxStudioService.StudioInstallation> detectedStudioInstallations = RobloxStudioService.GetDetectedStudioInstallations();
		if (!string.IsNullOrEmpty(_studioPath) && File.Exists(_studioPath) && !detectedStudioInstallations.Any((RobloxStudioService.StudioInstallation i) => i.Path.Equals(_studioPath, StringComparison.OrdinalIgnoreCase)))
		{
			detectedStudioInstallations.Insert(0, new RobloxStudioService.StudioInstallation("Roblox Studio (Ruta Activa)", _studioPath, "RSM", IsRecommended: true));
		}
		Grid overlayGrid = new Grid
		{
			Background = new SolidColorBrush(Color.FromArgb(204, 10, 10, 20))
		};
		Border border = new Border
		{
			Width = 540.0,
			MaxHeight = 440.0,
			Background = (SolidColorBrush)FindResource("CardBrush"),
			BorderBrush = (SolidColorBrush)FindResource("AccBrush"),
			BorderThickness = new Thickness(1.5),
			CornerRadius = new CornerRadius(12.0),
			Padding = new Thickness(20.0),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("modal_studio_title"),
			FontSize = 16.0,
			FontWeight = FontWeights.Bold,
			Foreground = (SolidColorBrush)FindResource("TextBrush"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 0.0, 4.0)
		});
		stackPanel.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("modal_studio_sub"),
			FontSize = 12.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 0.0, 14.0)
		});
		StackPanel stackPanel2 = new StackPanel();
		if (detectedStudioInstallations.Count == 0)
		{
			stackPanel2.Children.Add(new TextBlock
			{
				Text = LocalizationService.Get("modal_studio_empty"),
				FontSize = 13.0,
				Foreground = (SolidColorBrush)FindResource("WarnBrush"),
				HorizontalAlignment = HorizontalAlignment.Center,
				Margin = new Thickness(0.0, 10.0, 0.0, 10.0)
			});
		}
		else
		{
			foreach (RobloxStudioService.StudioInstallation inst in detectedStudioInstallations)
			{
				bool flag = !string.IsNullOrEmpty(_studioPath) && inst.Path.Equals(_studioPath, StringComparison.OrdinalIgnoreCase);
				Border border2 = new Border
				{
					Background = (flag ? new SolidColorBrush(Color.FromArgb(53, 139, 92, 246)) : ((SolidColorBrush)FindResource("Card2Brush"))),
					BorderBrush = (flag ? ((SolidColorBrush)FindResource("AccBrush")) : ((SolidColorBrush)FindResource("BordBrush"))),
					BorderThickness = new Thickness((!flag) ? 1 : 2),
					CornerRadius = new CornerRadius(8.0),
					Padding = new Thickness(12.0, 10.0, 12.0, 10.0),
					Margin = new Thickness(0.0, 0.0, 0.0, 8.0),
					Cursor = Cursors.Hand
				};
				StackPanel stackPanel3 = new StackPanel();
				Grid grid = new Grid();
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = GridLength.Auto
				});
				TextBlock element = new TextBlock
				{
					Text = inst.Name,
					FontSize = 13.0,
					FontWeight = FontWeights.Bold,
					Foreground = (flag ? ((SolidColorBrush)FindResource("GlowBrush")) : ((SolidColorBrush)FindResource("TextBrush")))
				};
				Grid.SetColumn(element, 0);
				grid.Children.Add(element);
				StackPanel stackPanel4 = new StackPanel
				{
					Orientation = Orientation.Horizontal
				};
				if (flag)
				{
					Border border3 = new Border
					{
						Background = (SolidColorBrush)FindResource("AccBrush"),
						CornerRadius = new CornerRadius(4.0),
						Padding = new Thickness(6.0, 2.0, 6.0, 2.0),
						Margin = new Thickness(4.0, 0.0, 0.0, 0.0)
					};
					border3.Child = new TextBlock
					{
						Text = LocalizationService.Get("modal_studio_active"),
						FontSize = 10.0,
						FontWeight = FontWeights.Bold,
						Foreground = Brushes.White
					};
					stackPanel4.Children.Add(border3);
				}
				if (inst.IsRecommended)
				{
					Border border4 = new Border
					{
						Background = (flag ? new SolidColorBrush(Color.FromRgb(124, 58, 237)) : ((SolidColorBrush)FindResource("AccBrush"))),
						CornerRadius = new CornerRadius(4.0),
						Padding = new Thickness(6.0, 2.0, 6.0, 2.0),
						Margin = new Thickness(4.0, 0.0, 0.0, 0.0)
					};
					border4.Child = new TextBlock
					{
						Text = LocalizationService.Get("modal_studio_recommended"),
						FontSize = 10.0,
						FontWeight = FontWeights.Bold,
						Foreground = Brushes.White
					};
					stackPanel4.Children.Add(border4);
				}
				else if (inst.Type == "Bloxstrap")
				{
					Border border5 = new Border
					{
						Background = (SolidColorBrush)FindResource("TealBrush"),
						CornerRadius = new CornerRadius(4.0),
						Padding = new Thickness(6.0, 2.0, 6.0, 2.0),
						Margin = new Thickness(4.0, 0.0, 0.0, 0.0)
					};
					border5.Child = new TextBlock
					{
						Text = "BLOXSTRAP",
						FontSize = 10.0,
						FontWeight = FontWeights.Bold,
						Foreground = Brushes.White
					};
					stackPanel4.Children.Add(border5);
				}
				else if (inst.Type == "Oficial")
				{
					Border border6 = new Border
					{
						Background = (SolidColorBrush)FindResource("BlueBrush"),
						CornerRadius = new CornerRadius(4.0),
						Padding = new Thickness(6.0, 2.0, 6.0, 2.0),
						Margin = new Thickness(4.0, 0.0, 0.0, 0.0)
					};
					border6.Child = new TextBlock
					{
						Text = "OFICIAL",
						FontSize = 10.0,
						FontWeight = FontWeights.Bold,
						Foreground = Brushes.White
					};
					stackPanel4.Children.Add(border6);
				}
				Grid.SetColumn(stackPanel4, 1);
				grid.Children.Add(stackPanel4);
				stackPanel3.Children.Add(grid);
				stackPanel3.Children.Add(new TextBlock
				{
					Text = inst.Path,
					FontSize = 11.0,
					Foreground = (SolidColorBrush)FindResource("MuteBrush"),
					TextTrimming = TextTrimming.CharacterEllipsis,
					Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
				});
				border2.Child = stackPanel3;
				string selectedPath = inst.Path;
				border2.MouseLeftButtonDown += delegate
				{
					_studioPath = selectedPath;
					studioLbl.Text = _studioPath;
					studioLbl.Foreground = (SolidColorBrush)FindResource("GlowBrush");
					NepConfig nepConfig = ConfigManager.LoadConfig();
					nepConfig.Studio = _studioPath;
					ConfigManager.SaveConfig(nepConfig);
					SetStatus("Ruta Studio seleccionada: " + inst.Name, (SolidColorBrush)FindResource("OkBrush"));
					RootMainGrid.Children.Remove(overlayGrid);
				};
				stackPanel2.Children.Add(border2);
			}
		}
		ScrollViewer element2 = new ScrollViewer
		{
			MaxHeight = 250.0,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Content = stackPanel2,
			Margin = new Thickness(0.0, 0.0, 0.0, 14.0)
		};
		stackPanel.Children.Add(element2);
		Button button = new Button
		{
			Content = LocalizationService.Get("modal_studio_close"),
			Background = (SolidColorBrush)FindResource("Card2Brush"),
			Style = (Style)FindResource("NepButtonStyle"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Padding = new Thickness(16.0, 6.0, 16.0, 6.0)
		};
		button.Click += delegate
		{
			RootMainGrid.Children.Remove(overlayGrid);
		};
		stackPanel.Children.Add(button);
		border.Child = stackPanel;
		overlayGrid.Children.Add(border);
		Grid.SetRowSpan(overlayGrid, 10);
		Grid.SetColumnSpan(overlayGrid, 10);
		Panel.SetZIndex(overlayGrid, 9999);
		RootMainGrid.Children.Add(overlayGrid);
	}

	private void NavigateTo(UIElement newView, string direction = "left")
	{
		if (_isNavigating)
		{
			return;
		}
		if (_currentView == null)
		{
			ViewContainer.Children.Clear();
			ViewContainer.Children.Add(newView);
			_currentView = newView;
			return;
		}
		_isNavigating = true;
		double num = ((ViewContainer.ActualWidth > 0.0) ? ViewContainer.ActualWidth : 720.0);
		UIElement oldView = _currentView;
		ViewContainer.Children.Add(newView);
		TranslateTransform translateTransform = new TranslateTransform();
		TranslateTransform translateTransform2 = new TranslateTransform();
		oldView.RenderTransform = translateTransform;
		newView.RenderTransform = translateTransform2;
		double num2 = ((direction == "left") ? num : (0.0 - num));
		double toValue = ((direction == "left") ? (0.0 - num) : num);
		translateTransform2.X = num2;
		TimeSpan timeSpan = TimeSpan.FromMilliseconds(260.0);
		CubicEase easingFunction = new CubicEase
		{
			EasingMode = EasingMode.EaseOut
		};
		DoubleAnimation animation = new DoubleAnimation(0.0, toValue, timeSpan)
		{
			EasingFunction = easingFunction
		};
		DoubleAnimation doubleAnimation = new DoubleAnimation(num2, 0.0, timeSpan)
		{
			EasingFunction = easingFunction
		};
		doubleAnimation.Completed += delegate
		{
			ViewContainer.Children.Remove(oldView);
			_currentView = newView;
			_isNavigating = false;
		};
		translateTransform.BeginAnimation(TranslateTransform.XProperty, animation);
		translateTransform2.BeginAnimation(TranslateTransform.XProperty, doubleAnimation);
	}

	private void ShowBootView()
	{
		Grid grid = new Grid
		{
			Background = (SolidColorBrush)FindResource("BgBrush")
		};
		StackPanel stackPanel = new StackPanel
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		Canvas canvas = new Canvas
		{
			Width = 96.0,
			Height = 96.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 16.0)
		};
		double num = 48.0;
		double num2 = 48.0;
		double num3 = 30.0;
		for (int num4 = 4; num4 >= 1; num4--)
		{
			double num5 = num3 + (double)(num4 * 6);
			Ellipse element = new Ellipse
			{
				Width = num5 * 2.0,
				Height = num5 * 2.0,
				Stroke = (SolidColorBrush)FindResource("AccBrush"),
				StrokeThickness = 1.0,
				Opacity = 0.3 + (double)num4 * 0.1
			};
			Canvas.SetLeft(element, num - num5);
			Canvas.SetTop(element, num2 - num5);
			canvas.Children.Add(element);
		}
		Ellipse element2 = new Ellipse
		{
			Width = num3 * 2.0,
			Height = num3 * 2.0,
			Fill = (SolidColorBrush)FindResource("MoonBrush"),
			Stroke = (SolidColorBrush)FindResource("GlowBrush"),
			StrokeThickness = 1.0
		};
		Canvas.SetLeft(element2, num - num3);
		Canvas.SetTop(element2, num2 - num3);
		canvas.Children.Add(element2);
		double num6 = num3 * 0.55;
		double num7 = num3 * 1.07;
		Ellipse element3 = new Ellipse
		{
			Width = num7 * 2.0,
			Height = num7 * 2.0,
			Fill = (SolidColorBrush)FindResource("BgBrush")
		};
		Canvas.SetLeft(element3, num + num6 - num7);
		Canvas.SetTop(element3, num2 - num7);
		canvas.Children.Add(element3);
		stackPanel.Children.Add(canvas);
		TextBlock element4 = new TextBlock
		{
			Text = "NEP TUNNEL",
			FontSize = 22.0,
			FontWeight = FontWeights.Bold,
			Foreground = (SolidColorBrush)FindResource("TextBrush"),
			HorizontalAlignment = HorizontalAlignment.Center
		};
		stackPanel.Children.Add(element4);
		TextBlock element5 = new TextBlock
		{
			Text = "Locating Roblox Studio…",
			FontSize = 13.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 4.0, 0.0, 8.0)
		};
		stackPanel.Children.Add(element5);
		TextBlock element6 = new TextBlock
		{
			Text = "●  ○  ○",
			FontSize = 11.0,
			Foreground = (SolidColorBrush)FindResource("AccBrush"),
			HorizontalAlignment = HorizontalAlignment.Center
		};
		stackPanel.Children.Add(element6);
		grid.Children.Add(stackPanel);
		NavigateTo(grid);
	}

	private void ShowMainMenuView(string direction = "left")
	{
		_isHostActive = false;
		_isJoinActive = false;
		_echoServer.Stop();
		UdpProxy.StopProxy(wait: false);
		RobloxStudioService.StopAllStudioProcesses();
		UpdateStudioStatusText();
		NepConfig nepConfig = ConfigManager.LoadConfig();
		Grid grid = new Grid
		{
			Background = (SolidColorBrush)FindResource("BgBrush")
		};
		ScrollViewer scrollViewer = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(28.0, 20.0, 28.0, 20.0)
		};
		Border border = new Border
		{
			Background = (SolidColorBrush)FindResource("CardBrush"),
			BorderBrush = (SolidColorBrush)FindResource("BordBrush"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(12.0),
			Padding = new Thickness(20.0, 16.0, 20.0, 16.0),
			Margin = new Thickness(0.0, 0.0, 0.0, 16.0)
		};
		StackPanel stackPanel2 = new StackPanel();
		stackPanel2.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("main_title"),
			FontSize = 16.0,
			FontWeight = FontWeights.Bold,
			Foreground = (SolidColorBrush)FindResource("TextBrush"),
			HorizontalAlignment = HorizontalAlignment.Center
		});
		stackPanel2.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("main_subtitle"),
			FontSize = 13.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 2.0, 0.0, 16.0)
		});
		StackPanel stackPanel3 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 0.0, 10.0)
		};
		Button button = new Button
		{
			Content = IconFactory.CreateButtonContent("host", LocalizationService.Get("btn_host")),
			Background = (SolidColorBrush)FindResource("AccBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(8.0, 0.0, 8.0, 0.0)
		};
		button.Click += delegate
		{
			ShowHostConfigView();
		};
		stackPanel3.Children.Add(button);
		Button button2 = new Button
		{
			Content = IconFactory.CreateButtonContent("join", LocalizationService.Get("btn_join")),
			Background = (SolidColorBrush)FindResource("BlueBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(8.0, 0.0, 8.0, 0.0)
		};
		button2.Click += delegate
		{
			ShowJoinConfigView();
		};
		stackPanel3.Children.Add(button2);
		stackPanel2.Children.Add(stackPanel3);
		StackPanel stackPanel4 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 0.0, 4.0)
		};
		Button button3 = new Button
		{
			Content = IconFactory.CreateButtonContent("echo", LocalizationService.Get("btn_echo")),
			Background = (SolidColorBrush)FindResource("TealBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(8.0, 0.0, 8.0, 0.0)
		};
		button3.Click += delegate
		{
			ShowEchoTestView();
		};
		stackPanel4.Children.Add(button3);
		Button button4 = new Button
		{
			Content = IconFactory.CreateButtonContent("map", LocalizationService.Get("btn_rbxm")),
			Background = new SolidColorBrush(Color.FromRgb(124, 58, 237)),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(8.0, 0.0, 8.0, 0.0)
		};
		button4.Click += delegate
		{
			ShowRbxmImporterView();
		};
		stackPanel4.Children.Add(button4);
		Button button5 = new Button
		{
			Content = IconFactory.CreateButtonContent("folder", LocalizationService.Get("btn_rsm_assistant")),
			Background = new SolidColorBrush(Color.FromRgb(217, 70, 239)),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(8.0, 0.0, 8.0, 0.0)
		};
		button5.Click += delegate
		{
			ShowRsmAssistantView();
		};
		stackPanel4.Children.Add(button5);
		stackPanel2.Children.Add(stackPanel4);
		border.Child = stackPanel2;
		stackPanel.Children.Add(border);
		Border element = new Border
		{
			BorderBrush = (SolidColorBrush)FindResource("BordBrush"),
			BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0),
			Margin = new Thickness(0.0, 4.0, 0.0, 16.0)
		};
		stackPanel.Children.Add(element);
		StackPanel stackPanel5 = new StackPanel
		{
			Margin = new Thickness(4.0, 0.0, 4.0, 0.0)
		};
		Grid grid2 = new Grid
		{
			Margin = new Thickness(0.0, 4.0, 0.0, 4.0)
		};
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(170.0)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		TextBlock element2 = new TextBlock
		{
			Text = LocalizationService.Get("lbl_studio"),
			FontSize = 15.0,
			FontWeight = FontWeights.Bold,
			Foreground = (SolidColorBrush)FindResource("MuteBrush"),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(element2, 0);
		grid2.Children.Add(element2);
		TextBlock studioLbl = new TextBlock
		{
			Text = ((!string.IsNullOrEmpty(_studioPath)) ? _studioPath : "Not found"),
			FontSize = 14.0,
			TextTrimming = TextTrimming.CharacterEllipsis,
			Foreground = ((!string.IsNullOrEmpty(_studioPath)) ? ((SolidColorBrush)FindResource("GlowBrush")) : ((SolidColorBrush)FindResource("ErrBrush"))),
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 12.0, 0.0)
		};
		Grid.SetColumn(studioLbl, 1);
		grid2.Children.Add(studioLbl);
		StackPanel stackPanel6 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center
		};
		Button button6 = new Button
		{
			Content = IconFactory.CreateButtonContent("folder", LocalizationService.Get("browse"), 14.0),
			Background = (SolidColorBrush)FindResource("Card2Brush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Padding = new Thickness(10.0, 4.0, 10.0, 4.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		button6.Click += delegate
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Title = "Select RobloxStudioBeta.exe",
				Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*"
			};
			if (openFileDialog.ShowDialog() == true)
			{
				_studioPath = openFileDialog.FileName;
				studioLbl.Text = _studioPath;
				studioLbl.Foreground = (SolidColorBrush)FindResource("GlowBrush");
				NepConfig nepConfig2 = ConfigManager.LoadConfig();
				nepConfig2.Studio = _studioPath;
				ConfigManager.SaveConfig(nepConfig2);
				SetStatus("Studio set  ·  " + _studioPath, (SolidColorBrush)FindResource("OkBrush"));
			}
		};
		stackPanel6.Children.Add(button6);
		Button button7 = new Button
		{
			Content = "•••",
			Background = (SolidColorBrush)FindResource("Card2Brush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Padding = new Thickness(8.0, 4.0, 8.0, 4.0),
			Margin = new Thickness(6.0, 0.0, 0.0, 0.0),
			FontWeight = FontWeights.Bold,
			VerticalAlignment = VerticalAlignment.Center,
			ToolTip = "Instalaciones de Roblox Studio Detectadas"
		};
		button7.Click += delegate
		{
			ShowStudioSelectorModal(studioLbl);
		};
		stackPanel6.Children.Add(button7);
		Grid.SetColumn(stackPanel6, 2);
		grid2.Children.Add(stackPanel6);
		stackPanel5.Children.Add(grid2);
		string item = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" : "Linux"));
		(string, string)[] array = new(string, string)[6]
		{
			(LocalizationService.Get("lbl_username"), (!string.IsNullOrWhiteSpace(nepConfig.Username)) ? nepConfig.Username : "(Por defecto / Default)"),
			(LocalizationService.Get("lbl_tunnel_addr"), nepConfig.Addr),
			(LocalizationService.Get("lbl_server_port"), nepConfig.Port),
			(LocalizationService.Get("lbl_uid"), nepConfig.Uid),
			(LocalizationService.Get("lbl_proxy_port"), 55555.ToString()),
			(LocalizationService.Get("lbl_platform"), item)
		};
		for (int num = 0; num < array.Length; num++)
		{
			(string, string) tuple = array[num];
			string item2 = tuple.Item1;
			string item3 = tuple.Item2;
			Grid grid3 = new Grid
			{
				Margin = new Thickness(0.0, 4.0, 0.0, 4.0)
			};
			grid3.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(170.0)
			});
			grid3.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			TextBlock element3 = new TextBlock
			{
				Text = item2,
				FontSize = 15.0,
				FontWeight = FontWeights.Bold,
				Foreground = (SolidColorBrush)FindResource("MuteBrush"),
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(element3, 0);
			grid3.Children.Add(element3);
			TextBlock element4 = new TextBlock
			{
				Text = item3,
				FontSize = 14.0,
				Foreground = (SolidColorBrush)FindResource("GlowBrush"),
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(element4, 1);
			grid3.Children.Add(element4);
			stackPanel5.Children.Add(grid3);
		}
		Grid grid4 = new Grid
		{
			Margin = new Thickness(0.0, 4.0, 0.0, 4.0)
		};
		grid4.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(170.0)
		});
		grid4.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		TextBlock element5 = new TextBlock
		{
			Text = LocalizationService.Get("lbl_bridge"),
			FontSize = 15.0,
			FontWeight = FontWeights.Bold,
			Foreground = (SolidColorBrush)FindResource("MuteBrush"),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(element5, 0);
		grid4.Children.Add(element5);
		TextBlock element6 = new TextBlock
		{
			Text = (RbxmBridgeServer.IsRunning ? $"● port {7878}" : "✗ failed to start"),
			FontSize = 14.0,
			Foreground = (RbxmBridgeServer.IsRunning ? ((SolidColorBrush)FindResource("OkBrush")) : ((SolidColorBrush)FindResource("ErrBrush"))),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(element6, 1);
		grid4.Children.Add(element6);
		stackPanel5.Children.Add(grid4);
		stackPanel.Children.Add(stackPanel5);
		scrollViewer.Content = stackPanel;
		grid.Children.Add(scrollViewer);
		NavigateTo(grid, direction);
	}

	private void OpenImageModal(BitmapImage bitmap)
	{
		ZoomedImageControl.Source = bitmap;
		ImageModalOverlay.Visibility = Visibility.Visible;
	}

	private void CloseImageModal_Click(object sender, RoutedEventArgs e)
	{
		ImageModalOverlay.Visibility = Visibility.Collapsed;
	}

	private void ShowTutorialView()
	{
		Grid grid = new Grid
		{
			Background = (SolidColorBrush)FindResource("BgBrush")
		};
		ScrollViewer scrollViewer = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(24.0, 16.0, 24.0, 16.0)
		};
		stackPanel.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("tut_title"),
			FontSize = 18.0,
			FontWeight = FontWeights.Bold,
			Foreground = (SolidColorBrush)FindResource("TextBrush"),
			HorizontalAlignment = HorizontalAlignment.Center
		});
		stackPanel.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("tut_sub"),
			FontSize = 13.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 2.0, 0.0, 16.0)
		});
		for (int i = 1; i <= 9; i++)
		{
			string key = $"tut_s{i}_t";
			string key2 = $"tut_s{i}_d";
			Border border = new Border
			{
				Background = (SolidColorBrush)FindResource("CardBrush"),
				BorderBrush = (SolidColorBrush)FindResource("BordBrush"),
				BorderThickness = new Thickness(1.0),
				CornerRadius = new CornerRadius(10.0),
				Padding = new Thickness(16.0),
				Margin = new Thickness(0.0, 0.0, 0.0, 14.0)
			};
			StackPanel stackPanel2 = new StackPanel();
			stackPanel2.Children.Add(new TextBlock
			{
				Text = LocalizationService.Get(key),
				FontSize = 15.0,
				FontWeight = FontWeights.Bold,
				Foreground = (SolidColorBrush)FindResource("GlowBrush"),
				Margin = new Thickness(0.0, 0.0, 0.0, 4.0)
			});
			stackPanel2.Children.Add(new TextBlock
			{
				Text = LocalizationService.Get(key2),
				FontSize = 13.0,
				Foreground = (SolidColorBrush)FindResource("TextBrush"),
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0.0, 0.0, 0.0, 10.0)
			});
			BitmapImage bitmap = null;
			try
			{
				Uri uriSource = new Uri($"pack://application:,,,/bundled_assets/tut_{i}.png", UriKind.Absolute);
				bitmap = new BitmapImage();
				bitmap.BeginInit();
				bitmap.CacheOption = BitmapCacheOption.OnLoad;
				bitmap.UriSource = uriSource;
				bitmap.EndInit();
			}
			catch
			{
				bitmap = null;
			}
			if (bitmap == null)
			{
				string path = $"bundled_assets/tut_{i}.png";
				if (File.Exists(path))
				{
					try
					{
						Uri uriSource2 = new Uri(System.IO.Path.GetFullPath(path), UriKind.Absolute);
						bitmap = new BitmapImage();
						bitmap.BeginInit();
						bitmap.CacheOption = BitmapCacheOption.OnLoad;
						bitmap.UriSource = uriSource2;
						bitmap.EndInit();
					}
					catch
					{
						bitmap = null;
					}
				}
			}
			if (bitmap != null)
			{
				Image image = new Image
				{
					Source = bitmap,
					MaxHeight = 320.0,
					Stretch = Stretch.Uniform,
					HorizontalAlignment = HorizontalAlignment.Center,
					Cursor = Cursors.Hand,
					Margin = new Thickness(0.0, 4.0, 0.0, 4.0),
					ToolTip = "Click to enlarge / Haz clic para maximizar"
				};
				image.MouseDown += delegate
				{
					OpenImageModal(bitmap);
				};
				Border element = new Border
				{
					Background = (SolidColorBrush)FindResource("Card2Brush"),
					BorderBrush = (SolidColorBrush)FindResource("BordBrush"),
					BorderThickness = new Thickness(1.0),
					CornerRadius = new CornerRadius(6.0),
					Padding = new Thickness(4.0),
					Child = image
				};
				stackPanel2.Children.Add(element);
			}
			border.Child = stackPanel2;
			stackPanel.Children.Add(border);
		}
		Button button = new Button
		{
			Content = IconFactory.CreateButtonContent("back", LocalizationService.Get("back"), 14.0),
			Background = (SolidColorBrush)FindResource("Card2Brush"),
			Style = (Style)FindResource("NepButtonStyle"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 10.0, 0.0, 10.0)
		};
		button.Click += delegate
		{
			ShowHostConfigView();
		};
		stackPanel.Children.Add(button);
		scrollViewer.Content = stackPanel;
		grid.Children.Add(scrollViewer);
		NavigateTo(grid);
	}

	private void ShowRbxmImporterView()
	{
		NepConfig nepConfig = ConfigManager.LoadConfig();
		List<string> savedMaps = new List<string>(nepConfig.SavedMaps);
		Grid grid = new Grid
		{
			Background = (SolidColorBrush)FindResource("BgBrush")
		};
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(20.0, 16.0, 20.0, 16.0)
		};
		stackPanel.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("rbxm_title"),
			FontSize = 18.0,
			FontWeight = FontWeights.Bold,
			Foreground = (SolidColorBrush)FindResource("TextBrush"),
			HorizontalAlignment = HorizontalAlignment.Center
		});
		stackPanel.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("rbxm_sub"),
			FontSize = 13.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 2.0, 0.0, 12.0)
		});
		TextBlock mapStatusLbl = new TextBlock
		{
			Text = "",
			FontSize = 12.0,
			Foreground = (SolidColorBrush)FindResource("OkBrush"),
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 0.0, 4.0, 4.0)
		};
		stackPanel.Children.Add(mapStatusLbl);
		Border border = new Border
		{
			Background = (SolidColorBrush)FindResource("CardBrush"),
			BorderBrush = (SolidColorBrush)FindResource("BordBrush"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Height = 180.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 12.0)
		};
		ScrollViewer scrollViewer = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};
		StackPanel listStack = new StackPanel();
		Action refreshList = null;
		refreshList = delegate
		{
			listStack.Children.Clear();
			if (savedMaps.Count == 0)
			{
				listStack.Children.Add(new TextBlock
				{
					Text = LocalizationService.Get("rbxm_empty"),
					FontSize = 13.0,
					Foreground = (SolidColorBrush)FindResource("MuteBrush"),
					HorizontalAlignment = HorizontalAlignment.Center,
					Margin = new Thickness(0.0, 40.0, 0.0, 0.0)
				});
			}
			else
			{
				for (int i = 0; i < savedMaps.Count; i++)
				{
					int index = i;
					string p = savedMaps[i];
					bool flag = File.Exists(p);
					Grid grid2 = new Grid
					{
						Background = ((i % 2 == 0) ? ((SolidColorBrush)FindResource("Card2Brush")) : ((SolidColorBrush)FindResource("CardBrush"))),
						Margin = new Thickness(0.0, 1.0, 0.0, 1.0)
					};
					grid2.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = new GridLength(1.0, GridUnitType.Star)
					});
					grid2.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = GridLength.Auto
					});
					StackPanel stackPanel4 = new StackPanel
					{
						Margin = new Thickness(10.0, 6.0, 10.0, 6.0)
					};
					stackPanel4.Children.Add(new TextBlock
					{
						Text = System.IO.Path.GetFileName(p),
						FontSize = 13.0,
						FontWeight = FontWeights.Bold,
						Foreground = (flag ? ((SolidColorBrush)FindResource("TextBrush")) : ((SolidColorBrush)FindResource("MuteBrush")))
					});
					string text = ((p.Length > 65) ? (p.Substring(0, 62) + "…") : p);
					stackPanel4.Children.Add(new TextBlock
					{
						Text = (flag ? text : ("⚠ missing " + text)),
						FontSize = 11.0,
						Foreground = (flag ? ((SolidColorBrush)FindResource("MuteBrush")) : ((SolidColorBrush)FindResource("ErrBrush")))
					});
					Grid.SetColumn(stackPanel4, 0);
					grid2.Children.Add(stackPanel4);
					StackPanel stackPanel5 = new StackPanel
					{
						Orientation = Orientation.Horizontal,
						Margin = new Thickness(8.0, 4.0, 8.0, 4.0)
					};
					Button button3 = new Button
					{
						Content = IconFactory.CreateButtonContent("send", LocalizationService.Get("btn_send_studio"), 14.0),
						Background = (SolidColorBrush)FindResource("AccBrush"),
						Style = (Style)FindResource("NepButtonStyle"),
						Padding = new Thickness(10.0, 4.0, 10.0, 4.0),
						IsEnabled = flag,
						Margin = new Thickness(0.0, 0.0, 6.0, 0.0)
					};
					button3.Click += delegate
					{
						var (flag2, text2) = RbxmBridgeServer.QueueRbxm(p);
						if (flag2)
						{
							mapStatusLbl.Text = "✓ \"" + text2 + "\" queued — click ▶ Listen in Studio plugin";
							mapStatusLbl.Foreground = (SolidColorBrush)FindResource("OkBrush");
							SetStatus("Map queued: " + text2, (SolidColorBrush)FindResource("OkBrush"));
						}
						else
						{
							mapStatusLbl.Text = "✗ " + text2;
							mapStatusLbl.Foreground = (SolidColorBrush)FindResource("ErrBrush");
						}
					};
					stackPanel5.Children.Add(button3);
					Button button4 = new Button
					{
						Content = IconFactory.CreateButtonContent("trash", "", 14.0),
						Background = (SolidColorBrush)FindResource("CardBrush"),
						Style = (Style)FindResource("NepButtonStyle"),
						Padding = new Thickness(8.0, 4.0, 8.0, 4.0)
					};
					button4.Click += delegate
					{
						savedMaps.RemoveAt(index);
						NepConfig nepConfig2 = ConfigManager.LoadConfig();
						nepConfig2.SavedMaps = savedMaps;
						ConfigManager.SaveConfig(nepConfig2);
						mapStatusLbl.Text = "Removed.";
						mapStatusLbl.Foreground = (SolidColorBrush)FindResource("MuteBrush");
						refreshList();
					};
					stackPanel5.Children.Add(button4);
					Grid.SetColumn(stackPanel5, 1);
					grid2.Children.Add(stackPanel5);
					listStack.Children.Add(grid2);
				}
			}
		};
		refreshList();
		scrollViewer.Content = listStack;
		border.Child = scrollViewer;
		stackPanel.Children.Add(border);
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 0.0, 12.0)
		};
		Button button = new Button
		{
			Content = IconFactory.CreateButtonContent("back", LocalizationService.Get("back"), 14.0),
			Background = (SolidColorBrush)FindResource("CardBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(6.0, 0.0, 6.0, 0.0)
		};
		button.Click += delegate
		{
			ShowMainMenuView("right");
		};
		stackPanel2.Children.Add(button);
		Button button2 = new Button
		{
			Content = IconFactory.CreateButtonContent("folder", LocalizationService.Get("btn_add_rbxm"), 14.0),
			Background = new SolidColorBrush(Color.FromRgb(124, 58, 237)),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(6.0, 0.0, 6.0, 0.0)
		};
		button2.Click += delegate
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Title = "Select .rbxm map file(s)",
				Filter = "Roblox Model (*.rbxm;*.rbxmx)|*.rbxm;*.rbxmx|All files (*.*)|*.*",
				Multiselect = true
			};
			if (openFileDialog.ShowDialog() == true)
			{
				int num = 0;
				string[] fileNames = openFileDialog.FileNames;
				for (int i = 0; i < fileNames.Length; i++)
				{
					string fullPath = System.IO.Path.GetFullPath(fileNames[i]);
					if (!savedMaps.Contains(fullPath))
					{
						savedMaps.Add(fullPath);
						num++;
					}
				}
				if (num > 0)
				{
					NepConfig nepConfig2 = ConfigManager.LoadConfig();
					nepConfig2.SavedMaps = savedMaps;
					ConfigManager.SaveConfig(nepConfig2);
					mapStatusLbl.Text = $"Added {num} map(s).";
					mapStatusLbl.Foreground = (SolidColorBrush)FindResource("OkBrush");
					refreshList();
				}
			}
		};
		stackPanel2.Children.Add(button2);
		stackPanel.Children.Add(stackPanel2);
		Border border2 = new Border
		{
			Background = (SolidColorBrush)FindResource("Card2Brush"),
			BorderBrush = (SolidColorBrush)FindResource("BordBrush"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(12.0)
		};
		StackPanel stackPanel3 = new StackPanel();
		stackPanel3.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("rbxm_how_works_title"),
			FontSize = 12.0,
			FontWeight = FontWeights.Bold,
			Foreground = (SolidColorBrush)FindResource("GlowBrush"),
			Margin = new Thickness(0.0, 0.0, 0.0, 4.0)
		});
		stackPanel3.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("rbxm_how_works_1"),
			FontSize = 11.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush")
		});
		stackPanel3.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("rbxm_how_works_2"),
			FontSize = 11.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush")
		});
		stackPanel3.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("rbxm_how_works_3"),
			FontSize = 11.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush")
		});
		border2.Child = stackPanel3;
		stackPanel.Children.Add(border2);
		grid.Children.Add(stackPanel);
		NavigateTo(grid);
	}

	private void ShowRsmAssistantView()
	{
		Grid grid = new Grid
		{
			Background = (SolidColorBrush)FindResource("BgBrush")
		};
		ScrollViewer scrollViewer = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(24.0, 12.0, 24.0, 12.0)
		};
		stackPanel.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("rsm_title"),
			FontSize = 18.0,
			FontWeight = FontWeights.Bold,
			Foreground = (SolidColorBrush)FindResource("TextBrush"),
			HorizontalAlignment = HorizontalAlignment.Center
		});
		stackPanel.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("rsm_sub"),
			FontSize = 13.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 2.0, 0.0, 12.0)
		});
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		string text = System.IO.Path.Combine(localAppData, "Roblox Studio", "RobloxStudioBeta.exe");
		string rsmFolder = System.IO.Path.Combine(localAppData, "Roblox Studio");
		string rsmManagerFolder = System.IO.Path.Combine(localAppData, "Roblox Studio Mod Manager");
		bool flag = File.Exists(text);
		Border border = new Border
		{
			Background = (SolidColorBrush)FindResource("CardBrush"),
			BorderBrush = (SolidColorBrush)FindResource("BordBrush"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(10.0),
			Padding = new Thickness(14.0),
			Margin = new Thickness(0.0, 0.0, 0.0, 12.0)
		};
		StackPanel stackPanel2 = new StackPanel();
		TextBlock element = new TextBlock
		{
			Text = (flag ? LocalizationService.Get("rsm_status_installed") : LocalizationService.Get("rsm_status_not_installed")),
			FontSize = 14.0,
			FontWeight = FontWeights.Bold,
			Foreground = (flag ? ((SolidColorBrush)FindResource("OkBrush")) : ((SolidColorBrush)FindResource("MuteBrush")))
		};
		stackPanel2.Children.Add(element);
		if (flag)
		{
			stackPanel2.Children.Add(new TextBlock
			{
				Text = text,
				FontSize = 12.0,
				Foreground = (SolidColorBrush)FindResource("GlowBrush"),
				TextTrimming = TextTrimming.CharacterEllipsis
			});
		}
		border.Child = stackPanel2;
		stackPanel.Children.Add(border);
		TextBlock errNoticeLbl = new TextBlock
		{
			Text = "",
			FontSize = 12.0,
			Foreground = (SolidColorBrush)FindResource("ErrBrush"),
			TextWrapping = TextWrapping.Wrap,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 0.0, 10.0)
		};
		stackPanel.Children.Add(errNoticeLbl);
		Border border2 = new Border
		{
			Background = (SolidColorBrush)FindResource("CardBrush"),
			BorderBrush = (SolidColorBrush)FindResource("BordBrush"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(10.0),
			Padding = new Thickness(10.0),
			Margin = new Thickness(0.0, 0.0, 0.0, 12.0)
		};
		UniformGrid uniformGrid = new UniformGrid
		{
			Columns = 2,
			Rows = 2
		};
		ProgressBar element2 = new ProgressBar
		{
			Height = 6.0,
			Minimum = 0.0,
			Maximum = 1.0,
			Value = 0.0,
			Visibility = Visibility.Collapsed,
			Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
		};
		stackPanel.Children.Add(element2);
		var (logBorder, logBox) = CreateLogBox(120.0);
		logBorder.Visibility = Visibility.Collapsed;
		logBorder.Margin = new Thickness(0.0, 0.0, 0.0, 10.0);
		stackPanel.Children.Add(logBorder);
		Button installBtn = new Button
		{
			Content = IconFactory.CreateButtonContent("folder", LocalizationService.Get("btn_rsm_install"), 14.0),
			Background = (SolidColorBrush)FindResource("AccBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(4.0)
		};
		installBtn.Click += delegate
		{
			errNoticeLbl.Text = "";
			installBtn.IsEnabled = false;
			logBorder.Visibility = Visibility.Visible;
			Task.Run(async delegate
			{
				try
				{
					bool success = await RsmInstallerService.LaunchOfficialRsmBootstrapperAsync(delegate(string msg, string tag)
					{
						base.Dispatcher.Invoke(delegate
						{
							LogAppend(logBox, msg, tag);
						});
					});
					base.Dispatcher.Invoke(delegate
					{
						installBtn.IsEnabled = true;
						if (success)
						{
							string rsmStudioExePath = RsmInstallerService.GetRsmStudioExePath();
							if (File.Exists(rsmStudioExePath))
							{
								_studioPath = rsmStudioExePath;
								NepConfig nepConfig = ConfigManager.LoadConfig();
								nepConfig.Studio = _studioPath;
								ConfigManager.SaveConfig(nepConfig);
							}
							SetStatus("RSM instalado y seleccionado como activo", (SolidColorBrush)FindResource("OkBrush"));
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
					Exception ex2 = ex;
					Exception ex3 = ex2;
					base.Dispatcher.Invoke(delegate
					{
						installBtn.IsEnabled = true;
						errNoticeLbl.Text = LocalizationService.Get("rsm_error_notice");
						LogAppend(logBox, "Exception: " + ex3.Message, "err");
						SetStatus("Installation error occurred", (SolidColorBrush)FindResource("ErrBrush"));
					});
				}
			});
		};
		uniformGrid.Children.Add(installBtn);
		Button repairBtn = new Button
		{
			Content = IconFactory.CreateButtonContent("test", LocalizationService.Get("btn_rsm_repair"), 14.0),
			Background = (SolidColorBrush)FindResource("WarnBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(4.0)
		};
		repairBtn.Click += delegate
		{
			errNoticeLbl.Text = "";
			repairBtn.IsEnabled = false;
			logBorder.Visibility = Visibility.Visible;
			Task.Run(async delegate
			{
				try
				{
					await RsmInstallerService.RepairFromGitHubRepoAsync(delegate(string msg, string tag)
					{
						base.Dispatcher.Invoke(delegate
						{
							LogAppend(logBox, msg, tag);
						});
					}, delegate
					{
					});
					base.Dispatcher.Invoke(delegate
					{
						repairBtn.IsEnabled = true;
						string rsmStudioExePath = RsmInstallerService.GetRsmStudioExePath();
						if (File.Exists(rsmStudioExePath))
						{
							_studioPath = rsmStudioExePath;
						}
						else
						{
							_studioPath = RobloxStudioService.GetStudioPath();
						}
						NepConfig nepConfig = ConfigManager.LoadConfig();
						nepConfig.Studio = _studioPath;
						ConfigManager.SaveConfig(nepConfig);
						SetStatus("Reparación RSM completada y seleccionada como activa.", (SolidColorBrush)FindResource("OkBrush"));
						ShowRsmAssistantView();
					});
				}
				catch (Exception ex)
				{
					Exception ex2 = ex;
					Exception ex3 = ex2;
					base.Dispatcher.Invoke(delegate
					{
						repairBtn.IsEnabled = true;
						errNoticeLbl.Text = LocalizationService.Get("rsm_error_notice");
						LogAppend(logBox, "Repair error: " + ex3.Message, "err");
						SetStatus("Repair error occurred", (SolidColorBrush)FindResource("ErrBrush"));
					});
				}
			});
		};
		uniformGrid.Children.Add(repairBtn);
		Button button = new Button
		{
			Content = IconFactory.CreateButtonContent("folder", LocalizationService.Get("btn_rsm_open_folder"), 14.0),
			Background = (SolidColorBrush)FindResource("BlueBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(4.0)
		};
		button.Click += delegate
		{
			try
			{
				string arguments = (Directory.Exists(rsmFolder) ? rsmFolder : localAppData);
				Process.Start("explorer.exe", arguments);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not open folder: " + ex.Message, "Folder Error", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		};
		uniformGrid.Children.Add(button);
		Button button2 = new Button
		{
			Content = IconFactory.CreateButtonContent("trash", LocalizationService.Get("btn_rsm_delete"), 14.0),
			Background = (SolidColorBrush)FindResource("ErrBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(4.0)
		};
		button2.Click += delegate
		{
			ShowConfirmationAlert(LocalizationService.Get("rsm_alert_delete_title"), LocalizationService.Get("rsm_alert_delete_msg"), LocalizationService.Get("btn_rsm_delete"), delegate
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
					MessageBox.Show("Error al eliminar RSM: " + ex.Message, "Delete Error", MessageBoxButton.OK, MessageBoxImage.Hand);
				}
			});
		};
		uniformGrid.Children.Add(button2);
		border2.Child = uniformGrid;
		stackPanel.Children.Add(border2);
		Button button3 = new Button
		{
			Content = IconFactory.CreateButtonContent("back", LocalizationService.Get("back"), 14.0),
			Background = (SolidColorBrush)FindResource("CardBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 8.0, 0.0, 16.0)
		};
		button3.Click += delegate
		{
			ShowMainMenuView("right");
		};
		stackPanel.Children.Add(button3);
		scrollViewer.Content = stackPanel;
		grid.Children.Add(scrollViewer);
		NavigateTo(grid);
	}

	private static void ForceDeleteDirectory(string targetDir)
	{
		if (!Directory.Exists(targetDir))
		{
			return;
		}
		try
		{
			foreach (Process item in from p in Process.GetProcesses()
				where p.ProcessName.Contains("RobloxStudio", StringComparison.OrdinalIgnoreCase)
				select p)
			{
				try
				{
					item.Kill();
					item.WaitForExit(1000);
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		try
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(targetDir);
			FileInfo[] files = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
			foreach (FileInfo fileInfo in files)
			{
				try
				{
					if (fileInfo.IsReadOnly)
					{
						fileInfo.IsReadOnly = false;
					}
					fileInfo.Attributes = FileAttributes.Normal;
				}
				catch
				{
				}
			}
			DirectoryInfo[] directories = directoryInfo.GetDirectories("*", SearchOption.AllDirectories);
			foreach (DirectoryInfo directoryInfo2 in directories)
			{
				try
				{
					directoryInfo2.Attributes = FileAttributes.Normal;
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		for (int num2 = 1; num2 <= 3; num2++)
		{
			try
			{
				Directory.Delete(targetDir, recursive: true);
				break;
			}
			catch
			{
				if (num2 == 3)
				{
					throw;
				}
				Thread.Sleep(300);
			}
		}
	}

	private void ShowEchoTestView()
	{
		NepConfig nepConfig = ConfigManager.LoadConfig();
		Grid grid = new Grid
		{
			Background = (SolidColorBrush)FindResource("BgBrush")
		};
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(18.0, 12.0, 18.0, 12.0)
		};
		stackPanel.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("echo_title"),
			FontSize = 18.0,
			FontWeight = FontWeights.Bold,
			Foreground = (SolidColorBrush)FindResource("TextBrush"),
			HorizontalAlignment = HorizontalAlignment.Center
		});
		stackPanel.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("echo_sub"),
			FontSize = 13.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 2.0, 0.0, 8.0)
		});
		Grid grid2 = new Grid
		{
			Margin = new Thickness(6.0, 0.0, 6.0, 8.0)
		};
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(140.0)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		StackPanel stackPanel2 = new StackPanel
		{
			Margin = new Thickness(0.0, 0.0, 8.0, 0.0)
		};
		stackPanel2.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("lbl_studio_port_host"),
			FontSize = 11.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush")
		});
		TextBox portTb = new TextBox
		{
			Text = nepConfig.Port,
			Style = (Style)FindResource("NepTextBoxStyle"),
			Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
		};
		stackPanel2.Children.Add(portTb);
		Grid.SetColumn(stackPanel2, 0);
		grid2.Children.Add(stackPanel2);
		StackPanel stackPanel3 = new StackPanel
		{
			Margin = new Thickness(8.0, 0.0, 0.0, 0.0)
		};
		stackPanel3.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("lbl_tunnel_addr_joiner"),
			FontSize = 11.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush")
		});
		TextBox addrTb = new TextBox
		{
			Text = nepConfig.Addr,
			Style = (Style)FindResource("NepTextBoxStyle"),
			Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
		};
		stackPanel3.Children.Add(addrTb);
		Grid.SetColumn(stackPanel3, 1);
		grid2.Children.Add(stackPanel3);
		stackPanel.Children.Add(grid2);
		(Border Border, RichTextBox RichText) logBox = CreateLogBox();
		stackPanel.Children.Add(logBox.Border);
		StackPanel stackPanel4 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 10.0, 0.0, 10.0)
		};
		Button button = new Button
		{
			Content = IconFactory.CreateButtonContent("back", LocalizationService.Get("back"), 14.0),
			Background = (SolidColorBrush)FindResource("CardBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(6.0, 0.0, 6.0, 0.0)
		};
		button.Click += delegate
		{
			_echoServer.Stop();
			ShowMainMenuView("right");
		};
		stackPanel4.Children.Add(button);
		Button echoHostBtn = new Button
		{
			Content = IconFactory.CreateButtonContent("echo", LocalizationService.Get("btn_host_start_echo"), 16.0),
			Background = (SolidColorBrush)FindResource("TealBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(6.0, 0.0, 6.0, 0.0)
		};
		echoHostBtn.Click += delegate
		{
			int result;
			if (_echoServer.IsRunning)
			{
				_echoServer.Stop();
				echoHostBtn.Content = IconFactory.CreateButtonContent("echo", LocalizationService.Get("btn_host_start_echo"), 16.0);
				echoHostBtn.Background = (SolidColorBrush)FindResource("TealBrush");
				LogAppend(logBox.RichText, $"Echo server stopped ({_echoServer.EchoedCount} total echoed)", "dim");
				SetStatus("Echo server stopped", (SolidColorBrush)FindResource("MuteBrush"));
			}
			else if (!int.TryParse(portTb.Text.Trim(), out result))
			{
				LogAppend(logBox.RichText, "Port must be a number", "err");
			}
			else if (_echoServer.Start(result, delegate(string m, string t)
			{
				LogAppend(logBox.RichText, m, t);
			}))
			{
				echoHostBtn.Content = IconFactory.CreateButtonContent("stop", LocalizationService.Get("btn_host_stop_echo"), 16.0);
				echoHostBtn.Background = (SolidColorBrush)FindResource("ErrBrush");
				LogAppend(logBox.RichText, $"✓ Echo server ACTIVE on 0.0.0.0:{result}", "ok");
				LogAppend(logBox.RichText, "Waiting for joiner to send probe packets...", "warn");
				SetStatus($"Echo server listening on port {result}", (SolidColorBrush)FindResource("OkBrush"));
			}
		};
		stackPanel4.Children.Add(echoHostBtn);
		Button button2 = new Button
		{
			Content = IconFactory.CreateButtonContent("echo", LocalizationService.Get("btn_join_run_echo"), 16.0),
			Background = (SolidColorBrush)FindResource("BlueBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(6.0, 0.0, 6.0, 0.0)
		};
		button2.Click += delegate
		{
			string text2 = addrTb.Text.Trim();
			if (string.IsNullOrEmpty(text2) || !text2.Contains(':'))
			{
				LogAppend(logBox.RichText, "Enter a tunnel address (host:port)", "err");
			}
			else
			{
				string[] parts = text2.Split(':', 2);
				if (!int.TryParse(parts[1], out var rp))
				{
					LogAppend(logBox.RichText, "Invalid tunnel port", "err");
				}
				else
				{
					Task.Run(() => EchoClient.RunEchoTestAsync(delegate(string m, string t)
					{
						base.Dispatcher.Invoke(delegate
						{
							LogAppend(logBox.RichText, m, t);
						});
					}, parts[0], rp));
				}
			}
		};
		stackPanel4.Children.Add(button2);
		stackPanel.Children.Add(stackPanel4);
		string[] array = LocalizationService.Get("echo_how_to_use").Split('\n');
		foreach (string text in array)
		{
			string tag = ((text.StartsWith("  HOST") || text.StartsWith("  UNIRSE") || text.StartsWith("  JOINER") || text.StartsWith("  ANFITRIÃO")) ? "ok" : ((text.StartsWith("CÓMO") || text.StartsWith("HOW") || text.StartsWith("COMO")) ? "info" : "dim"));
			LogAppend(logBox.RichText, text, tag);
		}
		LogAppend(logBox.RichText, "───────────────────────────────────────", "dim");
		grid.Children.Add(stackPanel);
		NavigateTo(grid);
	}

	private void ShowHostConfigView()
	{
		NepConfig cfg = ConfigManager.LoadConfig();
		Grid grid = new Grid
		{
			Background = (SolidColorBrush)FindResource("BgBrush")
		};
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(24.0, 12.0, 24.0, 12.0)
		};
		stackPanel.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("host_title"),
			FontSize = 18.0,
			FontWeight = FontWeights.Bold,
			Foreground = (SolidColorBrush)FindResource("TextBrush"),
			HorizontalAlignment = HorizontalAlignment.Center
		});
		stackPanel.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("host_sub"),
			FontSize = 13.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 2.0, 0.0, 12.0)
		});
		Border border = new Border
		{
			Background = (SolidColorBrush)FindResource("CardBrush"),
			BorderBrush = (SolidColorBrush)FindResource("BordBrush"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(12.0),
			Padding = new Thickness(16.0)
		};
		Grid grid2 = new Grid();
		grid2.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid2.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid2.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid2.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid2.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(140.0)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		TextBlock element = new TextBlock
		{
			Text = LocalizationService.Get("lbl_uid"),
			FontSize = 14.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush"),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetRow(element, 0);
		Grid.SetColumn(element, 0);
		grid2.Children.Add(element);
		TextBox uidTb = new TextBox
		{
			Text = cfg.Uid,
			Style = (Style)FindResource("NepTextBoxStyle"),
			Margin = new Thickness(0.0, 3.0, 0.0, 3.0)
		};
		Grid.SetRow(uidTb, 0);
		Grid.SetColumn(uidTb, 1);
		grid2.Children.Add(uidTb);
		TextBlock element2 = new TextBlock
		{
			Text = LocalizationService.Get("lbl_username"),
			FontSize = 14.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush"),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetRow(element2, 1);
		Grid.SetColumn(element2, 0);
		grid2.Children.Add(element2);
		TextBox userTb = new TextBox
		{
			Text = cfg.Username,
			Style = (Style)FindResource("NepTextBoxStyle"),
			Margin = new Thickness(0.0, 3.0, 0.0, 3.0)
		};
		Grid.SetRow(userTb, 1);
		Grid.SetColumn(userTb, 1);
		grid2.Children.Add(userTb);
		TextBlock element3 = new TextBlock
		{
			Text = LocalizationService.Get("lbl_server_port"),
			FontSize = 14.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush"),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetRow(element3, 2);
		Grid.SetColumn(element3, 0);
		grid2.Children.Add(element3);
		TextBox portTb = new TextBox
		{
			Text = cfg.Port,
			Style = (Style)FindResource("NepTextBoxStyle"),
			Margin = new Thickness(0.0, 3.0, 0.0, 3.0)
		};
		Grid.SetRow(portTb, 2);
		Grid.SetColumn(portTb, 1);
		grid2.Children.Add(portTb);
		TextBlock element4 = new TextBlock
		{
			Text = LocalizationService.Get("lbl_tunnel_addr"),
			FontSize = 14.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush"),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetRow(element4, 3);
		Grid.SetColumn(element4, 0);
		grid2.Children.Add(element4);
		TextBox addrTb = new TextBox
		{
			Text = cfg.HostAddr,
			Style = (Style)FindResource("NepTextBoxStyle"),
			Margin = new Thickness(0.0, 3.0, 0.0, 3.0)
		};
		Grid.SetRow(addrTb, 3);
		Grid.SetColumn(addrTb, 1);
		grid2.Children.Add(addrTb);
		TextBlock element5 = new TextBlock
		{
			Text = LocalizationService.Get("lbl_map_file"),
			FontSize = 14.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush"),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetRow(element5, 4);
		Grid.SetColumn(element5, 0);
		grid2.Children.Add(element5);
		Grid grid3 = new Grid();
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		TextBox mapTb = new TextBox
		{
			Text = cfg.Map,
			Style = (Style)FindResource("NepTextBoxStyle"),
			Margin = new Thickness(0.0, 3.0, 6.0, 3.0)
		};
		Grid.SetColumn(mapTb, 0);
		grid3.Children.Add(mapTb);
		Button button = new Button
		{
			Content = IconFactory.CreateButtonContent("folder", LocalizationService.Get("browse"), 14.0),
			Background = (SolidColorBrush)FindResource("Card2Brush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Padding = new Thickness(10.0, 4.0, 10.0, 4.0)
		};
		button.Click += delegate
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Title = "Select Roblox Map",
				Filter = "Roblox Place (*.rbxl;*.rbxlx)|*.rbxl;*.rbxlx|All files (*.*)|*.*"
			};
			if (openFileDialog.ShowDialog() == true)
			{
				mapTb.Text = openFileDialog.FileName;
			}
		};
		Grid.SetColumn(button, 1);
		grid3.Children.Add(button);
		Grid.SetRow(grid3, 4);
		Grid.SetColumn(grid3, 1);
		grid2.Children.Add(grid3);
		border.Child = grid2;
		stackPanel.Children.Add(border);
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 16.0, 0.0, 0.0)
		};
		Button button2 = new Button
		{
			Content = IconFactory.CreateButtonContent("back", LocalizationService.Get("back"), 14.0),
			Background = (SolidColorBrush)FindResource("CardBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(6.0, 0.0, 6.0, 0.0)
		};
		button2.Click += delegate
		{
			ShowMainMenuView("right");
		};
		stackPanel2.Children.Add(button2);
		Button button3 = new Button
		{
			Content = IconFactory.CreateButtonContent("test", LocalizationService.Get("btn_tutorial"), 14.0),
			Background = (SolidColorBrush)FindResource("Card2Brush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(6.0, 0.0, 6.0, 0.0)
		};
		button3.Click += delegate
		{
			ShowTutorialView();
		};
		stackPanel2.Children.Add(button3);
		Button button4 = new Button
		{
			Content = IconFactory.CreateButtonContent("file-code", "Importar / Actualizar Scripts", 14.0),
			Background = (SolidColorBrush)FindResource("Card2Brush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(6.0, 0.0, 6.0, 0.0)
		};
		button4.Click += delegate
		{
			try
			{
				RbxmBridgeServer.ForceScriptImport = true;
				RbxmBridgeServer.ScriptsImported = true;
				PluginInstaller.EnsurePluginInstalled(out string _);
				ShowSuccessAlert("✓ Importación de Scripts", "✓ Scripts importados/actualizados correctamente en Roblox Studio.\n\nLos nuevos scripts oficiales del tabulador han sido insertados.");
			}
			catch (Exception ex)
			{
				ShowSuccessAlert("✗ Error de Importación", "No se pudieron importar los scripts: " + ex.Message);
			}
		};
		stackPanel2.Children.Add(button4);
		Button button5 = new Button
		{
			Content = IconFactory.CreateButtonContent("play", LocalizationService.Get("btn_launch_server"), 16.0),
			Background = (SolidColorBrush)FindResource("AccBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(6.0, 0.0, 6.0, 0.0)
		};
		button5.Click += delegate
		{
			string text = uidTb.Text.Trim();
			string text2 = portTb.Text.Trim();
			string text3 = addrTb.Text.Trim();
			string text4 = mapTb.Text.Trim();
			string text5 = userTb.Text.Trim();
			int result;
			if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text2) || string.IsNullOrEmpty(text3))
			{
				MessageBox.Show("All fields are required.", "Missing Fields", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
			else if (!int.TryParse(text2, out result))
			{
				MessageBox.Show("Port must be a number.", "Invalid Port", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
			else
			{
				if (string.IsNullOrEmpty(_studioPath) || !File.Exists(_studioPath))
				{
					_studioPath = RobloxStudioService.GetStudioPath();
					NepConfig nepConfig = ConfigManager.LoadConfig();
					nepConfig.Studio = _studioPath;
					ConfigManager.SaveConfig(nepConfig);
				}
				if (string.IsNullOrEmpty(_studioPath) || !File.Exists(_studioPath))
				{
					string text6 = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" : "Linux"));
					MessageBox.Show("Roblox Studio was not found on " + text6 + ".\nPlease ensure Roblox Studio is installed.", "Studio Not Found", MessageBoxButton.OK, MessageBoxImage.Hand);
				}
				else
				{
					cfg.Uid = text;
					cfg.Username = text5;
					cfg.Port = text2;
					cfg.HostAddr = text3;
					cfg.Addr = text3;
					cfg.Map = text4;
					cfg.Studio = _studioPath;
					ConfigManager.SaveConfig(cfg);
					RbxmBridgeServer.ActiveUsername = text5;
					RbxmBridgeServer.ActiveUid = text;
					ShowHostRunningView(text, text2, text3, text4);
				}
			}
		};
		stackPanel2.Children.Add(button5);
		stackPanel.Children.Add(stackPanel2);
		grid.Children.Add(stackPanel);
		NavigateTo(grid);
	}

	private void ShowHostRunningView(string uid, string port, string addr, string mapPath)
	{
		_isHostActive = true;
		string pg = Guid.NewGuid().ToString().ToUpper();
		string tg = Guid.NewGuid().ToString().ToUpper();
		Grid grid = new Grid
		{
			Background = (SolidColorBrush)FindResource("BgBrush")
		};
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(18.0, 12.0, 18.0, 12.0)
		};
		stackPanel.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("host_console_title"),
			FontSize = 18.0,
			FontWeight = FontWeights.Bold,
			Foreground = (SolidColorBrush)FindResource("TextBrush"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 4.0, 0.0, 4.0)
		});
		(Border Border, RichTextBox RichText) logBox = CreateLogBox(200.0);
		stackPanel.Children.Add(logBox.Border);
		RobloxStudioService.OnStudioError = delegate(string msg, string tag)
		{
			base.Dispatcher.Invoke(delegate
			{
				LogAppend(logBox.RichText, msg, tag);
			});
		};
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 10.0, 0.0, 0.0)
		};
		Button joinLocalBtn = new Button
		{
			Content = IconFactory.CreateButtonContent("join", LocalizationService.Get("btn_join_locally"), 16.0),
			Background = (SolidColorBrush)FindResource("WarnBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			IsEnabled = false,
			Margin = new Thickness(6.0, 0.0, 6.0, 0.0)
		};
		joinLocalBtn.Click += delegate
		{
			try
			{
				RobloxStudioService.LaunchClient(_studioPath, "127.0.0.1", port, pg, tg, uid, "StudioPlayer_Host");
				LogAppend(logBox.RichText, "Local client launched.", "info");
			}
			catch (Exception ex)
			{
				LogAppend(logBox.RichText, "Launch error: " + ex.Message, "err");
			}
		};
		stackPanel2.Children.Add(joinLocalBtn);
		Button button = new Button
		{
			Content = IconFactory.CreateButtonContent("stop", LocalizationService.Get("btn_stop_back"), 16.0),
			Background = (SolidColorBrush)FindResource("ErrBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(6.0, 0.0, 6.0, 0.0)
		};
		button.Click += delegate
		{
			ShowConfirmationAlert(LocalizationService.Get("alert_stop_host_title"), LocalizationService.Get("alert_stop_host_msg"), LocalizationService.Get("alert_stop_host_btn"), delegate
			{
				ShowMainMenuView("right");
			});
		};
		stackPanel2.Children.Add(button);
		Button button2 = new Button
		{
			Content = IconFactory.CreateButtonContent("test", LocalizationService.Get("test"), 14.0),
			Background = (SolidColorBrush)FindResource("Card2Brush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(6.0, 0.0, 6.0, 0.0)
		};
		button2.Click += delegate
		{
			string h = (addr.Contains(':') ? addr.Split(':', 2)[0] : addr);
			int result;
			int tp = ((addr.Contains(':') && int.TryParse(addr.Split(':', 2)[1], out result)) ? result : int.Parse(port));
			Task.Run(() => ConnectivityTester.RunConnectivityTestAsync(h, tp, delegate(string m, string t)
			{
				base.Dispatcher.Invoke(delegate
				{
					LogAppend(logBox.RichText, m, t);
				});
			}, isHostSide: true, int.Parse(port)));
		};
		stackPanel2.Children.Add(button2);
		stackPanel.Children.Add(stackPanel2);
		grid.Children.Add(stackPanel);
		NavigateTo(grid);
		Task.Run(async delegate
		{
			Logger.Log($"Host Session Started: UID={uid}, Port={port}, Addr={addr}, Map={mapPath}");
			Logger.FetchLatestRobloxStudioLog();
			base.Dispatcher.Invoke(delegate
			{
				LogAppend(logBox.RichText, "Parent GUID: " + pg, "dim");
				LogAppend(logBox.RichText, "Play  GUID : " + tg, "dim");
				LogAppend(logBox.RichText, "Port       : " + port);
				LogAppend(logBox.RichText, "Address    : " + addr, "info");
			});
			if (!string.IsNullOrEmpty(mapPath) && File.Exists(mapPath))
			{
				base.Dispatcher.Invoke(delegate
				{
					LogAppend(logBox.RichText, "Injecting selected map: " + System.IO.Path.GetFileName(mapPath), "warn");
				});
				if (MapInjector.InjectMap(mapPath))
				{
					base.Dispatcher.Invoke(delegate
					{
						LogAppend(logBox.RichText, "✓ Map copied to Roblox runtime cache", "ok");
					});
				}
				else
				{
					base.Dispatcher.Invoke(delegate
					{
						LogAppend(logBox.RichText, "✗ Failed to inject map. Studio will load default cache.", "err");
					});
				}
			}
			else
			{
				base.Dispatcher.Invoke(delegate
				{
					LogAppend(logBox.RichText, "No map selected. Injecting default Roblox Baseplate map…", "warn");
				});
				if (MapInjector.InjectMap(""))
				{
					base.Dispatcher.Invoke(delegate
					{
						LogAppend(logBox.RichText, "✓ Default Baseplate map loaded into Roblox runtime cache", "ok");
					});
				}
			}
			base.Dispatcher.Invoke(delegate
			{
				LogAppend(logBox.RichText, "Launching Studio server process…");
			});
			try
			{
				UdpProxy.StopProxy(wait: false);
				RobloxStudioService.LaunchServer(_studioPath, port, uid, pg, tg);
				base.Dispatcher.Invoke(delegate
				{
					LogAppend(logBox.RichText, "Server started! Waiting 5 s for Studio init…", "ok");
				});
				ConfigManager.WriteSessionLog(pg, tg, addr, port, uid);
				await Task.Delay(5000);
				base.Dispatcher.Invoke(delegate
				{
					LogAppend(logBox.RichText, "● SERVER IS LIVE", "ok");
					LogAppend(logBox.RichText, "Session info saved → " + ConfigManager.LogFile, "dim");
					Clipboard.SetText(addr);
					joinLocalBtn.IsEnabled = true;
					SetStatus(LocalizationService.Get("status_live"), (SolidColorBrush)FindResource("OkBrush"));
				});
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Exception ex3 = ex2;
				base.Dispatcher.Invoke(delegate
				{
					LogAppend(logBox.RichText, "ERROR: " + ex3.Message, "err");
					SetStatus("Server launch failed", (SolidColorBrush)FindResource("ErrBrush"));
				});
			}
		});
	}

	private void ShowJoinConfigView()
	{
		Grid grid = new Grid
		{
			Background = (SolidColorBrush)FindResource("BgBrush")
		};
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(24.0, 12.0, 24.0, 12.0)
		};
		stackPanel.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("join_title"),
			FontSize = 18.0,
			FontWeight = FontWeights.Bold,
			Foreground = (SolidColorBrush)FindResource("TextBrush"),
			HorizontalAlignment = HorizontalAlignment.Center
		});
		stackPanel.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("join_sub"),
			FontSize = 13.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 2.0, 0.0, 12.0)
		});
		Border border = new Border
		{
			Background = (SolidColorBrush)FindResource("CardBrush"),
			BorderBrush = (SolidColorBrush)FindResource("BordBrush"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(12.0),
			Padding = new Thickness(16.0)
		};
		NepConfig cfg = ConfigManager.LoadConfig();
		StackPanel stackPanel2 = new StackPanel();
		stackPanel2.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("lbl_username"),
			FontSize = 14.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush"),
			Margin = new Thickness(0.0, 0.0, 0.0, 4.0)
		});
		TextBox userTb = new TextBox
		{
			Text = cfg.Username,
			Style = (Style)FindResource("NepTextBoxStyle"),
			Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
		};
		stackPanel2.Children.Add(userTb);
		stackPanel2.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("lbl_tunnel_input"),
			FontSize = 14.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush"),
			Margin = new Thickness(0.0, 0.0, 0.0, 4.0)
		});
		TextBox addrTb = new TextBox
		{
			Text = ((!string.IsNullOrEmpty(cfg.JoinAddr)) ? cfg.JoinAddr : cfg.HostAddr),
			Style = (Style)FindResource("NepTextBoxStyle"),
			Margin = new Thickness(0.0, 0.0, 0.0, 6.0)
		};
		stackPanel2.Children.Add(addrTb);
		stackPanel2.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("lbl_proxy_hint"),
			FontSize = 12.0,
			Foreground = (SolidColorBrush)FindResource("MuteBrush")
		});
		TextBlock errLbl = new TextBlock
		{
			Text = "",
			FontSize = 12.0,
			Foreground = (SolidColorBrush)FindResource("ErrBrush"),
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
		};
		stackPanel2.Children.Add(errLbl);
		border.Child = stackPanel2;
		stackPanel.Children.Add(border);
		StackPanel stackPanel3 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 16.0, 0.0, 0.0)
		};
		Button button = new Button
		{
			Content = IconFactory.CreateButtonContent("back", LocalizationService.Get("back"), 14.0),
			Background = (SolidColorBrush)FindResource("CardBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(6.0, 0.0, 6.0, 0.0)
		};
		button.Click += delegate
		{
			ShowMainMenuView("right");
		};
		stackPanel3.Children.Add(button);
		Button button2 = new Button
		{
			Content = IconFactory.CreateButtonContent("join", LocalizationService.Get("btn_connect_launch"), 16.0),
			Background = (SolidColorBrush)FindResource("BlueBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(6.0, 0.0, 6.0, 0.0)
		};
		button2.Click += delegate
		{
			string text = userTb.Text.Trim();
			string text2 = addrTb.Text.Trim();
			if (string.IsNullOrEmpty(text2) || !text2.Contains(':'))
			{
				errLbl.Text = "Format must be host:port";
			}
			else
			{
				string[] array = text2.Split(':', 2);
				if (!int.TryParse(array[1], out var result))
				{
					errLbl.Text = "Port must be a number";
				}
				else
				{
					if (string.IsNullOrEmpty(_studioPath) || !File.Exists(_studioPath))
					{
						_studioPath = RobloxStudioService.GetStudioPath();
						NepConfig nepConfig = ConfigManager.LoadConfig();
						nepConfig.Studio = _studioPath;
						ConfigManager.SaveConfig(nepConfig);
					}
					if (string.IsNullOrEmpty(_studioPath) || !File.Exists(_studioPath))
					{
						MessageBox.Show("Roblox Studio was not found on your system.", "Studio Not Found", MessageBoxButton.OK, MessageBoxImage.Hand);
					}
					else
					{
						errLbl.Text = "";
						cfg.Username = text;
						cfg.JoinAddr = text2;
						ConfigManager.SaveConfig(cfg);
						RbxmBridgeServer.ActiveUsername = text;
						ShowJoinRunningView(array[0], result);
					}
				}
			}
		};
		stackPanel3.Children.Add(button2);
		stackPanel.Children.Add(stackPanel3);
		grid.Children.Add(stackPanel);
		NavigateTo(grid);
	}

	private void ShowJoinRunningView(string dstHost, int dstPort)
	{
		_isJoinActive = true;
		Grid grid = new Grid
		{
			Background = (SolidColorBrush)FindResource("BgBrush")
		};
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(18.0, 12.0, 18.0, 12.0)
		};
		stackPanel.Children.Add(new TextBlock
		{
			Text = LocalizationService.Get("join_console_title"),
			FontSize = 18.0,
			FontWeight = FontWeights.Bold,
			Foreground = (SolidColorBrush)FindResource("TextBrush"),
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 4.0, 0.0, 4.0)
		});
		(Border Border, RichTextBox RichText) logBox = CreateLogBox(200.0);
		stackPanel.Children.Add(logBox.Border);
		RobloxStudioService.OnStudioError = delegate(string msg, string tag)
		{
			base.Dispatcher.Invoke(delegate
			{
				LogAppend(logBox.RichText, msg, tag);
			});
		};
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 10.0, 0.0, 0.0)
		};
		Action disconnect = delegate
		{
			LogAppend(logBox.RichText, "Stopping proxy…", "warn");
			UdpProxy.StopProxy();
			SetStatus(LocalizationService.Get("status_disconnected"), (SolidColorBrush)FindResource("MuteBrush"));
			Task.Delay(400).ContinueWith(delegate
			{
				base.Dispatcher.Invoke(delegate
				{
					ShowMainMenuView("right");
				});
			});
		};
		Button button = new Button
		{
			Content = IconFactory.CreateButtonContent("stop", LocalizationService.Get("btn_disc_back"), 16.0),
			Background = (SolidColorBrush)FindResource("ErrBrush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(6.0, 0.0, 6.0, 0.0)
		};
		button.Click += delegate
		{
			ShowConfirmationAlert(LocalizationService.Get("alert_disc_title"), LocalizationService.Get("alert_disc_msg"), LocalizationService.Get("alert_disc_btn"), delegate
			{
				disconnect();
			});
		};
		stackPanel2.Children.Add(button);
		Button button2 = new Button
		{
			Content = IconFactory.CreateButtonContent("test", LocalizationService.Get("test"), 14.0),
			Background = (SolidColorBrush)FindResource("Card2Brush"),
			Style = (Style)FindResource("NepButtonStyle"),
			Margin = new Thickness(6.0, 0.0, 6.0, 0.0)
		};
		button2.Click += delegate
		{
			Task.Run(() => ConnectivityTester.RunConnectivityTestAsync(dstHost, dstPort, delegate(string m, string t)
			{
				base.Dispatcher.Invoke(delegate
				{
					LogAppend(logBox.RichText, m, t);
				});
			}));
		};
		stackPanel2.Children.Add(button2);
		stackPanel.Children.Add(stackPanel2);
		grid.Children.Add(stackPanel);
		NavigateTo(grid);
		Task.Run(async delegate
		{
			Logger.Log($"Join Session Started: Target={dstHost}:{dstPort}");
			Logger.FetchLatestRobloxStudioLog();
			string pg = Guid.NewGuid().ToString().ToUpper();
			string tg = Guid.NewGuid().ToString().ToUpper();
			base.Dispatcher.Invoke(delegate
			{
				LogAppend(logBox.RichText, "Starting UDP Proxy...", "dim");
			});
			if (!UdpProxy.StartProxy(dstHost, dstPort))
			{
				base.Dispatcher.Invoke(delegate
				{
					LogAppend(logBox.RichText, "✗ Proxy failed to start (DNS timeout?).", "err");
					SetStatus("Connection Failed", (SolidColorBrush)FindResource("ErrBrush"));
				});
				return;
			}
			UdpProxy.WarmTunnel(dstHost, dstPort);
			await Task.Delay(250);
			base.Dispatcher.Invoke(delegate
			{
				LogAppend(logBox.RichText, "Parent GUID: " + pg, "dim");
				LogAppend(logBox.RichText, "Play  GUID : " + tg, "dim");
				LogAppend(logBox.RichText, "Launching Studio client via Local Proxy…");
			});
			try
			{
				NepConfig nepConfig = ConfigManager.LoadConfig();
				if (string.IsNullOrWhiteSpace(nepConfig.Username))
				{
					_ = nepConfig.Uid;
				}
				else
				{
					_ = nepConfig.Username;
				}
				RobloxStudioService.LaunchClient(_studioPath, "127.0.0.1", 55555.ToString(), pg, tg, nepConfig.Uid, "StudioPlayer_Proxy");
				base.Dispatcher.Invoke(delegate
				{
					LogAppend(logBox.RichText, "● CONNECTED — Studio launched", "ok");
					SetStatus(LocalizationService.Get("status_connected"), (SolidColorBrush)FindResource("OkBrush"));
				});
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Exception ex3 = ex2;
				base.Dispatcher.Invoke(delegate
				{
					LogAppend(logBox.RichText, "Studio launch error: " + ex3.Message, "err");
					SetStatus("Studio launch failed", (SolidColorBrush)FindResource("ErrBrush"));
				});
			}
		});
	}

	private (Border Border, RichTextBox RichText) CreateLogBox(double height = 180.0)
	{
		RichTextBox richTextBox = new RichTextBox
		{
			Background = new SolidColorBrush(Color.FromRgb(6, 2, 16)),
			Foreground = new SolidColorBrush(Color.FromRgb(179, 157, 219)),
			FontFamily = new FontFamily("Consolas"),
			FontSize = 11.0,
			IsReadOnly = true,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			BorderThickness = new Thickness(0.0),
			Padding = new Thickness(6.0)
		};
		return (Border: new Border
		{
			BorderBrush = (SolidColorBrush)FindResource("BordBrush"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(6.0),
			Height = height,
			Child = richTextBox
		}, RichText: richTextBox);
	}

	private void LogAppend(RichTextBox rtb, string msg, string tag = "")
	{
		Paragraph paragraph = new Paragraph
		{
			Margin = new Thickness(0.0)
		};
		Run item = new Run($"[{DateTime.Now:HH:mm:ss}]  ")
		{
			Foreground = (SolidColorBrush)FindResource("MuteBrush")
		};
		paragraph.Inlines.Add(item);
		SolidColorBrush foreground = tag switch
		{
			"ok" => (SolidColorBrush)FindResource("OkBrush"), 
			"err" => (SolidColorBrush)FindResource("ErrBrush"), 
			"warn" => (SolidColorBrush)FindResource("WarnBrush"), 
			"info" => (SolidColorBrush)FindResource("GlowBrush"), 
			"dim" => (SolidColorBrush)FindResource("MuteBrush"), 
			_ => (SolidColorBrush)FindResource("TextBrush"), 
		};
		Run item2 = new Run(msg)
		{
			Foreground = foreground
		};
		paragraph.Inlines.Add(item2);
		rtb.Document.Blocks.Add(paragraph);
		rtb.ScrollToEnd();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.10.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/NepTunnel;component/mainwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.10.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			RootMainGrid = (Grid)target;
			break;
		case 2:
			WinMinBtn = (Button)target;
			WinMinBtn.Click += WinMinBtn_Click;
			break;
		case 3:
			WinMaxBtn = (Button)target;
			WinMaxBtn.Click += WinMaxBtn_Click;
			break;
		case 4:
			WinCloseBtn = (Button)target;
			WinCloseBtn.Click += WinCloseBtn_Click;
			break;
		case 5:
			BannerImage = (Image)target;
			break;
		case 6:
			ViewContainer = (Grid)target;
			break;
		case 7:
			StatusLabel = (TextBlock)target;
			break;
		case 8:
			LangBtn = (Button)target;
			LangBtn.Click += LangBtn_Click;
			break;
		case 9:
			AlertOverlayGrid = (Grid)target;
			break;
		case 10:
			AlertTitleTxt = (TextBlock)target;
			break;
		case 11:
			AlertMessageTxt = (TextBlock)target;
			break;
		case 12:
			AlertCancelBtn = (Button)target;
			break;
		case 13:
			AlertConfirmBtn = (Button)target;
			break;
		case 14:
			ImageModalOverlay = (Grid)target;
			ImageModalOverlay.MouseDown += CloseImageModal_Click;
			break;
		case 15:
			ZoomedImageControl = (Image)target;
			break;
		case 16:
			((Button)target).Click += CloseImageModal_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}

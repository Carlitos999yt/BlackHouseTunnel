using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NepTunnel.Services;

namespace NepTunnel.Views
{
    public static class ToolViews
    {
        public static FrameworkElement CreateTutorialView(
            Action onBackClick,
            Action<BitmapImage> onOpenImageModal,
            Func<string, object> findResource)
        {
            var grid = new Grid { Background = (SolidColorBrush)findResource("BgBrush") };
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { Margin = new Thickness(24, 16, 24, 16) };

            stack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("tut_title"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)findResource("TextBrush"),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            stack.Children.Add(new TextBlock
            {
                Text = LocalizationService.Get("tut_sub"),
                FontSize = 13,
                Foreground = (SolidColorBrush)findResource("MuteBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 16)
            });

            for (int i = 1; i <= 9; i++)
            {
                string titleKey = $"tut_s{i}_t";
                string descKey = $"tut_s{i}_d";

                var card = new Border
                {
                    Background = (SolidColorBrush)findResource("CardBrush"),
                    BorderBrush = (SolidColorBrush)findResource("BordBrush"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 0, 0, 14)
                };

                var stepStack = new StackPanel();
                stepStack.Children.Add(new TextBlock
                {
                    Text = LocalizationService.Get(titleKey),
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    Foreground = (SolidColorBrush)findResource("GlowBrush"),
                    Margin = new Thickness(0, 0, 0, 4)
                });

                stepStack.Children.Add(new TextBlock
                {
                    Text = LocalizationService.Get(descKey),
                    FontSize = 13,
                    Foreground = (SolidColorBrush)findResource("TextBrush"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                BitmapImage? bitmap = null;

                try
                {
                    var packUri = new Uri($"pack://application:,,,/bundled_assets/tut_{i}.png", UriKind.Absolute);
                    bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = packUri;
                    bitmap.EndInit();
                }
                catch { bitmap = null; }

                if (bitmap == null)
                {
                    string imgFileName = $"bundled_assets/tut_{i}.png";
                    if (File.Exists(imgFileName))
                    {
                        try
                        {
                            var imgUri = new Uri(Path.GetFullPath(imgFileName), UriKind.Absolute);
                            bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = imgUri;
                            bitmap.EndInit();
                        }
                        catch { bitmap = null; }
                    }
                }

                if (bitmap != null)
                {
                    var imgControl = new Image
                    {
                        Source = bitmap,
                        MaxHeight = 320,
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Margin = new Thickness(0, 4, 0, 4),
                        ToolTip = "Click to enlarge / Haz clic para maximizar"
                    };
                    imgControl.MouseDown += (s, e) => onOpenImageModal(bitmap);

                    var imgBorder = new Border
                    {
                        Background = (SolidColorBrush)findResource("Card2Brush"),
                        BorderBrush = (SolidColorBrush)findResource("BordBrush"),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(4),
                        Child = imgControl
                    };
                    stepStack.Children.Add(imgBorder);
                }

                card.Child = stepStack;
                stack.Children.Add(card);
            }

            var backBtn = new Button
            {
                Content = IconFactory.CreateButtonContent("back", LocalizationService.Get("back"), 14),
                Background = (SolidColorBrush)findResource("Card2Brush"),
                Style = (Style)findResource("NepButtonStyle"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };
            backBtn.Click += (s, e) => onBackClick();
            stack.Children.Add(backBtn);

            scroll.Content = stack;
            grid.Children.Add(scroll);
            return grid;
        }
    }
}

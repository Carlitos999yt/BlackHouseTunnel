using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using BlackHouseTunnel.Services;

namespace BlackHouseTunnel.Views
{
    public class StudioSelectorModal : UserControl
    {
        public event EventHandler<string>? OnStudioSelected;
        public event EventHandler? OnCloseRequested;

        private readonly string _currentStudioPath;

        public StudioSelectorModal(string currentStudioPath)
        {
            _currentStudioPath = currentStudioPath;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Grid modalRoot = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(190, 6, 6, 10))
            };

            Border modalCard = new Border
            {
                Width = 560,
                MaxHeight = 480,
                Padding = new Thickness(24),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0C0C14")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(16),
                Effect = new DropShadowEffect
                {
                    Color = (Color)ColorConverter.ConvertFromString("#5865F2"),
                    BlurRadius = 30,
                    Opacity = 0.35,
                    ShadowDepth = 0
                }
            };

            StackPanel modalStack = new StackPanel();

            TextBlock title = new TextBlock
            {
                Text = "🎯 Selector de Instalación de Roblox Studio",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };

            TextBlock sub = new TextBlock
            {
                Text = "Selecciona la versión o Mod Manager ejecutable a utilizar para crear o unirte a túneles.",
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };

            modalStack.Children.Add(title);
            modalStack.Children.Add(sub);

            var installations = RobloxStudioService.GetDetectedStudioInstallations();
            if (!string.IsNullOrEmpty(_currentStudioPath) && File.Exists(_currentStudioPath) &&
                !installations.Any(i => i.Path.Equals(_currentStudioPath, StringComparison.OrdinalIgnoreCase)))
            {
                installations.Insert(0, new RobloxStudioService.StudioInstallation("Roblox Studio (Ruta Activa)", _currentStudioPath, "RSM", true));
            }

            StackPanel listStack = new StackPanel();

            if (installations.Count == 0)
            {
                listStack.Children.Add(new TextBlock
                {
                    Text = "⚠ No se detectaron instalaciones automáticas de Roblox Studio.",
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 20)
                });
            }
            else
            {
                foreach (var inst in installations)
                {
                    bool isSelected = !string.IsNullOrEmpty(_currentStudioPath) &&
                                       inst.Path.Equals(_currentStudioPath, StringComparison.OrdinalIgnoreCase);

                    Border itemCard = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isSelected ? "#1E1E34" : "#12121A")),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isSelected ? "#5865F2" : "#222234")),
                        BorderThickness = new Thickness(isSelected ? 2 : 1),
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(14, 10, 14, 10),
                        Margin = new Thickness(0, 0, 0, 8),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };

                    StackPanel itemStack = new StackPanel();
                    Grid headerGrid = new Grid();
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    StackPanel titleWithIcon = new StackPanel { Orientation = Orientation.Horizontal };

                    ImageSource? iconSrc = GetExecutableIcon(inst.Path);
                    if (iconSrc != null)
                    {
                        Image iconImg = new Image
                        {
                            Source = iconSrc,
                            Width = 22,
                            Height = 22,
                            Margin = new Thickness(0, 0, 8, 0),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        titleWithIcon.Children.Add(iconImg);
                    }
                    else
                    {
                        TextBlock fallbackIcon = new TextBlock
                        {
                            Text = "🎮 ",
                            FontSize = 14,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        titleWithIcon.Children.Add(fallbackIcon);
                    }

                    TextBlock titleTxt = new TextBlock
                    {
                        Text = inst.Name,
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = isSelected ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC")),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    titleWithIcon.Children.Add(titleTxt);

                    Grid.SetColumn(titleWithIcon, 0);
                    headerGrid.Children.Add(titleWithIcon);

                    StackPanel badgePanel = new StackPanel { Orientation = Orientation.Horizontal };
                    if (isSelected)
                    {
                        Border activeTag = CreateBadge("ACTIVO", "#5865F2");
                        badgePanel.Children.Add(activeTag);
                    }
                    if (inst.IsRecommended)
                    {
                        Border recTag = CreateBadge("RECOMENDADO", "#7C3AED");
                        badgePanel.Children.Add(recTag);
                    }
                    else if (inst.Type == "Bloxstrap")
                    {
                        Border tagBorder = CreateBadge("BLOXSTRAP", "#10B981");
                        badgePanel.Children.Add(tagBorder);
                    }
                    else if (inst.Type == "Oficial")
                    {
                        Border tagBorder = CreateBadge("OFICIAL", "#3B82F6");
                        badgePanel.Children.Add(tagBorder);
                    }

                    Grid.SetColumn(badgePanel, 1);
                    headerGrid.Children.Add(badgePanel);
                    itemStack.Children.Add(headerGrid);

                    TextBlock pathTxt = new TextBlock
                    {
                        Text = inst.Path,
                        FontSize = 10,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8E9297")),
                        Margin = new Thickness(30, 4, 0, 0),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    itemStack.Children.Add(pathTxt);

                    itemCard.Child = itemStack;

                    string selectedPath = inst.Path;
                    itemCard.MouseDown += (s, e) =>
                    {
                        OnStudioSelected?.Invoke(this, selectedPath);
                    };

                    listStack.Children.Add(itemCard);
                }
            }

            ScrollViewer listScroll = new ScrollViewer
            {
                MaxHeight = 300,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = listStack
            };
            modalStack.Children.Add(listScroll);

            Button cancelBtn = new Button
            {
                Content = "Cancelar",
                Height = 36,
                Padding = new Thickness(20, 0, 20, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#222234")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };

            ControlTemplate cTemplate = new ControlTemplate(typeof(Button));
            FrameworkElementFactory cBorder = new FrameworkElementFactory(typeof(Border));
            cBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            cBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            FrameworkElementFactory cPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            cPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            cBorder.AppendChild(cPresenter);
            cTemplate.VisualTree = cBorder;
            cancelBtn.Template = cTemplate;

            cancelBtn.Click += (s, e) => OnCloseRequested?.Invoke(this, EventArgs.Empty);
            modalStack.Children.Add(cancelBtn);

            modalCard.Child = modalStack;
            modalRoot.Children.Add(modalCard);

            this.Content = modalRoot;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SMALLICON = 0x000000001;

        private ImageSource? GetExecutableIcon(string exePath)
        {
            try
            {
                if (File.Exists(exePath))
                {
                    SHFILEINFO shinfo = new SHFILEINFO();
                    IntPtr hImg = SHGetFileInfo(exePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_SMALLICON);
                    if (shinfo.hIcon != IntPtr.Zero)
                    {
                        ImageSource img = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                            shinfo.hIcon,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        DestroyIcon(shinfo.hIcon);
                        return img;
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        private Border CreateBadge(string label, string colorHex)
        {
            return new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(4, 0, 0, 0),
                Child = new TextBlock
                {
                    Text = label,
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                }
            };
        }
    }
}

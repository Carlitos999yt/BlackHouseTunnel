using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
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

                    TextBlock titleTxt = new TextBlock
                    {
                        Text = inst.Name,
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = isSelected ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC"))
                    };
                    Grid.SetColumn(titleTxt, 0);
                    headerGrid.Children.Add(titleTxt);

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
                        FontSize = 11,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8E9297")),
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        Margin = new Thickness(0, 4, 0, 0)
                    };
                    itemStack.Children.Add(pathTxt);
                    itemCard.Child = itemStack;

                    string targetPath = inst.Path;
                    itemCard.MouseLeftButtonDown += (s, e) =>
                    {
                        OnStudioSelected?.Invoke(this, targetPath);
                        OnCloseRequested?.Invoke(this, EventArgs.Empty);
                    };

                    listStack.Children.Add(itemCard);
                }
            }

            ScrollViewer listScroll = new ScrollViewer
            {
                MaxHeight = 260,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = listStack,
                Margin = new Thickness(0, 0, 0, 16)
            };
            modalStack.Children.Add(listScroll);

            Button closeBtn = new Button
            {
                Content = "Cerrar",
                Width = 120,
                Height = 36,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F1F30")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            ControlTemplate btnTemplate = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            FrameworkElementFactory presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);
            btnTemplate.VisualTree = borderFactory;
            closeBtn.Template = btnTemplate;
            closeBtn.Click += (s, e) => OnCloseRequested?.Invoke(this, EventArgs.Empty);

            modalStack.Children.Add(closeBtn);
            modalCard.Child = modalStack;
            modalRoot.Children.Add(modalCard);

            this.Content = modalRoot;
        }

        private Border CreateBadge(string text, string hexBg)
        {
            Border b = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexBg)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(4, 0, 0, 0)
            };
            TextBlock t = new TextBlock { Text = text, FontSize = 9, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
            b.Child = t;
            return b;
        }
    }
}

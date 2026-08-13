using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using BlackHouseTunnel.Services;

namespace BlackHouseTunnel.Views
{
    public class RbxmImporterView : UserControl
    {
        private readonly StackPanel _listStack;
        private readonly List<string> _savedMaps = new();

        public RbxmImporterView()
        {
            Grid mainGrid = new Grid { Margin = new Thickness(28) };
            StackPanel panel = new StackPanel { MaxWidth = 750, HorizontalAlignment = HorizontalAlignment.Left };

            TextBlock title = new TextBlock
            {
                Text = "📦 Gestor e Importador de Mapas & Modelos (.rbxm / .rbxmx)",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            };

            TextBlock sub = new TextBlock
            {
                Text = "Encola archivos .rbxm en el servidor puente HTTP (puerto 7878) para transferirlos a Roblox Studio en 1 clic.",
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA")),
                Margin = new Thickness(0, 0, 0, 20)
            };

            panel.Children.Add(title);
            panel.Children.Add(sub);

            // Action Header Bar
            StackPanel headerActions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };

            Button addMapBtn = new Button
            {
                Content = "📁 Agregar Archivo (.rbxm / .rbxmx)",
                Height = 44,
                Padding = new Thickness(24, 0, 24, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C3AED")),
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            SetButtonTemplate(addMapBtn);

            addMapBtn.Click += (s, e) =>
            {
                OpenFileDialog dlg = new OpenFileDialog
                {
                    Title = "Seleccionar mapa o modelo .rbxm",
                    Filter = "Modelos Roblox (*.rbxm;*.rbxmx)|*.rbxm;*.rbxmx|Todos los archivos (*.*)|*.*",
                    Multiselect = true
                };
                if (dlg.ShowDialog() == true)
                {
                    foreach (var fn in dlg.FileNames)
                    {
                        string fullP = Path.GetFullPath(fn);
                        if (!_savedMaps.Contains(fullP))
                        {
                            _savedMaps.Add(fullP);
                        }
                    }
                    RefreshMapList();
                }
            };

            headerActions.Children.Add(addMapBtn);
            panel.Children.Add(headerActions);

            // Scrollable Map List Container
            Border listBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D0D14")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F1F30")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Height = 320,
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 16)
            };

            ScrollViewer listScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            _listStack = new StackPanel();
            listScroll.Content = _listStack;
            listBorder.Child = listScroll;

            panel.Children.Add(listBorder);

            RefreshMapList();

            mainGrid.Children.Add(panel);
            this.Content = mainGrid;
        }

        private void RefreshMapList()
        {
            _listStack.Children.Clear();
            if (_savedMaps.Count == 0)
            {
                _listStack.Children.Add(new TextBlock
                {
                    Text = "No hay mapas cargados. Haz clic en 'Agregar Archivo' para comenzar.",
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8E9297")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 120, 0, 0)
                });
                return;
            }

            for (int i = 0; i < _savedMaps.Count; i++)
            {
                string path = _savedMaps[i];
                bool exists = File.Exists(path);

                Grid rowGrid = new Grid
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(i % 2 == 0 ? "#12121A" : "#161622")),
                    Margin = new Thickness(0, 0, 0, 6)
                };

                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                StackPanel leftPanel = new StackPanel { Margin = new Thickness(12, 8, 12, 8) };
                leftPanel.Children.Add(new TextBlock
                {
                    Text = Path.GetFileName(path),
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = exists ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8E9297"))
                });

                leftPanel.Children.Add(new TextBlock
                {
                    Text = path,
                    FontSize = 11,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(exists ? "#8E9297" : "#ED4245")),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });

                Grid.SetColumn(leftPanel, 0);
                rowGrid.Children.Add(leftPanel);

                StackPanel rightPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8) };

                Button sendBtn = new Button
                {
                    Content = "🚀 Enviar a Studio",
                    Height = 32,
                    Padding = new Thickness(12, 0, 12, 0),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5865F2")),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    IsEnabled = exists,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                SetButtonTemplate(sendBtn);

                string targetPath = path;
                sendBtn.Click += (s, e) =>
                {
                    var (ok, msg) = RbxmBridgeServer.QueueRbxm(targetPath);
                    if (ok)
                    {
                        DarkMessageBox.Show($"✓ Mapa '{msg}' encolado correctamente en el servidor puente HTTP (puerto 7878).\n\nEn Roblox Studio presiona ▶ Listen para descargarlo.", "Mapa Encolado", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        DarkMessageBox.Show($"Error al encolar mapa: {msg}", "Error Mapa", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                Button deleteBtn = new Button
                {
                    Content = "🗑️",
                    Width = 32,
                    Height = 32,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ED4245")),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                SetButtonTemplate(deleteBtn);

                deleteBtn.Click += (s, e) =>
                {
                    _savedMaps.Remove(targetPath);
                    RefreshMapList();
                };

                rightPanel.Children.Add(sendBtn);
                rightPanel.Children.Add(deleteBtn);
                Grid.SetColumn(rightPanel, 1);
                rowGrid.Children.Add(rightPanel);

                _listStack.Children.Add(rowGrid);
            }
        }

        private void SetButtonTemplate(Button btn)
        {
            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            FrameworkElementFactory presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);
            template.VisualTree = borderFactory;
            btn.Template = template;
        }
    }
}

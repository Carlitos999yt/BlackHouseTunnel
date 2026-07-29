using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NepTunnel.Services
{
    public static class IconFactory
    {
        public static Geometry GetIconGeometry(string iconName)
        {
            return iconName switch
            {
                // Host: Server box with arrow pointing UP out of it
                "host" => Geometry.Parse("M 3,8 L 3,14 L 13,14 L 13,8 M 5,7 L 8,3 L 11,7 M 8,3 L 8,11"),

                // Join: Connected chain links / opposing link arcs
                "join" => Geometry.Parse("M 6,3 C 2,3 2,13 6,13 M 10,3 C 14,3 14,13 10,13 M 4,8 L 12,8"),

                // Echo: Two opposing exchange arrows (right-top, left-bottom)
                "echo" => Geometry.Parse("M 3,5 L 13,5 M 10,2 L 13,5 L 10,8 M 13,11 L 3,11 M 6,8 L 3,11 L 6,14"),

                // Map: Grid box with cross lines and pin dot
                "map" => Geometry.Parse("M 2,2 L 14,2 L 14,14 L 2,14 Z M 8,2 L 8,14 M 2,8 L 14,8 M 11,5 A 1.2,1.2 0 1,1 11,4.99"),

                // Folder: Folder shape
                "folder" => Geometry.Parse("M 2,3 L 6,3 L 8,5 L 14,5 L 14,13 L 2,13 Z"),

                // Play: Right arrow triangle
                "play" => Geometry.Parse("M 4,2 L 14,8 L 4,14 Z"),

                // Back: Left arrow triangle
                "back" => Geometry.Parse("M 12,2 L 4,8 L 12,14 Z"),

                // Stop: Cross X
                "stop" => Geometry.Parse("M 3,3 L 13,13 M 13,3 L 3,13"),

                // Test: Signal bars
                "test" => Geometry.Parse("M 3,12 L 3,14 M 7,8 L 7,14 M 11,4 L 11,14 M 2,14 L 14,14"),

                // Send: Right-up arrow
                "send" => Geometry.Parse("M 3,13 L 13,3 M 13,3 L 6,3 M 13,3 L 13,10"),

                // Trash: Trash bin
                "trash" => Geometry.Parse("M 3,4 L 13,4 M 5,4 L 5,13 L 11,13 L 11,4 M 6,2 L 10,2"),

                _ => Geometry.Parse("M 3,3 L 13,13 M 13,3 L 3,13")
            };
        }

        public static StackPanel CreateButtonContent(string iconName, string text, double size = 18)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (!string.IsNullOrEmpty(iconName))
            {
                var path = new Path
                {
                    Data = GetIconGeometry(iconName),
                    Stroke = new SolidColorBrush(Color.FromRgb(0xF0, 0xE6, 0xFF)),
                    StrokeThickness = 1.8,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var viewbox = new Viewbox
                {
                    Width = size,
                    Height = size,
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Child = path
                };

                panel.Children.Add(viewbox);
            }

            if (!string.IsNullOrEmpty(text))
            {
                var txtBlock = new TextBlock
                {
                    Text = text,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xE6, 0xFF)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                panel.Children.Add(txtBlock);
            }

            return panel;
        }
    }
}

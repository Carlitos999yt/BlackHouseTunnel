using System;
using System.Windows;
using System.Windows.Media;

namespace NepTunnel.Services
{
    // Generates resolution-independent vector DrawingImages for UI banner and logo branding.
    public static class VectorAssetService
    {
        // Generates a crisp vector banner with gradient background, glowing orbital rings, and vector text.
        public static DrawingImage CreateVectorBanner(double width = 720, double height = 180)
        {
            var group = new DrawingGroup();

            // 1. Gradient Background Rect
            var bgGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            bgGradient.GradientStops.Add(new GradientStop(Color.FromRgb(0x0F, 0x0A, 0x1E), 0.0));
            bgGradient.GradientStops.Add(new GradientStop(Color.FromRgb(0x1B, 0x11, 0x38), 0.5));
            bgGradient.GradientStops.Add(new GradientStop(Color.FromRgb(0x28, 0x14, 0x54), 1.0));

            group.Children.Add(new GeometryDrawing
            {
                Brush = bgGradient,
                Geometry = new RectangleGeometry(new Rect(0, 0, width, height))
            });

            // 2. Ambient Vector Glow Rings
            var glowBrush = new RadialGradientBrush
            {
                Center = new Point(0.8, 0.3),
                GradientOrigin = new Point(0.8, 0.3),
                RadiusX = 0.5,
                RadiusY = 0.8
            };
            glowBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0x55, 0x8B, 0x5C, 0xF6), 0.0));
            glowBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x8B, 0x5C, 0xF6), 1.0));

            group.Children.Add(new GeometryDrawing
            {
                Brush = glowBrush,
                Geometry = new RectangleGeometry(new Rect(0, 0, width, height))
            });

            // 3. Vector Geometric Grid & Tunnel Lines
            var linePen = new Pen(new SolidColorBrush(Color.FromArgb(0x25, 0xA7, 0x8B, 0xFA)), 1.2);

            var pathGroup = new GeometryGroup();
            for (int x = -100; x < (int)width + 200; x += 40)
            {
                pathGroup.Children.Add(new LineGeometry(new Point(x, 0), new Point(x * 0.7 + width * 0.15, height)));
            }
            for (int y = 0; y <= (int)height; y += 30)
            {
                pathGroup.Children.Add(new LineGeometry(new Point(0, y), new Point(width, y)));
            }

            group.Children.Add(new GeometryDrawing
            {
                Pen = linePen,
                Geometry = pathGroup
            });

            // 4. Vector Logo Crescent Icon
            var moonGroup = new GeometryGroup();
            moonGroup.Children.Add(new EllipseGeometry(new Point(70, height / 2), 34, 34));

            var moonPen = new Pen(new SolidColorBrush(Color.FromRgb(0xC4, 0xB5, 0xFD)), 2.0);
            var moonFill = new SolidColorBrush(Color.FromArgb(0x40, 0x8B, 0x5C, 0xF6));

            group.Children.Add(new GeometryDrawing
            {
                Brush = moonFill,
                Pen = moonPen,
                Geometry = moonGroup
            });

            // 5. Vector Typography Title & Subtitle
            var formattedText = new FormattedText(
                "NEP TUNNEL",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                28,
                new SolidColorBrush(Color.FromRgb(0xF3, 0xE8, 0xFF)),
                1.0
            );

            var textGeometry = formattedText.BuildGeometry(new Point(120, height / 2 - 24));
            group.Children.Add(new GeometryDrawing
            {
                Brush = new SolidColorBrush(Color.FromRgb(0xF3, 0xE8, 0xFF)),
                Geometry = textGeometry
            });

            var subText = new FormattedText(
                "High Performance Roblox Studio Session Tunneling Engine",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                13,
                new SolidColorBrush(Color.FromRgb(0xA7, 0x8B, 0xFA)),
                1.0
            );

            var subGeometry = subText.BuildGeometry(new Point(120, height / 2 + 10));
            group.Children.Add(new GeometryDrawing
            {
                Brush = new SolidColorBrush(Color.FromRgb(0xA7, 0x8B, 0xFA)),
                Geometry = subGeometry
            });

            var drawingImage = new DrawingImage(group);
            drawingImage.Freeze();
            return drawingImage;
        }

        // Generates a crisp vector logo icon.
        public static DrawingImage CreateVectorLogo(double size = 96)
        {
            var group = new DrawingGroup();
            double center = size / 2;
            double radius = size * 0.35;

            // Outer Glowing Circle
            var outerBrush = new SolidColorBrush(Color.FromArgb(0x35, 0x8B, 0x5C, 0xF6));
            var outerPen = new Pen(new SolidColorBrush(Color.FromRgb(0x8B, 0x5C, 0xF6)), 1.5);
            group.Children.Add(new GeometryDrawing
            {
                Brush = outerBrush,
                Pen = outerPen,
                Geometry = new EllipseGeometry(new Point(center, center), radius + 8, radius + 8)
            });

            // Inner Moon Shape
            var moonPen = new Pen(new SolidColorBrush(Color.FromRgb(0xC4, 0xB5, 0xFD)), 2.0);
            var moonFill = new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED));
            group.Children.Add(new GeometryDrawing
            {
                Brush = moonFill,
                Pen = moonPen,
                Geometry = new EllipseGeometry(new Point(center, center), radius, radius)
            });

            var image = new DrawingImage(group);
            image.Freeze();
            return image;
        }
    }
}

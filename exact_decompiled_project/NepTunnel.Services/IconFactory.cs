using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NepTunnel.Services;

public static class IconFactory
{
	public static Geometry GetIconGeometry(string iconName)
	{
		return iconName switch
		{
			"host" => Geometry.Parse("M 3,8 L 3,14 L 13,14 L 13,8 M 5,7 L 8,3 L 11,7 M 8,3 L 8,11"), 
			"join" => Geometry.Parse("M 6,3 C 2,3 2,13 6,13 M 10,3 C 14,3 14,13 10,13 M 4,8 L 12,8"), 
			"echo" => Geometry.Parse("M 3,5 L 13,5 M 10,2 L 13,5 L 10,8 M 13,11 L 3,11 M 6,8 L 3,11 L 6,14"), 
			"map" => Geometry.Parse("M 2,2 L 14,2 L 14,14 L 2,14 Z M 8,2 L 8,14 M 2,8 L 14,8 M 11,5 A 1.2,1.2 0 1,1 11,4.99"), 
			"folder" => Geometry.Parse("M 2,3 L 6,3 L 8,5 L 14,5 L 14,13 L 2,13 Z"), 
			"play" => Geometry.Parse("M 4,2 L 14,8 L 4,14 Z"), 
			"back" => Geometry.Parse("M 12,2 L 4,8 L 12,14 Z"), 
			"stop" => Geometry.Parse("M 3,3 L 13,13 M 13,3 L 3,13"), 
			"test" => Geometry.Parse("M 3,12 L 3,14 M 7,8 L 7,14 M 11,4 L 11,14 M 2,14 L 14,14"), 
			"send" => Geometry.Parse("M 3,13 L 13,3 M 13,3 L 6,3 M 13,3 L 13,10"), 
			"trash" => Geometry.Parse("M 3,4 L 13,4 M 5,4 L 5,13 L 11,13 L 11,4 M 6,2 L 10,2"), 
			_ => Geometry.Parse("M 3,3 L 13,13 M 13,3 L 3,13"), 
		};
	}

	public static StackPanel CreateButtonContent(string iconName, string text, double size = 18.0)
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		if (!string.IsNullOrEmpty(iconName))
		{
			Path child = new Path
			{
				Data = GetIconGeometry(iconName),
				Stroke = new SolidColorBrush(Color.FromRgb(240, 230, byte.MaxValue)),
				StrokeThickness = 1.8,
				StrokeLineJoin = PenLineJoin.Round,
				StrokeStartLineCap = PenLineCap.Round,
				StrokeEndLineCap = PenLineCap.Round,
				Stretch = Stretch.Uniform,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			Viewbox element = new Viewbox
			{
				Width = size,
				Height = size,
				Margin = new Thickness(0.0, 0.0, 8.0, 0.0),
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
				Child = child
			};
			stackPanel.Children.Add(element);
		}
		if (!string.IsNullOrEmpty(text))
		{
			TextBlock element2 = new TextBlock
			{
				Text = text,
				FontSize = 14.0,
				FontWeight = FontWeights.Bold,
				Foreground = new SolidColorBrush(Color.FromRgb(240, 230, byte.MaxValue)),
				VerticalAlignment = VerticalAlignment.Center
			};
			stackPanel.Children.Add(element2);
		}
		return stackPanel;
	}
}

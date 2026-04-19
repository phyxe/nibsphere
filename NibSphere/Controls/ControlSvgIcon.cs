using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using Svg.Skia;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Resources;
using System.Xml.Linq;

namespace NibSphere.Controls
{
	public class ControlSvgIcon : UserControl
	{
		private readonly SKElement _surface;
		private readonly SKSvg _svg = new();

		public ControlSvgIcon()
		{
			IsHitTestVisible = false;

			_surface = new SKElement();
			_surface.PaintSurface += Surface_PaintSurface;

			Content = _surface;

			Loaded += (_, _) => LoadSvg();
			SizeChanged += (_, _) => _surface.InvalidateVisual();
		}

		public static readonly DependencyProperty SourceProperty =
			DependencyProperty.Register(
				nameof(Source),
				typeof(string),
				typeof(ControlSvgIcon),
				new PropertyMetadata(null, OnVisualPropertyChanged));

		public string? Source
		{
			get => (string?)GetValue(SourceProperty);
			set => SetValue(SourceProperty, value);
		}

		public static readonly DependencyProperty TintProperty =
			DependencyProperty.Register(
				nameof(Tint),
				typeof(Brush),
				typeof(ControlSvgIcon),
				new PropertyMetadata(Brushes.Black, OnVisualPropertyChanged));

		public Brush Tint
		{
			get => (Brush)GetValue(TintProperty);
			set => SetValue(TintProperty, value);
		}

		private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is ControlSvgIcon icon)
			{
				icon.LoadSvg();
			}
		}

		private void LoadSvg()
		{
			if (string.IsNullOrWhiteSpace(Source))
			{
				_surface.InvalidateVisual();
				return;
			}

			try
			{
				Uri uri = BuildUri(Source);
				StreamResourceInfo? resource = Application.GetResourceStream(uri);

				if (resource == null)
				{
					_surface.InvalidateVisual();
					return;
				}

				using StreamReader reader = new(resource.Stream, Encoding.UTF8);
				string svgText = reader.ReadToEnd();

				Color tintColor = (Tint as SolidColorBrush)?.Color ?? Colors.Black;
				string normalizedSvg = NormalizeSvgToSingleColor(svgText, tintColor);

				using MemoryStream stream = new(Encoding.UTF8.GetBytes(normalizedSvg));
				_svg.Load(stream);
			}
			catch
			{
			}

			_surface.InvalidateVisual();
		}

		private static Uri BuildUri(string source)
		{
			if (Uri.TryCreate(source, UriKind.Absolute, out Uri? absoluteUri))
			{
				return absoluteUri;
			}

			return new Uri(source, UriKind.Relative);
		}

		private void Surface_PaintSurface(object? sender, SKPaintSurfaceEventArgs e)
		{
			SKCanvas canvas = e.Surface.Canvas;
			canvas.Clear(SKColors.Transparent);

			SKPicture? picture = _svg.Picture;
			if (picture == null)
			{
				return;
			}

			SKRect bounds = picture.CullRect;
			if (bounds.Width <= 0 || bounds.Height <= 0)
			{
				return;
			}

			float scaleX = e.Info.Width / bounds.Width;
			float scaleY = e.Info.Height / bounds.Height;
			float scale = Math.Min(scaleX, scaleY);

			float translateX = (e.Info.Width - (bounds.Width * scale)) / 2f - (bounds.Left * scale);
			float translateY = (e.Info.Height - (bounds.Height * scale)) / 2f - (bounds.Top * scale);

			canvas.Save();
			canvas.Translate(translateX, translateY);
			canvas.Scale(scale);
			canvas.DrawPicture(picture);
			canvas.Restore();
		}

		private static string NormalizeSvgToSingleColor(string svgText, Color color)
		{
			XDocument doc = XDocument.Parse(svgText, LoadOptions.PreserveWhitespace);
			string hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

			if (doc.Root != null)
			{
				doc.Root.SetAttributeValue("color", hex);
			}

			foreach (XElement element in doc.Descendants())
			{
				NormalizePaintAttribute(element, "fill", hex);
				NormalizePaintAttribute(element, "stroke", hex);
				NormalizeColorAttribute(element, hex);
				NormalizeStyleAttribute(element, hex);
				ApplyDefaultPaintIfNeeded(element, hex);
			}

			return doc.ToString(SaveOptions.DisableFormatting);
		}

		private static void NormalizePaintAttribute(XElement element, string attributeName, string hex)
		{
			XAttribute? attr = element.Attribute(attributeName);
			if (attr == null)
			{
				return;
			}

			string value = attr.Value.Trim();

			if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			if (value.StartsWith("url(", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			attr.Value = hex;
		}

		private static void NormalizeColorAttribute(XElement element, string hex)
		{
			XAttribute? attr = element.Attribute("color");
			if (attr == null)
			{
				return;
			}

			attr.Value = hex;
		}

		private static void NormalizeStyleAttribute(XElement element, string hex)
		{
			XAttribute? styleAttr = element.Attribute("style");
			if (styleAttr == null)
			{
				return;
			}

			string[] parts = styleAttr.Value.Split(';', StringSplitOptions.RemoveEmptyEntries);
			List<string> rewritten = new();

			foreach (string part in parts)
			{
				int separatorIndex = part.IndexOf(':');
				if (separatorIndex < 0)
				{
					continue;
				}

				string name = part[..separatorIndex].Trim();
				string value = part[(separatorIndex + 1)..].Trim();

				if (name.Equals("fill", StringComparison.OrdinalIgnoreCase))
				{
					rewritten.Add(value.Equals("none", StringComparison.OrdinalIgnoreCase)
						? "fill:none"
						: $"fill:{hex}");
				}
				else if (name.Equals("stroke", StringComparison.OrdinalIgnoreCase))
				{
					rewritten.Add(value.Equals("none", StringComparison.OrdinalIgnoreCase)
						? "stroke:none"
						: $"stroke:{hex}");
				}
				else if (name.Equals("color", StringComparison.OrdinalIgnoreCase))
				{
					rewritten.Add($"color:{hex}");
				}
				else
				{
					rewritten.Add($"{name}:{value}");
				}
			}

			styleAttr.Value = string.Join(";", rewritten);
		}

		private static void ApplyDefaultPaintIfNeeded(XElement element, string hex)
		{
			string localName = element.Name.LocalName;

			if (!IsDrawableElement(localName))
			{
				return;
			}

			XAttribute? fillAttr = element.Attribute("fill");
			XAttribute? strokeAttr = element.Attribute("stroke");
			XAttribute? styleAttr = element.Attribute("style");

			bool hasFill = fillAttr != null;
			bool hasStroke = strokeAttr != null;
			bool hasStyledFillOrStroke =
				styleAttr != null &&
				(styleAttr.Value.Contains("fill:", StringComparison.OrdinalIgnoreCase) ||
				 styleAttr.Value.Contains("stroke:", StringComparison.OrdinalIgnoreCase));

			if (hasFill || hasStroke || hasStyledFillOrStroke)
			{
				return;
			}

			element.SetAttributeValue("fill", hex);
		}

		private static bool IsDrawableElement(string localName)
		{
			return localName.Equals("path", StringComparison.OrdinalIgnoreCase) ||
				   localName.Equals("circle", StringComparison.OrdinalIgnoreCase) ||
				   localName.Equals("ellipse", StringComparison.OrdinalIgnoreCase) ||
				   localName.Equals("polygon", StringComparison.OrdinalIgnoreCase) ||
				   localName.Equals("polyline", StringComparison.OrdinalIgnoreCase) ||
				   localName.Equals("rect", StringComparison.OrdinalIgnoreCase);
		}
	}
}
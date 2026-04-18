using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Svg.Skia;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Allied_Forms.Controls
{
	public partial class SkiaSvgIcon : UserControl
	{
		private readonly SKSvg _svg = new();

		public SkiaSvgIcon()
		{
			InitializeComponent();

			Loaded += (_, _) => LoadSvg();
			SizeChanged += (_, _) => IconSurface.InvalidateVisual();
		}

		public static readonly DependencyProperty SourceProperty =
			DependencyProperty.Register(
				nameof(Source),
				typeof(string),
				typeof(SkiaSvgIcon),
				new PropertyMetadata(null, OnSourceChanged));

		public string? Source
		{
			get => (string?)GetValue(SourceProperty);
			set => SetValue(SourceProperty, value);
		}

		public static readonly DependencyProperty TintProperty =
			DependencyProperty.Register(
				nameof(Tint),
				typeof(Brush),
				typeof(SkiaSvgIcon),
				new PropertyMetadata(Brushes.Black, OnTintChanged));

		public Brush Tint
		{
			get => (Brush)GetValue(TintProperty);
			set => SetValue(TintProperty, value);
		}

		private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is SkiaSvgIcon icon)
			{
				icon.LoadSvg();
			}
		}

		private static void OnTintChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is SkiaSvgIcon icon)
			{
				icon.IconSurface.InvalidateVisual();
			}
		}

		private void LoadSvg()
		{
			if (string.IsNullOrWhiteSpace(Source))
			{
				IconSurface.InvalidateVisual();
				return;
			}

			try
			{
				Uri uri = BuildUri(Source);
				var resource = Application.GetResourceStream(uri);

				if (resource == null)
				{
					IconSurface.InvalidateVisual();
					return;
				}

				using var stream = resource.Stream;
				_svg.Load(stream);
			}
			catch
			{
				// For now, fail silently so the app does not crash on bad SVG.
				// We can add debugging later if needed.
			}

			IconSurface.InvalidateVisual();
		}

		private static Uri BuildUri(string source)
		{
			if (Uri.TryCreate(source, UriKind.Absolute, out var absoluteUri))
			{
				return absoluteUri;
			}

			return new Uri(source, UriKind.Relative);
		}

		private void IconSurface_PaintSurface(object? sender, SKPaintSurfaceEventArgs e)
		{
			var canvas = e.Surface.Canvas;
			canvas.Clear(SKColors.Transparent);

			var picture = _svg.Picture;
			if (picture == null)
			{
				return;
			}

			var bounds = picture.CullRect;
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

			using var paint = CreateTintPaint();

			if (paint != null)
			{
				canvas.DrawPicture(picture, paint);
			}
			else
			{
				canvas.DrawPicture(picture);
			}

			canvas.Restore();
		}

		private SKPaint? CreateTintPaint()
		{
			if (Tint is not SolidColorBrush solidBrush)
			{
				return null;
			}

			var color = solidBrush.Color;

			return new SKPaint
			{
				IsAntialias = true,
				ColorFilter = SKColorFilter.CreateBlendMode(
					new SKColor(color.R, color.G, color.B, color.A),
					SKBlendMode.SrcIn)
			};
		}
	}
}
using System.Windows;

namespace NibSphere.Services
{
	public static class ThemeManager
	{
		private const string LightThemePath = "Themes/LightTheme.xaml";
		private const string DarkThemePath = "Themes/DarkTheme.xaml";

		public static void ApplyLightTheme()
		{
			ApplyTheme(LightThemePath);
		}

		public static void ApplyDarkTheme()
		{
			ApplyTheme(DarkThemePath);
		}

		private static void ApplyTheme(string themePath)
		{
			var appResources = Application.Current.Resources.MergedDictionaries;

			var existingTheme = appResources
				.FirstOrDefault(d =>
					d.Source != null &&
					(d.Source.OriginalString.EndsWith(LightThemePath, StringComparison.OrdinalIgnoreCase) ||
					 d.Source.OriginalString.EndsWith(DarkThemePath, StringComparison.OrdinalIgnoreCase)));

			if (existingTheme != null)
			{
				appResources.Remove(existingTheme);
			}

			appResources.Insert(1, new ResourceDictionary
			{
				Source = new Uri(themePath, UriKind.Relative)
			});
		}
	}
}
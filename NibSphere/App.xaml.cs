using NibSphere.Core.Interfaces;
using NibSphere.Data.Database;
using NibSphere.Data.Infrastructure;
using NibSphere.Data.Repositories;
using Microsoft.Win32;
using System.Windows;

namespace NibSphere
{
	public partial class App : Application
	{
		public static IAppPaths AppPaths { get; private set; } = null!;
		public static bool IsDarkTheme { get; private set; }

		protected override async void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			AppPaths = new AppPaths();

			var storageInitializer = new AppStorageInitializer(AppPaths);
			storageInitializer.EnsureDirectoriesExist();

			var databaseInitializer = new DatabaseInitializer(AppPaths);
			await databaseInitializer.InitializeAsync();

			bool useDarkTheme = await ResolveInitialThemeAsync();
			ApplyTheme(useDarkTheme);
		}

		public static void ApplyTheme(bool useDarkTheme)
		{
			ResourceDictionary themeDictionary = new ResourceDictionary
			{
				Source = new Uri(
					useDarkTheme
						? "Themes/DarkTheme.xaml"
						: "Themes/LightTheme.xaml",
					UriKind.Relative)
			};

			var mergedDictionaries = Current.Resources.MergedDictionaries;

			if (mergedDictionaries.Count > 2)
			{
				mergedDictionaries[2] = themeDictionary;
			}

			IsDarkTheme = useDarkTheme;
		}

		public static void ToggleTheme()
		{
			ApplyTheme(!IsDarkTheme);
		}

		private static async Task<bool> ResolveInitialThemeAsync()
		{
			var repository = new AppUserProfileRepository(AppPaths);
			var userProfile = await repository.GetPrimaryUserProfileAsync();

			string? preference = userProfile?.ThemePreference;

			if (string.Equals(preference, "Dark", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			if (string.Equals(preference, "Light", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			return IsSystemDarkMode();
		}

		private static bool IsSystemDarkMode()
		{
			const string personalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
			const string appsUseLightTheme = "AppsUseLightTheme";

			using RegistryKey? key = Registry.CurrentUser.OpenSubKey(personalizeKeyPath);

			object? value = key?.GetValue(appsUseLightTheme);

			if (value is int intValue)
			{
				return intValue == 0;
			}

			return false;
		}
	}
}
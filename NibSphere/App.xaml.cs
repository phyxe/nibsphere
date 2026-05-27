using Microsoft.Win32;
using NibSphere.Core.Interfaces;
using NibSphere.Data.Database;
using NibSphere.Data.Infrastructure;
using NibSphere.Data.Repositories;
using NibSphere.Modules;
using System.IO;
using System.Windows;

namespace NibSphere
{
	public partial class App : Application
	{
		public static IAppPaths AppPaths { get; private set; } = null!;
		public static ModuleCatalog ModulesCatalog { get; private set; } = null!;
		public static bool IsDarkTheme { get; private set; }

		protected override async void OnStartup(StartupEventArgs e)
		{

			AppPaths = new AppPaths();
			ModulesCatalog = ModuleCatalog.CreateDefault();

			var storageInitializer = new AppStorageInitializer(AppPaths);
			storageInitializer.EnsureDirectoriesExist();
			EnsureReferenceDataFiles();

			string cachedThemePreference = ResolveCachedThemePreference();
			ApplyThemePreference(cachedThemePreference);

			var databaseInitializer = new DatabaseInitializer(AppPaths);
			await databaseInitializer.InitializeAsync();

			await ModulesCatalog.InitializeDatabasesAsync(AppPaths);

			await ReconcileThemePreferenceCacheFromDatabaseAsync();

			base.OnStartup(e);
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
			string? preference = await ResolveDatabaseThemePreferenceAsync();
			return ResolveThemePreferenceToDark(preference);
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

		public static void ApplyThemePreference(string? themePreference)
		{
			bool useDarkTheme = ResolveThemePreferenceToDark(themePreference);
			ApplyTheme(useDarkTheme);
		}

		public static void SaveThemePreferenceCache(string? themePreference)
		{
			string normalizedPreference = NormalizeThemePreference(themePreference);

			Directory.CreateDirectory(AppPaths.ConfigDirectory);
			File.WriteAllText(GetThemePreferenceCachePath(), normalizedPreference);
		}

		private static string ResolveCachedThemePreference()
		{
			string cachePath = GetThemePreferenceCachePath();

			if (!File.Exists(cachePath))
			{
				return "System";
			}

			string value = File.ReadAllText(cachePath).Trim();
			return NormalizeThemePreference(value);
		}

		private static async Task<string?> ResolveDatabaseThemePreferenceAsync()
		{
			var repository = new AppUserProfileRepository(AppPaths);
			var userProfile = await repository.GetPrimaryUserProfileAsync();

			return userProfile?.ThemePreference;
		}

		private static async Task ReconcileThemePreferenceCacheFromDatabaseAsync()
		{
			string? databasePreference = await ResolveDatabaseThemePreferenceAsync();

			if (string.IsNullOrWhiteSpace(databasePreference))
			{
				return;
			}

			string normalizedPreference = NormalizeThemePreference(databasePreference);

			SaveThemePreferenceCache(normalizedPreference);
			ApplyThemePreference(normalizedPreference);
		}

		private static bool ResolveThemePreferenceToDark(string? themePreference)
		{
			string normalizedPreference = NormalizeThemePreference(themePreference);

			if (string.Equals(normalizedPreference, "Dark", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			if (string.Equals(normalizedPreference, "Light", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			return IsSystemDarkMode();
		}

		private static string NormalizeThemePreference(string? themePreference)
		{
			if (string.Equals(themePreference, "Dark", StringComparison.OrdinalIgnoreCase))
			{
				return "Dark";
			}

			if (string.Equals(themePreference, "Light", StringComparison.OrdinalIgnoreCase))
			{
				return "Light";
			}

			return "System";
		}

		private static string GetThemePreferenceCachePath()
		{
			return Path.Combine(AppPaths.ConfigDirectory, "theme-preference.txt");
		}

		private static void EnsureReferenceDataFiles()
		{
			string sourcePath = Path.Combine(
				AppContext.BaseDirectory,
				"Resources",
				"ReferenceData",
				"ph-addresses.json");

			string targetPath = AppPaths.PhilippineAddressDataFilePath;

			if (!File.Exists(sourcePath))
			{
				return;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

			if (!File.Exists(targetPath))
			{
				File.Copy(sourcePath, targetPath, overwrite: false);
			}
		}
	}
}
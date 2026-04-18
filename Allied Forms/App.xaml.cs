using AFCore.Interfaces;
using AFData.Database;
using AFData.Infrastructure;
using System.Windows;

namespace Allied_Forms
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

			ApplyTheme(false);
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
	}
}
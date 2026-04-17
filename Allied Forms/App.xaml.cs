using AFCore.Interfaces;
using AFData.Database;
using AFData.Infrastructure;
using System.Windows;

namespace Allied_Forms
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static IAppPaths AppPaths { get; private set; } = null!;

		protected override async void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			AppPaths = new AppPaths();

			var storageInitializer = new AppStorageInitializer(AppPaths);
			storageInitializer.EnsureDirectoriesExist();

			var databaseInitializer = new DatabaseInitializer(AppPaths);
			await databaseInitializer.InitializeAsync();
		}
	}

}

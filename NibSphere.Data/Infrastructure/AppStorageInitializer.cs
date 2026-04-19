using NibSphere.Core.Interfaces;
using System.IO;

namespace NibSphere.Data.Infrastructure
{
	public class AppStorageInitializer
	{
		private readonly IAppPaths _appPaths;

		public AppStorageInitializer(IAppPaths appPaths)
		{
			_appPaths = appPaths;
		}

		public void EnsureDirectoriesExist()
		{
			Directory.CreateDirectory(_appPaths.RootDirectory);
			Directory.CreateDirectory(_appPaths.DataDirectory);
			Directory.CreateDirectory(_appPaths.BackupDirectory);
			Directory.CreateDirectory(_appPaths.ExportDirectory);
			Directory.CreateDirectory(_appPaths.LogDirectory);
			Directory.CreateDirectory(_appPaths.ConfigDirectory);
			Directory.CreateDirectory(_appPaths.ReferenceDataDirectory);

			Directory.CreateDirectory(_appPaths.StorageDirectory);
			Directory.CreateDirectory(_appPaths.ImagesDirectory);
			Directory.CreateDirectory(_appPaths.DocumentsDirectory);
			Directory.CreateDirectory(_appPaths.UserProfileImagesDirectory);
			Directory.CreateDirectory(_appPaths.ModuleImagesDirectory);
			Directory.CreateDirectory(_appPaths.ModuleDocumentsDirectory);
		}
	}
}
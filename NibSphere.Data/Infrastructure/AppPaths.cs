using NibSphere.Core.Interfaces;
using System.IO;

namespace NibSphere.Data.Infrastructure
{
	public class AppPaths : IAppPaths
	{
		public string RootDirectory { get; }

		public string DataDirectory => Path.Combine(RootDirectory, "Data");
		public string BackupDirectory => Path.Combine(RootDirectory, "Backups");
		public string ExportDirectory => Path.Combine(RootDirectory, "Exports");
		public string LogDirectory => Path.Combine(RootDirectory, "Logs");
		public string ConfigDirectory => Path.Combine(RootDirectory, "Config");

		public string ReferenceDataDirectory => Path.Combine(ConfigDirectory, "ReferenceData");
		public string PhilippineAddressDataFilePath => Path.Combine(ReferenceDataDirectory, "ph-addresses.json");

		public string StorageDirectory => Path.Combine(RootDirectory, "Storage");
		public string ImagesDirectory => Path.Combine(StorageDirectory, "Images");
		public string DocumentsDirectory => Path.Combine(StorageDirectory, "Documents");

		public string UserProfileImagesDirectory => Path.Combine(ImagesDirectory, "UserProfiles");
		public string SchoolLogoImagesDirectory => Path.Combine(ImagesDirectory, "SchoolLogos");
		public string ModuleImagesDirectory => Path.Combine(ImagesDirectory, "Modules");
		public string ModuleDocumentsDirectory => Path.Combine(DocumentsDirectory, "Modules");

		public string DatabaseFilePath => Path.Combine(DataDirectory, "NibSphere.mdf");

		public AppPaths()
		{
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			RootDirectory = Path.Combine(localAppData, "NibSphere");
		}
	}
}
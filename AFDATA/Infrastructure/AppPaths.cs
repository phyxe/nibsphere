using AFCore.Interfaces;
using System.IO;

namespace AFData.Infrastructure
{
	public class AppPaths : IAppPaths
	{
		public string RootDirectory { get; }
		public string DataDirectory => Path.Combine(RootDirectory, "Data");
		public string BackupDirectory => Path.Combine(RootDirectory, "Backups");
		public string ExportDirectory => Path.Combine(RootDirectory, "Exports");
		public string LogDirectory => Path.Combine(RootDirectory, "Logs");
		public string ConfigDirectory => Path.Combine(RootDirectory, "Config");
		public string DatabaseFilePath => Path.Combine(DataDirectory, "NibSphere.mdf");

		public AppPaths()
		{
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			RootDirectory = Path.Combine(localAppData, "NibSphere");
		}
	}
}
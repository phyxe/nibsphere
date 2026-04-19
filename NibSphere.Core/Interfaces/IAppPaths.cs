namespace NibSphere.Core.Interfaces
{
	public interface IAppPaths
	{
		string RootDirectory { get; }
		string DataDirectory { get; }
		string BackupDirectory { get; }
		string ExportDirectory { get; }
		string LogDirectory { get; }
		string ConfigDirectory { get; }

		string StorageDirectory { get; }
		string ImagesDirectory { get; }
		string DocumentsDirectory { get; }
		string UserProfileImagesDirectory { get; }
		string ModuleImagesDirectory { get; }
		string ModuleDocumentsDirectory { get; }

		string DatabaseFilePath { get; }
	}
}
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
		string DatabaseFilePath { get; }
	}
}
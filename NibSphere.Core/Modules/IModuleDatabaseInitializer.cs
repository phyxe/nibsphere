using NibSphere.Core.Interfaces;

namespace NibSphere.Core.Modules
{
	public interface IModuleDatabaseInitializer
	{
		string ModuleKey { get; }

		int SortOrder { get; }

		int SchemaVersion { get; }

		Task InitializeAsync(
			IAppPaths appPaths,
			CancellationToken cancellationToken = default);
	}
}
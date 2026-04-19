using NibSphere.Core.Interfaces;

namespace NibSphere.Data.Database
{
	public class DatabaseFileHelper
	{
		private readonly IAppPaths _appPaths;

		public DatabaseFileHelper(IAppPaths appPaths)
		{
			_appPaths = appPaths;
		}

		public string GetDatabaseFilePath()
		{
			return _appPaths.DatabaseFilePath;
		}
	}
}
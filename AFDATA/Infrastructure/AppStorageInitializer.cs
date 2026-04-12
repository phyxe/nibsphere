using System.IO;
using AFCore.Interfaces;

namespace AFData.Infrastructure
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
        }
    }
}
using NibSphere.Core.Interfaces;
using Microsoft.Data.SqlClient;

namespace NibSphere.Data.Database
{
	public class LocalDbConnectionFactory
	{
		private readonly IAppPaths _appPaths;

		public LocalDbConnectionFactory(IAppPaths appPaths)
		{
			_appPaths = appPaths;
		}

		public SqlConnection CreateMasterConnection()
		{
			string connectionString =
				@"Data Source=(LocalDB)\MSSQLLocalDB;Integrated Security=True;Connect Timeout=30";

			return new SqlConnection(connectionString);
		}

		public SqlConnection CreateAppConnection()
		{
			SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
			{
				DataSource = @"(LocalDB)\MSSQLLocalDB",
				AttachDBFilename = _appPaths.DatabaseFilePath,
				IntegratedSecurity = true,
				ConnectTimeout = 30
			};

			return new SqlConnection(builder.ConnectionString);
		}
	}
}
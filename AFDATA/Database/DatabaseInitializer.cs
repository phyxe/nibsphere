using AFCore.Interfaces;
using Microsoft.Data.SqlClient;
using System.IO;

namespace AFData.Database
{
	public class DatabaseInitializer
	{
		private readonly IAppPaths _appPaths;
		private readonly LocalDbConnectionFactory _connectionFactory;

		public DatabaseInitializer(IAppPaths appPaths)
		{
			_appPaths = appPaths;
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task InitializeAsync()
		{
			if (!File.Exists(_appPaths.DatabaseFilePath))
			{
				await CreateDatabaseAsync();
			}

			await CreateTablesAsync();
		}

		private async Task CreateDatabaseAsync()
		{
			string databaseName = "NibSphere";
			string logFilePath = Path.Combine(_appPaths.DataDirectory, "NibSphere_log.ldf");

			string createDatabaseSql =
				$"""
                CREATE DATABASE [{databaseName}]
                ON PRIMARY
                (
                    NAME = N'{databaseName}',
                    FILENAME = '{_appPaths.DatabaseFilePath}'
                )
                LOG ON
                (
                    NAME = N'{databaseName}_Log',
                    FILENAME = '{logFilePath}'
                );
                """;

			using SqlConnection connection = _connectionFactory.CreateMasterConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(createDatabaseSql, connection);
			await command.ExecuteNonQueryAsync();
		}

		private async Task CreateTablesAsync()
		{
			string sql =
				"""
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SchoolProfile')
                BEGIN
                    CREATE TABLE SchoolProfile
                    (
                        Id INT PRIMARY KEY IDENTITY(1,1),
                        SchoolName NVARCHAR(200) NOT NULL,
                        SchoolId NVARCHAR(100) NULL,
                        Region NVARCHAR(150) NULL,
                        Division NVARCHAR(150) NULL,
                        District NVARCHAR(150) NULL,
                        SchoolHeadName NVARCHAR(150) NULL,
                        SchoolHeadPosition NVARCHAR(150) NULL,
                        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME2 NULL
                    );
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppUserProfile')
                BEGIN
                    CREATE TABLE AppUserProfile
                    (
                        Id INT PRIMARY KEY IDENTITY(1,1),
                        FullName NVARCHAR(150) NOT NULL,
                        PositionTitle NVARCHAR(150) NULL,
                        EmailAddress NVARCHAR(150) NULL,
                        ContactNumber NVARCHAR(50) NULL,
                        SignaturePath NVARCHAR(500) NULL,
                        ThemePreference NVARCHAR(20) NULL,
                        IsPrimary BIT NOT NULL DEFAULT 1,
                        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME2 NULL
                    );
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LearningArea')
                BEGIN
                    CREATE TABLE LearningArea
                    (
                        Id INT PRIMARY KEY IDENTITY(1,1),
                        Category NVARCHAR(100) NOT NULL,
                        Code NVARCHAR(50) NOT NULL,
                        Description NVARCHAR(200) NOT NULL,
                        Sort INT NOT NULL DEFAULT 0,
                        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        UpdatedAt DATETIME2 NULL
                    );
                END
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			await command.ExecuteNonQueryAsync();
		}
	}
}
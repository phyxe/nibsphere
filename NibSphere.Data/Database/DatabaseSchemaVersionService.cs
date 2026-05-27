using Microsoft.Data.SqlClient;

namespace NibSphere.Data.Database
{
	public sealed class DatabaseSchemaVersionService
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public DatabaseSchemaVersionService(LocalDbConnectionFactory connectionFactory)
		{
			_connectionFactory = connectionFactory;
		}

		public async Task EnsureVersionTableAsync(CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
				IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AppSchemaVersion')
				BEGIN
					CREATE TABLE AppSchemaVersion
					(
						ModuleKey NVARCHAR(100) NOT NULL PRIMARY KEY,
						SchemaVersion INT NOT NULL,
						CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
						UpdatedAt DATETIME2 NULL
					);
				END
				""";

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		public async Task<bool> IsCurrentAsync(
			string moduleKey,
			int expectedVersion,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
				SELECT SchemaVersion
				FROM AppSchemaVersion
				WHERE ModuleKey = @ModuleKey;
				""";

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@ModuleKey", moduleKey);

			object? result = await command.ExecuteScalarAsync(cancellationToken);

			return result is int actualVersion && actualVersion >= expectedVersion;
		}

		public async Task SetVersionAsync(
			string moduleKey,
			int schemaVersion,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
				IF EXISTS
				(
					SELECT 1
					FROM AppSchemaVersion
					WHERE ModuleKey = @ModuleKey
				)
				BEGIN
					UPDATE AppSchemaVersion
					SET
						SchemaVersion = @SchemaVersion,
						UpdatedAt = GETDATE()
					WHERE ModuleKey = @ModuleKey;
				END
				ELSE
				BEGIN
					INSERT INTO AppSchemaVersion
					(
						ModuleKey,
						SchemaVersion
					)
					VALUES
					(
						@ModuleKey,
						@SchemaVersion
					);
				END
				""";

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@ModuleKey", moduleKey);
			command.Parameters.AddWithValue("@SchemaVersion", schemaVersion);

			await command.ExecuteNonQueryAsync(cancellationToken);
		}
	}
}
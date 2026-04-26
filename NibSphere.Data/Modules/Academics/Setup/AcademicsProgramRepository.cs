using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Academics.Setup;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Academics.Setup
{
	public sealed class AcademicsProgramRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public AcademicsProgramRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<AcademicsProgram>> GetAllAsync(
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    Id,
                    Code,
                    Name,
                    Description,
                    SortOrder,
                    IsActive
                FROM Academics_Program
                WHERE @IncludeInactive = 1
                   OR IsActive = 1
                ORDER BY
                    IsActive DESC,
                    SortOrder,
                    Name,
                    Id;
                """;

			List<AcademicsProgram> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				items.Add(Map(reader));
			}

			return items;
		}

		public async Task<AcademicsProgram?> GetByIdAsync(
			int id,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    Id,
                    Code,
                    Name,
                    Description,
                    SortOrder,
                    IsActive
                FROM Academics_Program
                WHERE Id = @Id;
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@Id", id);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			if (await reader.ReadAsync(cancellationToken))
			{
				return Map(reader);
			}

			return null;
		}

		public async Task<int> InsertAsync(
			AcademicsProgram program,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                INSERT INTO Academics_Program
                (
                    Code,
                    Name,
                    Description,
                    SortOrder,
                    IsActive
                )
                VALUES
                (
                    @Code,
                    @Name,
                    @Description,
                    @SortOrder,
                    @IsActive
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			PrepareForSave(program);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddParameters(command, program);

			object? result = await command.ExecuteScalarAsync(cancellationToken);
			return result is int id ? id : 0;
		}

		public async Task UpdateAsync(
			AcademicsProgram program,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_Program
                SET
                    Code = @Code,
                    Name = @Name,
                    Description = @Description,
                    SortOrder = @SortOrder,
                    IsActive = @IsActive,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			PrepareForSave(program);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddParameters(command, program);
			command.Parameters.AddWithValue("@Id", program.Id);

			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		public async Task SetIsActiveAsync(
			int id,
			bool isActive,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_Program
                SET
                    IsActive = @IsActive,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@Id", id);
			command.Parameters.AddWithValue("@IsActive", isActive);

			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		private static AcademicsProgram Map(SqlDataReader reader)
		{
			return new AcademicsProgram
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				Code = reader["Code"] as string ?? string.Empty,
				Name = reader["Name"] as string ?? string.Empty,
				Description = reader["Description"] as string ?? string.Empty,
				SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
			};
		}

		private static void PrepareForSave(AcademicsProgram program)
		{
			program.Code = NormalizeCode(program.Code);
			program.Name = NormalizeRequired(program.Name);
			program.Description = Normalize(program.Description) ?? string.Empty;
		}

		private static void AddParameters(SqlCommand command, AcademicsProgram program)
		{
			command.Parameters.AddWithValue("@Code", program.Code);
			command.Parameters.AddWithValue("@Name", program.Name);
			command.Parameters.AddWithValue("@Description", ToDbNullable(program.Description));
			command.Parameters.AddWithValue("@SortOrder", program.SortOrder);
			command.Parameters.AddWithValue("@IsActive", program.IsActive);
		}

		private static object ToDbNullable(string? value)
		{
			return string.IsNullOrWhiteSpace(value)
				? DBNull.Value
				: value.Trim();
		}

		private static string? Normalize(string? value)
		{
			return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
		}

		private static string NormalizeRequired(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
		}

		private static string NormalizeCode(string value)
		{
			string normalized = string.IsNullOrWhiteSpace(value)
				? string.Empty
				: value.Trim().ToUpperInvariant();

			normalized = normalized.Replace(" ", "_");
			normalized = normalized.Replace("-", "_");

			while (normalized.Contains("__", StringComparison.Ordinal))
			{
				normalized = normalized.Replace("__", "_");
			}

			return normalized;
		}
	}
}
using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Academics.Setup;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Academics.Setup
{
	public sealed class AcademicsEnrollmentStatusRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public AcademicsEnrollmentStatusRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<AcademicsEnrollmentStatus>> GetAllAsync(
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    Id,
                    Code,
                    Name,
                    SortOrder,
                    IsActive
                FROM Academics_EnrollmentStatus
                WHERE @IncludeInactive = 1
                   OR IsActive = 1
                ORDER BY
                    IsActive DESC,
                    SortOrder,
                    Name,
                    Id;
                """;

			List<AcademicsEnrollmentStatus> items = new();

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

		public async Task<int> InsertAsync(
			AcademicsEnrollmentStatus status,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                INSERT INTO Academics_EnrollmentStatus
                (
                    Code,
                    Name,
                    SortOrder,
                    IsActive
                )
                VALUES
                (
                    @Code,
                    @Name,
                    @SortOrder,
                    @IsActive
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			PrepareForSave(status);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddParameters(command, status);

			object? result = await command.ExecuteScalarAsync(cancellationToken);
			return result is int id ? id : 0;
		}

		public async Task UpdateAsync(
			AcademicsEnrollmentStatus status,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_EnrollmentStatus
                SET
                    Code = @Code,
                    Name = @Name,
                    SortOrder = @SortOrder,
                    IsActive = @IsActive,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			PrepareForSave(status);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddParameters(command, status);
			command.Parameters.AddWithValue("@Id", status.Id);

			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		public async Task SetIsActiveAsync(
			int id,
			bool isActive,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_EnrollmentStatus
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

		private static AcademicsEnrollmentStatus Map(SqlDataReader reader)
		{
			return new AcademicsEnrollmentStatus
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				Code = reader["Code"] as string ?? string.Empty,
				Name = reader["Name"] as string ?? string.Empty,
				SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
			};
		}

		private static void PrepareForSave(AcademicsEnrollmentStatus status)
		{
			status.Code = NormalizeCode(status.Code);
			status.Name = NormalizeRequired(status.Name);
		}

		private static void AddParameters(SqlCommand command, AcademicsEnrollmentStatus status)
		{
			command.Parameters.AddWithValue("@Code", status.Code);
			command.Parameters.AddWithValue("@Name", status.Name);
			command.Parameters.AddWithValue("@SortOrder", status.SortOrder);
			command.Parameters.AddWithValue("@IsActive", status.IsActive);
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

		private static string NormalizeRequired(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
		}
	}
}
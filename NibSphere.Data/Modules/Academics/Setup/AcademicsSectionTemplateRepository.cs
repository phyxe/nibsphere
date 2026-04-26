using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Academics.Setup;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Academics.Setup
{
	public sealed class AcademicsSectionTemplateRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public AcademicsSectionTemplateRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<AcademicsSectionTemplate>> GetAllAsync(
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    Id,
                    GradeLevelName,
                    SectionName,
                    SortOrder,
                    IsActive
                FROM Academics_SectionTemplate
                WHERE @IncludeInactive = 1
                   OR IsActive = 1
                ORDER BY
                    IsActive DESC,
                    SortOrder,
                    GradeLevelName,
                    SectionName,
                    Id;
                """;

			List<AcademicsSectionTemplate> items = new();

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

		public async Task<AcademicsSectionTemplate?> GetByIdAsync(
			int id,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    Id,
                    GradeLevelName,
                    SectionName,
                    SortOrder,
                    IsActive
                FROM Academics_SectionTemplate
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
			AcademicsSectionTemplate sectionTemplate,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                INSERT INTO Academics_SectionTemplate
                (
                    GradeLevelName,
                    SectionName,
                    SortOrder,
                    IsActive
                )
                VALUES
                (
                    @GradeLevelName,
                    @SectionName,
                    @SortOrder,
                    @IsActive
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			PrepareForSave(sectionTemplate);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddParameters(command, sectionTemplate);

			object? result = await command.ExecuteScalarAsync(cancellationToken);
			return result is int id ? id : 0;
		}

		public async Task UpdateAsync(
			AcademicsSectionTemplate sectionTemplate,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_SectionTemplate
                SET
                    GradeLevelName = @GradeLevelName,
                    SectionName = @SectionName,
                    SortOrder = @SortOrder,
                    IsActive = @IsActive,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			PrepareForSave(sectionTemplate);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddParameters(command, sectionTemplate);
			command.Parameters.AddWithValue("@Id", sectionTemplate.Id);

			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		public async Task SetIsActiveAsync(
			int id,
			bool isActive,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_SectionTemplate
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

		private static AcademicsSectionTemplate Map(SqlDataReader reader)
		{
			return new AcademicsSectionTemplate
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				GradeLevelName = reader["GradeLevelName"] as string ?? string.Empty,
				SectionName = reader["SectionName"] as string ?? string.Empty,
				SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
			};
		}

		private static void PrepareForSave(AcademicsSectionTemplate sectionTemplate)
		{
			sectionTemplate.GradeLevelName = NormalizeRequired(sectionTemplate.GradeLevelName);
			sectionTemplate.SectionName = NormalizeRequired(sectionTemplate.SectionName);
			sectionTemplate.SortOrder = Math.Max(0, sectionTemplate.SortOrder);
		}

		private static void AddParameters(
			SqlCommand command,
			AcademicsSectionTemplate sectionTemplate)
		{
			command.Parameters.AddWithValue("@GradeLevelName", sectionTemplate.GradeLevelName);
			command.Parameters.AddWithValue("@SectionName", sectionTemplate.SectionName);
			command.Parameters.AddWithValue("@SortOrder", sectionTemplate.SortOrder);
			command.Parameters.AddWithValue("@IsActive", sectionTemplate.IsActive);
		}

		private static string NormalizeRequired(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
		}
	}
}
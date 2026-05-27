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
                    sectionTemplate.Id,
                    sectionTemplate.GradeLevelName,
                    sectionTemplate.SectionName,
                    sectionTemplate.SortOrder,
                    sectionTemplate.IsActive,
                    (
                        SELECT COUNT(1)
                        FROM Academics_SchoolYearSection section
                        WHERE section.SourceSectionTemplateId = sectionTemplate.Id
                    ) AS DependentRecordCount
                FROM Academics_SectionTemplate sectionTemplate
                WHERE @IncludeInactive = 1
                   OR sectionTemplate.IsActive = 1
                ORDER BY
                    sectionTemplate.IsActive DESC,
                    sectionTemplate.SortOrder,
                    sectionTemplate.GradeLevelName,
                    sectionTemplate.SectionName,
                    sectionTemplate.Id;
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
                    sectionTemplate.Id,
                    sectionTemplate.GradeLevelName,
                    sectionTemplate.SectionName,
                    sectionTemplate.SortOrder,
                    sectionTemplate.IsActive,
                    (
                        SELECT COUNT(1)
                        FROM Academics_SchoolYearSection section
                        WHERE section.SourceSectionTemplateId = sectionTemplate.Id
                    ) AS DependentRecordCount
                FROM Academics_SectionTemplate sectionTemplate
                WHERE sectionTemplate.Id = @Id;
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

			await ValidateGradeLevelAsync(
				connection,
				sectionTemplate.GradeLevelName,
				cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddParameters(command, sectionTemplate);

			object? result = await command.ExecuteScalarAsync(cancellationToken);
			return result is int id ? id : 0;
		}

		public async Task UpdateAsync(
			AcademicsSectionTemplate sectionTemplate,
			CancellationToken cancellationToken = default)
		{
			const string currentSql =
				"""
                SELECT GradeLevelName
                FROM Academics_SectionTemplate
                WHERE Id = @Id;
                """;

			const string updateSql =
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

			await ValidateGradeLevelAsync(
				connection,
				sectionTemplate.GradeLevelName,
				cancellationToken);

			using SqlCommand currentCommand = new(currentSql, connection);
			currentCommand.Parameters.AddWithValue("@Id", sectionTemplate.Id);

			object? currentGradeLevelNameResult = await currentCommand.ExecuteScalarAsync(cancellationToken);

			if (currentGradeLevelNameResult == null || currentGradeLevelNameResult == DBNull.Value)
			{
				throw new InvalidOperationException("Section template was not found.");
			}

			string currentGradeLevelName = Convert.ToString(currentGradeLevelNameResult) ?? string.Empty;

			if (!string.Equals(currentGradeLevelName, sectionTemplate.GradeLevelName, StringComparison.OrdinalIgnoreCase))
			{
				int dependentRecordCount = await CountDependentRecordsAsync(
					connection,
					sectionTemplate.Id,
					cancellationToken);

				if (dependentRecordCount > 0)
				{
					throw new InvalidOperationException("Section template grade level cannot be changed because school-year sections already use this template.");
				}
			}

			using SqlCommand command = new(updateSql, connection);
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

		public async Task DeleteAsync(
			int id,
			CancellationToken cancellationToken = default)
		{
			const string existsSql =
				"""
                SELECT COUNT(1)
                FROM Academics_SectionTemplate
                WHERE Id = @Id;
                """;

			const string deleteSql =
				"""
                DELETE FROM Academics_SectionTemplate
                WHERE Id = @Id;
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand existsCommand = new(existsSql, connection);
			existsCommand.Parameters.AddWithValue("@Id", id);

			int sectionTemplateCount = Convert.ToInt32(
				await existsCommand.ExecuteScalarAsync(cancellationToken));

			if (sectionTemplateCount == 0)
			{
				throw new InvalidOperationException("Section template was not found.");
			}

			int dependentRecordCount = await CountDependentRecordsAsync(
				connection,
				id,
				cancellationToken);

			if (dependentRecordCount > 0)
			{
				throw new InvalidOperationException("Section template cannot be deleted because school-year sections already use it.");
			}

			using SqlCommand deleteCommand = new(deleteSql, connection);
			deleteCommand.Parameters.AddWithValue("@Id", id);

			await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
		}

		public async Task<List<string>> GetGradeLevelNameOptionsAsync(
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT Name
                FROM Academics_GradeLevel
                WHERE @IncludeInactive = 1
                   OR IsActive = 1
                ORDER BY
                    IsActive DESC,
                    SortOrder,
                    Name,
                    Id;
                """;

			List<string> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				items.Add(reader["Name"] as string ?? string.Empty);
			}

			return items;
		}

		private static AcademicsSectionTemplate Map(SqlDataReader reader)
		{
			int dependentRecordCount = reader["DependentRecordCount"] == DBNull.Value
				? 0
				: Convert.ToInt32(reader["DependentRecordCount"]);

			return new AcademicsSectionTemplate
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				GradeLevelName = reader["GradeLevelName"] as string ?? string.Empty,
				SectionName = reader["SectionName"] as string ?? string.Empty,
				SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
				DependentRecordCount = dependentRecordCount,
				IsEditable = dependentRecordCount == 0,
				IsDeletable = dependentRecordCount == 0
			};
		}

		private static async Task ValidateGradeLevelAsync(
			SqlConnection connection,
			string gradeLevelName,
			CancellationToken cancellationToken)
		{
			const string sql =
				"""
                SELECT COUNT(1)
                FROM Academics_GradeLevel
                WHERE Name = @Name
                  AND IsActive = 1;
                """;

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@Name", gradeLevelName);

			int count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));

			if (count == 0)
			{
				throw new InvalidOperationException("Select an active grade level.");
			}
		}

		private static async Task<int> CountDependentRecordsAsync(
			SqlConnection connection,
			int sectionTemplateId,
			CancellationToken cancellationToken)
		{
			const string sql =
				"""
                SELECT COUNT(1)
                FROM Academics_SchoolYearSection
                WHERE SourceSectionTemplateId = @SectionTemplateId;
                """;

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@SectionTemplateId", sectionTemplateId);

			object? result = await command.ExecuteScalarAsync(cancellationToken);

			return result == null || result == DBNull.Value
				? 0
				: Convert.ToInt32(result);
		}

		private static void PrepareForSave(AcademicsSectionTemplate sectionTemplate)
		{
			sectionTemplate.GradeLevelName = NormalizeRequired(sectionTemplate.GradeLevelName);
			sectionTemplate.SectionName = NormalizeRequired(sectionTemplate.SectionName);
			sectionTemplate.SortOrder = Math.Max(0, sectionTemplate.SortOrder);

			if (string.IsNullOrWhiteSpace(sectionTemplate.GradeLevelName))
			{
				throw new InvalidOperationException("Grade level is required.");
			}

			if (string.IsNullOrWhiteSpace(sectionTemplate.SectionName))
			{
				throw new InvalidOperationException("Section name is required.");
			}
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
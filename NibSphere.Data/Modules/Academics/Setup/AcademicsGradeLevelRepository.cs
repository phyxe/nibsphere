using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Academics.Setup;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Academics.Setup
{
	public sealed class AcademicsGradeLevelRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public AcademicsGradeLevelRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<AcademicsGradeLevel>> GetAllAsync(
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    gl.Id,
                    gl.Name,
                    gl.ShortName,
                    gl.SortOrder,
                    gl.IsActive,
                    (
                        (
                            SELECT COUNT(1)
                            FROM Academics_SectionTemplate sectionTemplate
                            WHERE sectionTemplate.GradeLevelName = gl.Name
                        )
                        +
                        (
                            SELECT COUNT(1)
                            FROM Academics_ProgramProspectusLine prospectusLine
                            WHERE prospectusLine.GradeLevelName = gl.Name
                        )
                        +
                        (
                            SELECT COUNT(1)
                            FROM Academics_SchoolYearSection section
                            WHERE section.GradeLevelName = gl.Name
                        )
                        +
                        (
                            SELECT COUNT(1)
                            FROM Academics_SchoolYearProgramLine programLine
                            WHERE programLine.GradeLevelName = gl.Name
                        )
                        +
                        (
                            SELECT COUNT(1)
                            FROM Academics_Enrollment enrollment
                            WHERE enrollment.GradeLevelName = gl.Name
                        )
                    ) AS DependentRecordCount
                FROM Academics_GradeLevel gl
                WHERE @IncludeInactive = 1
                   OR gl.IsActive = 1
                ORDER BY
                    gl.IsActive DESC,
                    gl.SortOrder,
                    gl.Name,
                    gl.Id;
                """;

			List<AcademicsGradeLevel> items = new();

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

		public async Task<AcademicsGradeLevel?> GetByIdAsync(
			int id,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    gl.Id,
                    gl.Name,
                    gl.ShortName,
                    gl.SortOrder,
                    gl.IsActive,
                    (
                        (
                            SELECT COUNT(1)
                            FROM Academics_SectionTemplate sectionTemplate
                            WHERE sectionTemplate.GradeLevelName = gl.Name
                        )
                        +
                        (
                            SELECT COUNT(1)
                            FROM Academics_ProgramProspectusLine prospectusLine
                            WHERE prospectusLine.GradeLevelName = gl.Name
                        )
                        +
                        (
                            SELECT COUNT(1)
                            FROM Academics_SchoolYearSection section
                            WHERE section.GradeLevelName = gl.Name
                        )
                        +
                        (
                            SELECT COUNT(1)
                            FROM Academics_SchoolYearProgramLine programLine
                            WHERE programLine.GradeLevelName = gl.Name
                        )
                        +
                        (
                            SELECT COUNT(1)
                            FROM Academics_Enrollment enrollment
                            WHERE enrollment.GradeLevelName = gl.Name
                        )
                    ) AS DependentRecordCount
                FROM Academics_GradeLevel gl
                WHERE gl.Id = @Id;
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
			AcademicsGradeLevel gradeLevel,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                INSERT INTO Academics_GradeLevel
                (
                    Name,
                    ShortName,
                    SortOrder,
                    IsActive
                )
                VALUES
                (
                    @Name,
                    @ShortName,
                    @SortOrder,
                    @IsActive
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			PrepareForSave(gradeLevel);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddParameters(command, gradeLevel);

			object? result = await command.ExecuteScalarAsync(cancellationToken);
			return result is int id ? id : 0;
		}

		public async Task UpdateAsync(
			AcademicsGradeLevel gradeLevel,
			CancellationToken cancellationToken = default)
		{
			const string currentSql =
				"""
                SELECT Name
                FROM Academics_GradeLevel
                WHERE Id = @Id;
                """;

			const string updateSql =
				"""
                UPDATE Academics_GradeLevel
                SET
                    Name = @Name,
                    ShortName = @ShortName,
                    SortOrder = @SortOrder,
                    IsActive = @IsActive,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			PrepareForSave(gradeLevel);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand currentCommand = new(currentSql, connection);
			currentCommand.Parameters.AddWithValue("@Id", gradeLevel.Id);

			object? currentNameResult = await currentCommand.ExecuteScalarAsync(cancellationToken);

			if (currentNameResult == null || currentNameResult == DBNull.Value)
			{
				throw new InvalidOperationException("Grade level was not found.");
			}

			string currentName = Convert.ToString(currentNameResult) ?? string.Empty;

			if (!string.Equals(currentName, gradeLevel.Name, StringComparison.OrdinalIgnoreCase))
			{
				int dependentRecordCount = await CountDependentRecordsAsync(
					connection,
					currentName,
					cancellationToken);

				if (dependentRecordCount > 0)
				{
					throw new InvalidOperationException("Grade level name cannot be changed because records already use it.");
				}
			}

			using SqlCommand updateCommand = new(updateSql, connection);
			AddParameters(updateCommand, gradeLevel);
			updateCommand.Parameters.AddWithValue("@Id", gradeLevel.Id);

			await updateCommand.ExecuteNonQueryAsync(cancellationToken);
		}

		public async Task SetIsActiveAsync(
			int id,
			bool isActive,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_GradeLevel
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
			const string currentSql =
				"""
                SELECT Name
                FROM Academics_GradeLevel
                WHERE Id = @Id;
                """;

			const string deleteSql =
				"""
                DELETE FROM Academics_GradeLevel
                WHERE Id = @Id;
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand currentCommand = new(currentSql, connection);
			currentCommand.Parameters.AddWithValue("@Id", id);

			object? currentNameResult = await currentCommand.ExecuteScalarAsync(cancellationToken);

			if (currentNameResult == null || currentNameResult == DBNull.Value)
			{
				throw new InvalidOperationException("Grade level was not found.");
			}

			string currentName = Convert.ToString(currentNameResult) ?? string.Empty;

			int dependentRecordCount = await CountDependentRecordsAsync(
				connection,
				currentName,
				cancellationToken);

			if (dependentRecordCount > 0)
			{
				throw new InvalidOperationException("Grade level cannot be deleted because records already use it.");
			}

			using SqlCommand deleteCommand = new(deleteSql, connection);
			deleteCommand.Parameters.AddWithValue("@Id", id);

			await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
		}

		private static AcademicsGradeLevel Map(SqlDataReader reader)
		{
			int dependentRecordCount = reader["DependentRecordCount"] == DBNull.Value
				? 0
				: Convert.ToInt32(reader["DependentRecordCount"]);

			return new AcademicsGradeLevel
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				Name = reader["Name"] as string ?? string.Empty,
				ShortName = reader["ShortName"] as string ?? string.Empty,
				SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
				DependentRecordCount = dependentRecordCount,
				IsEditable = dependentRecordCount == 0,
				IsDeletable = dependentRecordCount == 0
			};
		}

		private static async Task<int> CountDependentRecordsAsync(
			SqlConnection connection,
			string gradeLevelName,
			CancellationToken cancellationToken)
		{
			const string sql =
				"""
                SELECT
                    (
                        (
                            SELECT COUNT(1)
                            FROM Academics_SectionTemplate sectionTemplate
                            WHERE sectionTemplate.GradeLevelName = @GradeLevelName
                        )
                        +
                        (
                            SELECT COUNT(1)
                            FROM Academics_ProgramProspectusLine prospectusLine
                            WHERE prospectusLine.GradeLevelName = @GradeLevelName
                        )
                        +
                        (
                            SELECT COUNT(1)
                            FROM Academics_SchoolYearSection section
                            WHERE section.GradeLevelName = @GradeLevelName
                        )
                        +
                        (
                            SELECT COUNT(1)
                            FROM Academics_SchoolYearProgramLine programLine
                            WHERE programLine.GradeLevelName = @GradeLevelName
                        )
                        +
                        (
                            SELECT COUNT(1)
                            FROM Academics_Enrollment enrollment
                            WHERE enrollment.GradeLevelName = @GradeLevelName
                        )
                    ) AS DependentRecordCount;
                """;

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@GradeLevelName", gradeLevelName);

			object? result = await command.ExecuteScalarAsync(cancellationToken);

			return result == null || result == DBNull.Value
				? 0
				: Convert.ToInt32(result);
		}

		private static void PrepareForSave(AcademicsGradeLevel gradeLevel)
		{
			gradeLevel.Name = NormalizeRequired(gradeLevel.Name);
			gradeLevel.ShortName = Normalize(gradeLevel.ShortName) ?? string.Empty;
			gradeLevel.SortOrder = Math.Max(0, gradeLevel.SortOrder);

			if (string.IsNullOrWhiteSpace(gradeLevel.Name))
			{
				throw new InvalidOperationException("Grade level name is required.");
			}
		}

		private static void AddParameters(SqlCommand command, AcademicsGradeLevel gradeLevel)
		{
			command.Parameters.AddWithValue("@Name", gradeLevel.Name);
			command.Parameters.AddWithValue("@ShortName", ToDbNullable(gradeLevel.ShortName));
			command.Parameters.AddWithValue("@SortOrder", gradeLevel.SortOrder);
			command.Parameters.AddWithValue("@IsActive", gradeLevel.IsActive);
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
	}
}
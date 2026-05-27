using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Academics.Setup;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Academics.Setup
{
	public sealed class AcademicsProgramProspectusLineRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public AcademicsProgramProspectusLineRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<AcademicsProgramProspectusLine>> GetByProgramIdAsync(
			int programId,
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    ppl.Id,
                    ppl.ProgramId,
                    ppl.GradeLevelName,
                    ppl.TemplateTermSequence,
                    ppl.TemplateTermLabel,
                    ppl.LearningAreaId,
                    la.Code AS LearningAreaCode,
                    la.ShortName AS LearningAreaShortName,
                    la.Description AS LearningAreaDescription,
                    ppl.SortOrder,
                    ppl.IsActive,
                    (
                        SELECT COUNT(1)
                        FROM Academics_SchoolYearProgramLine deployedLine
                        WHERE deployedLine.SourceProgramProspectusLineId = ppl.Id
                    ) AS DependentRecordCount
                FROM Academics_ProgramProspectusLine ppl
                INNER JOIN LearningArea la
                    ON ppl.LearningAreaId = la.Id
                WHERE ppl.ProgramId = @ProgramId
                  AND (@IncludeInactive = 1 OR ppl.IsActive = 1)
                ORDER BY
                    ppl.IsActive DESC,
                    ppl.GradeLevelName,
                    ppl.TemplateTermSequence,
                    ppl.SortOrder,
                    la.Sort,
                    la.Description,
                    ppl.Id;
                """;

			List<AcademicsProgramProspectusLine> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@ProgramId", programId);
			command.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				items.Add(Map(reader));
			}

			return items;
		}

		public async Task<AcademicsProgramProspectusLine?> GetByIdAsync(
			int id,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    ppl.Id,
                    ppl.ProgramId,
                    ppl.GradeLevelName,
                    ppl.TemplateTermSequence,
                    ppl.TemplateTermLabel,
                    ppl.LearningAreaId,
                    la.Code AS LearningAreaCode,
                    la.ShortName AS LearningAreaShortName,
                    la.Description AS LearningAreaDescription,
                    ppl.SortOrder,
                    ppl.IsActive,
                    (
                        SELECT COUNT(1)
                        FROM Academics_SchoolYearProgramLine deployedLine
                        WHERE deployedLine.SourceProgramProspectusLineId = ppl.Id
                    ) AS DependentRecordCount
                FROM Academics_ProgramProspectusLine ppl
                INNER JOIN LearningArea la
                    ON ppl.LearningAreaId = la.Id
                WHERE ppl.Id = @Id;
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
			AcademicsProgramProspectusLine line,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                INSERT INTO Academics_ProgramProspectusLine
                (
                    ProgramId,
                    GradeLevelName,
                    TemplateTermSequence,
                    TemplateTermLabel,
                    LearningAreaId,
                    SortOrder,
                    IsActive
                )
                VALUES
                (
                    @ProgramId,
                    @GradeLevelName,
                    @TemplateTermSequence,
                    @TemplateTermLabel,
                    @LearningAreaId,
                    @SortOrder,
                    @IsActive
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			PrepareForSave(line);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			await ValidateForSaveAsync(line, connection, cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddParameters(command, line);

			object? result = await command.ExecuteScalarAsync(cancellationToken);
			return result is int id ? id : 0;
		}

		public async Task UpdateAsync(
			AcademicsProgramProspectusLine line,
			CancellationToken cancellationToken = default)
		{
			const string currentSql =
				"""
                SELECT
                    ProgramId,
                    GradeLevelName,
                    TemplateTermSequence,
                    TemplateTermLabel,
                    LearningAreaId
                FROM Academics_ProgramProspectusLine
                WHERE Id = @Id;
                """;

			const string updateSql =
				"""
                UPDATE Academics_ProgramProspectusLine
                SET
                    ProgramId = @ProgramId,
                    GradeLevelName = @GradeLevelName,
                    TemplateTermSequence = @TemplateTermSequence,
                    TemplateTermLabel = @TemplateTermLabel,
                    LearningAreaId = @LearningAreaId,
                    SortOrder = @SortOrder,
                    IsActive = @IsActive,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			PrepareForSave(line);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			await ValidateForSaveAsync(line, connection, cancellationToken);

			using SqlCommand currentCommand = new(currentSql, connection);
			currentCommand.Parameters.AddWithValue("@Id", line.Id);

			using SqlDataReader currentReader = await currentCommand.ExecuteReaderAsync(cancellationToken);

			if (!await currentReader.ReadAsync(cancellationToken))
			{
				throw new InvalidOperationException("Prospectus line was not found.");
			}

			bool coreChanged =
				currentReader.GetInt32(currentReader.GetOrdinal("ProgramId")) != line.ProgramId ||
				!string.Equals(currentReader["GradeLevelName"] as string ?? string.Empty, line.GradeLevelName, StringComparison.OrdinalIgnoreCase) ||
				currentReader.GetInt32(currentReader.GetOrdinal("TemplateTermSequence")) != line.TemplateTermSequence ||
				!string.Equals(currentReader["TemplateTermLabel"] as string ?? string.Empty, line.TemplateTermLabel, StringComparison.OrdinalIgnoreCase) ||
				currentReader.GetInt32(currentReader.GetOrdinal("LearningAreaId")) != line.LearningAreaId;

			await currentReader.CloseAsync();

			if (coreChanged)
			{
				int dependentRecordCount = await CountDependentRecordsAsync(
					connection,
					line.Id,
					cancellationToken);

				if (dependentRecordCount > 0)
				{
					throw new InvalidOperationException("Prospectus line structure cannot be changed because it has already been deployed to a school year program.");
				}
			}

			using SqlCommand command = new(updateSql, connection);
			AddParameters(command, line);
			command.Parameters.AddWithValue("@Id", line.Id);

			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		public async Task SetIsActiveAsync(
			int id,
			bool isActive,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_ProgramProspectusLine
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
                FROM Academics_ProgramProspectusLine
                WHERE Id = @Id;
                """;

			const string deleteSql =
				"""
                DELETE FROM Academics_ProgramProspectusLine
                WHERE Id = @Id;
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand existsCommand = new(existsSql, connection);
			existsCommand.Parameters.AddWithValue("@Id", id);

			int lineCount = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken));

			if (lineCount == 0)
			{
				throw new InvalidOperationException("Prospectus line was not found.");
			}

			int dependentRecordCount = await CountDependentRecordsAsync(
				connection,
				id,
				cancellationToken);

			if (dependentRecordCount > 0)
			{
				throw new InvalidOperationException("Prospectus line cannot be deleted because it has already been deployed to a school year program.");
			}

			using SqlCommand deleteCommand = new(deleteSql, connection);
			deleteCommand.Parameters.AddWithValue("@Id", id);

			await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
		}

		private static AcademicsProgramProspectusLine Map(SqlDataReader reader)
		{
			int dependentRecordCount = reader["DependentRecordCount"] == DBNull.Value
				? 0
				: Convert.ToInt32(reader["DependentRecordCount"]);

			return new AcademicsProgramProspectusLine
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				ProgramId = reader.GetInt32(reader.GetOrdinal("ProgramId")),
				GradeLevelName = reader["GradeLevelName"] as string ?? string.Empty,
				TemplateTermSequence = reader.GetInt32(reader.GetOrdinal("TemplateTermSequence")),
				TemplateTermLabel = reader["TemplateTermLabel"] as string ?? string.Empty,
				LearningAreaId = reader.GetInt32(reader.GetOrdinal("LearningAreaId")),
				LearningAreaCode = reader["LearningAreaCode"] as string ?? string.Empty,
				LearningAreaShortName = reader["LearningAreaShortName"] as string ?? string.Empty,
				LearningAreaDescription = reader["LearningAreaDescription"] as string ?? string.Empty,
				SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
				DependentRecordCount = dependentRecordCount,
				IsEditable = dependentRecordCount == 0,
				IsDeletable = dependentRecordCount == 0
			};
		}

		private static async Task ValidateForSaveAsync(
			AcademicsProgramProspectusLine line,
			SqlConnection connection,
			CancellationToken cancellationToken)
		{
			await ValidateProgramAsync(line.ProgramId, connection, cancellationToken);
			await ValidateGradeLevelAsync(line.GradeLevelName, connection, cancellationToken);
			await ValidateLearningAreaAsync(line.LearningAreaId, connection, cancellationToken);
		}

		private static async Task ValidateProgramAsync(
			int programId,
			SqlConnection connection,
			CancellationToken cancellationToken)
		{
			const string sql =
				"""
                SELECT COUNT(1)
                FROM Academics_Program
                WHERE Id = @ProgramId
                  AND IsActive = 1;
                """;

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@ProgramId", programId);

			int count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));

			if (count == 0)
			{
				throw new InvalidOperationException("Select an active program.");
			}
		}

		private static async Task ValidateGradeLevelAsync(
			string gradeLevelName,
			SqlConnection connection,
			CancellationToken cancellationToken)
		{
			const string sql =
				"""
                SELECT COUNT(1)
                FROM Academics_GradeLevel
                WHERE Name = @GradeLevelName
                  AND IsActive = 1;
                """;

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@GradeLevelName", gradeLevelName);

			int count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));

			if (count == 0)
			{
				throw new InvalidOperationException("Select an active grade level.");
			}
		}

		private static async Task ValidateLearningAreaAsync(
			int learningAreaId,
			SqlConnection connection,
			CancellationToken cancellationToken)
		{
			const string sql =
				"""
                SELECT COUNT(1)
                FROM LearningArea
                WHERE Id = @LearningAreaId;
                """;

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@LearningAreaId", learningAreaId);

			int count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));

			if (count == 0)
			{
				throw new InvalidOperationException("Select a valid learning area.");
			}
		}

		private static async Task<int> CountDependentRecordsAsync(
			SqlConnection connection,
			int prospectusLineId,
			CancellationToken cancellationToken)
		{
			const string sql =
				"""
                SELECT COUNT(1)
                FROM Academics_SchoolYearProgramLine
                WHERE SourceProgramProspectusLineId = @ProspectusLineId;
                """;

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@ProspectusLineId", prospectusLineId);

			object? result = await command.ExecuteScalarAsync(cancellationToken);

			return result == null || result == DBNull.Value
				? 0
				: Convert.ToInt32(result);
		}

		private static void PrepareForSave(AcademicsProgramProspectusLine line)
		{
			line.GradeLevelName = NormalizeRequired(line.GradeLevelName);
			line.TemplateTermLabel = NormalizeRequired(line.TemplateTermLabel);
			line.TemplateTermSequence = Math.Max(1, line.TemplateTermSequence);
			line.SortOrder = Math.Max(0, line.SortOrder);

			if (line.ProgramId <= 0)
			{
				throw new InvalidOperationException("Program is required.");
			}

			if (string.IsNullOrWhiteSpace(line.GradeLevelName))
			{
				throw new InvalidOperationException("Grade level is required.");
			}

			if (string.IsNullOrWhiteSpace(line.TemplateTermLabel))
			{
				throw new InvalidOperationException("Template term label is required.");
			}

			if (line.LearningAreaId <= 0)
			{
				throw new InvalidOperationException("Learning area is required.");
			}
		}

		private static void AddParameters(SqlCommand command, AcademicsProgramProspectusLine line)
		{
			command.Parameters.AddWithValue("@ProgramId", line.ProgramId);
			command.Parameters.AddWithValue("@GradeLevelName", line.GradeLevelName);
			command.Parameters.AddWithValue("@TemplateTermSequence", line.TemplateTermSequence);
			command.Parameters.AddWithValue("@TemplateTermLabel", line.TemplateTermLabel);
			command.Parameters.AddWithValue("@LearningAreaId", line.LearningAreaId);
			command.Parameters.AddWithValue("@SortOrder", line.SortOrder);
			command.Parameters.AddWithValue("@IsActive", line.IsActive);
		}

		private static string NormalizeRequired(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
		}
	}
}
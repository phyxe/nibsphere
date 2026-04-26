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
                    ppl.IsActive
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
                    ppl.IsActive
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

			using SqlCommand command = new(sql, connection);
			AddParameters(command, line);

			object? result = await command.ExecuteScalarAsync(cancellationToken);
			return result is int id ? id : 0;
		}

		public async Task UpdateAsync(
			AcademicsProgramProspectusLine line,
			CancellationToken cancellationToken = default)
		{
			const string sql =
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

			using SqlCommand command = new(sql, connection);
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

		private static AcademicsProgramProspectusLine Map(SqlDataReader reader)
		{
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
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
			};
		}

		private static void PrepareForSave(AcademicsProgramProspectusLine line)
		{
			line.GradeLevelName = NormalizeRequired(line.GradeLevelName);
			line.TemplateTermLabel = NormalizeRequired(line.TemplateTermLabel);
			line.TemplateTermSequence = Math.Max(1, line.TemplateTermSequence);
			line.SortOrder = Math.Max(0, line.SortOrder);
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
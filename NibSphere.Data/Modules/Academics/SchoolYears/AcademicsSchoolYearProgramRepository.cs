using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Academics.SchoolYears;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Academics.SchoolYears
{
	public sealed class AcademicsSchoolYearProgramRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public AcademicsSchoolYearProgramRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<AcademicsSchoolYearProgram>> GetBySchoolYearIdAsync(
			int schoolYearId,
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    syp.Id,
                    syp.SchoolYearId,
                    sy.Name AS SchoolYearName,
                    syp.SourceProgramId,
                    syp.ProgramCode,
                    syp.ProgramName,
                    syp.ProgramDescription,
                    syp.SortOrder,
                    syp.IsActive
                FROM Academics_SchoolYearProgram syp
                INNER JOIN Academics_SchoolYear sy
                    ON syp.SchoolYearId = sy.Id
                WHERE syp.SchoolYearId = @SchoolYearId
                  AND (@IncludeInactive = 1 OR syp.IsActive = 1)
                ORDER BY
                    syp.IsActive DESC,
                    syp.SortOrder,
                    syp.ProgramName,
                    syp.Id;
                """;

			List<AcademicsSchoolYearProgram> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@SchoolYearId", schoolYearId);
			command.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				items.Add(MapProgram(reader));
			}

			return items;
		}

		public async Task<List<AcademicsSchoolYearProgramLine>> GetLinesBySchoolYearProgramIdAsync(
			int schoolYearProgramId,
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    Id,
                    SchoolYearProgramId,
                    SourceProgramProspectusLineId,
                    GradeLevelName,
                    TemplateTermSequence,
                    TemplateTermLabel,
                    LearningAreaId,
                    LearningAreaCode,
                    LearningAreaShortName,
                    LearningAreaDescription,
                    SortOrder,
                    IsActive
                FROM Academics_SchoolYearProgramLine
                WHERE SchoolYearProgramId = @SchoolYearProgramId
                  AND (@IncludeInactive = 1 OR IsActive = 1)
                ORDER BY
                    IsActive DESC,
                    GradeLevelName,
                    TemplateTermSequence,
                    SortOrder,
                    LearningAreaDescription,
                    Id;
                """;

			List<AcademicsSchoolYearProgramLine> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@SchoolYearProgramId", schoolYearProgramId);
			command.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				items.Add(MapProgramLine(reader));
			}

			return items;
		}

		public async Task<int> DeployProgramAsync(
			int schoolYearId,
			int sourceProgramId,
			CancellationToken cancellationToken = default)
		{
			const string existingSql =
				"""
                SELECT Id
                FROM Academics_SchoolYearProgram
                WHERE SchoolYearId = @SchoolYearId
                  AND SourceProgramId = @SourceProgramId;
                """;

			const string insertProgramSql =
				"""
                INSERT INTO Academics_SchoolYearProgram
                (
                    SchoolYearId,
                    SourceProgramId,
                    ProgramCode,
                    ProgramName,
                    ProgramDescription,
                    SortOrder,
                    IsActive
                )
                SELECT
                    @SchoolYearId,
                    p.Id,
                    p.Code,
                    p.Name,
                    p.Description,
                    p.SortOrder,
                    1
                FROM Academics_Program p
                WHERE p.Id = @SourceProgramId;

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			const string insertLinesSql =
				"""
                INSERT INTO Academics_SchoolYearProgramLine
                (
                    SchoolYearProgramId,
                    SourceProgramProspectusLineId,
                    GradeLevelName,
                    TemplateTermSequence,
                    TemplateTermLabel,
                    LearningAreaId,
                    LearningAreaCode,
                    LearningAreaShortName,
                    LearningAreaDescription,
                    SortOrder,
                    IsActive
                )
                SELECT
                    @SchoolYearProgramId,
                    ppl.Id,
                    ppl.GradeLevelName,
                    ppl.TemplateTermSequence,
                    ppl.TemplateTermLabel,
                    ppl.LearningAreaId,
                    la.Code,
                    la.ShortName,
                    la.Description,
                    ppl.SortOrder,
                    ppl.IsActive
                FROM Academics_ProgramProspectusLine ppl
                INNER JOIN LearningArea la
                    ON ppl.LearningAreaId = la.Id
                WHERE ppl.ProgramId = @SourceProgramId
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM Academics_SchoolYearProgramLine existing
                      WHERE existing.SchoolYearProgramId = @SchoolYearProgramId
                        AND existing.SourceProgramProspectusLineId = ppl.Id
                  );
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlTransaction transaction = connection.BeginTransaction();

			try
			{
				int schoolYearProgramId;

				using (SqlCommand existingCommand = new(existingSql, connection, transaction))
				{
					existingCommand.Parameters.AddWithValue("@SchoolYearId", schoolYearId);
					existingCommand.Parameters.AddWithValue("@SourceProgramId", sourceProgramId);

					object? existingResult = await existingCommand.ExecuteScalarAsync(cancellationToken);

					if (existingResult is int existingId)
					{
						schoolYearProgramId = existingId;
					}
					else
					{
						using SqlCommand insertProgramCommand = new(insertProgramSql, connection, transaction);
						insertProgramCommand.Parameters.AddWithValue("@SchoolYearId", schoolYearId);
						insertProgramCommand.Parameters.AddWithValue("@SourceProgramId", sourceProgramId);

						object? insertResult = await insertProgramCommand.ExecuteScalarAsync(cancellationToken);
						schoolYearProgramId = insertResult is int newId ? newId : 0;
					}
				}

				if (schoolYearProgramId <= 0)
				{
					throw new InvalidOperationException("The selected program could not be deployed.");
				}

				using SqlCommand insertLinesCommand = new(insertLinesSql, connection, transaction);
				insertLinesCommand.Parameters.AddWithValue("@SchoolYearProgramId", schoolYearProgramId);
				insertLinesCommand.Parameters.AddWithValue("@SourceProgramId", sourceProgramId);

				await insertLinesCommand.ExecuteNonQueryAsync(cancellationToken);

				transaction.Commit();

				return schoolYearProgramId;
			}
			catch
			{
				transaction.Rollback();
				throw;
			}
		}

		public async Task SetProgramIsActiveAsync(
			int id,
			bool isActive,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_SchoolYearProgram
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

		public async Task SetLineIsActiveAsync(
			int id,
			bool isActive,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_SchoolYearProgramLine
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

		private static AcademicsSchoolYearProgram MapProgram(SqlDataReader reader)
		{
			return new AcademicsSchoolYearProgram
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				SchoolYearId = reader.GetInt32(reader.GetOrdinal("SchoolYearId")),
				SchoolYearName = reader["SchoolYearName"] as string ?? string.Empty,
				SourceProgramId = reader.GetInt32(reader.GetOrdinal("SourceProgramId")),
				ProgramCode = reader["ProgramCode"] as string ?? string.Empty,
				ProgramName = reader["ProgramName"] as string ?? string.Empty,
				ProgramDescription = reader["ProgramDescription"] as string ?? string.Empty,
				SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
			};
		}

		private static AcademicsSchoolYearProgramLine MapProgramLine(SqlDataReader reader)
		{
			return new AcademicsSchoolYearProgramLine
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				SchoolYearProgramId = reader.GetInt32(reader.GetOrdinal("SchoolYearProgramId")),
				SourceProgramProspectusLineId = reader.GetInt32(reader.GetOrdinal("SourceProgramProspectusLineId")),
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
	}
}
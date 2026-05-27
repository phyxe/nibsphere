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

		public async Task<List<AcademicsProgramProspectusCluster>> GetClustersByProgramIdAsync(
	int programId,
	bool includeInactive = false,
	CancellationToken cancellationToken = default)
		{
			const string sql =
	"""
    SELECT
        ppl.ProgramId,
        ppl.GradeLevelName,
        ppl.TemplateTermSequence,
        MIN(ppl.TemplateTermLabel) AS TemplateTermLabel,
        MIN(ppl.SortOrder) AS SortOrder,
        COUNT(DISTINCT ppl.Id) AS LearningAreaCount,
        COUNT(DISTINCT CASE WHEN ppl.IsActive = 1 THEN ppl.Id END) AS ActiveLearningAreaCount,
        MAX(CASE WHEN ppl.IsActive = 1 THEN 1 ELSE 0 END) AS IsActive,
        COUNT(deployedLine.Id) AS DependentRecordCount
    FROM Academics_ProgramProspectusLine ppl
    LEFT JOIN Academics_SchoolYearProgramLine deployedLine
        ON deployedLine.SourceProgramProspectusLineId = ppl.Id
    WHERE ppl.ProgramId = @ProgramId
      AND (@IncludeInactive = 1 OR ppl.IsActive = 1)
    GROUP BY
        ppl.ProgramId,
        ppl.GradeLevelName,
        ppl.TemplateTermSequence
    ORDER BY
        MAX(CASE WHEN ppl.IsActive = 1 THEN 1 ELSE 0 END) DESC,
        ppl.GradeLevelName,
        ppl.TemplateTermSequence,
        MIN(ppl.SortOrder);
    """;

			List<AcademicsProgramProspectusCluster> clusters = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@ProgramId", programId);
			command.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				int dependentRecordCount = reader["DependentRecordCount"] == DBNull.Value
					? 0
					: Convert.ToInt32(reader["DependentRecordCount"]);

				clusters.Add(new AcademicsProgramProspectusCluster
				{
					ProgramId = reader.GetInt32(reader.GetOrdinal("ProgramId")),
					GradeLevelName = reader["GradeLevelName"] as string ?? string.Empty,
					TemplateTermSequence = reader.GetInt32(reader.GetOrdinal("TemplateTermSequence")),
					TemplateTermLabel = reader["TemplateTermLabel"] as string ?? string.Empty,
					SortOrder = reader["SortOrder"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SortOrder"]),
					LearningAreaCount = reader["LearningAreaCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["LearningAreaCount"]),
					ActiveLearningAreaCount = reader["ActiveLearningAreaCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ActiveLearningAreaCount"]),
					IsActive = reader["IsActive"] != DBNull.Value && Convert.ToInt32(reader["IsActive"]) == 1,
					DependentRecordCount = dependentRecordCount,
					IsEditable = dependentRecordCount == 0,
					IsDeletable = dependentRecordCount == 0
				});
			}

			await reader.CloseAsync();

			foreach (AcademicsProgramProspectusCluster cluster in clusters)
			{
				cluster.LearningAreas = await GetClusterLearningAreasAsync(
					cluster.ProgramId,
					cluster.GradeLevelName,
					cluster.TemplateTermSequence,
					includeInactive,
					cancellationToken);
			}

			return clusters;
		}

		public async Task<List<AcademicsProgramProspectusClusterLearningArea>> GetClusterLearningAreasAsync(
			int programId,
			string gradeLevelName,
			int templateTermSequence,
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
        SELECT
            ppl.Id AS ProspectusLineId,
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
          AND ppl.GradeLevelName = @GradeLevelName
          AND ppl.TemplateTermSequence = @TemplateTermSequence
          AND (@IncludeInactive = 1 OR ppl.IsActive = 1)
        ORDER BY
            ppl.IsActive DESC,
            ppl.SortOrder,
            la.Sort,
            la.Description,
            ppl.Id;
        """;

			List<AcademicsProgramProspectusClusterLearningArea> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@ProgramId", programId);
			command.Parameters.AddWithValue("@GradeLevelName", NormalizeRequired(gradeLevelName));
			command.Parameters.AddWithValue("@TemplateTermSequence", Math.Max(1, templateTermSequence));
			command.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				items.Add(new AcademicsProgramProspectusClusterLearningArea
				{
					ProspectusLineId = reader.GetInt32(reader.GetOrdinal("ProspectusLineId")),
					LearningAreaId = reader.GetInt32(reader.GetOrdinal("LearningAreaId")),
					LearningAreaCode = reader["LearningAreaCode"] as string ?? string.Empty,
					LearningAreaShortName = reader["LearningAreaShortName"] as string ?? string.Empty,
					LearningAreaDescription = reader["LearningAreaDescription"] as string ?? string.Empty,
					SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
					IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
					DependentRecordCount = reader["DependentRecordCount"] == DBNull.Value
						? 0
						: Convert.ToInt32(reader["DependentRecordCount"])
				});
			}

			return items;
		}

		public async Task<int> GetNextTemplateTermSequenceAsync(
			int programId,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
        SELECT COALESCE(MAX(TemplateTermSequence), 0) + 1
        FROM Academics_ProgramProspectusLine
        WHERE ProgramId = @ProgramId;
        """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@ProgramId", programId);

			object? result = await command.ExecuteScalarAsync(cancellationToken);

			return result == null || result == DBNull.Value
				? 1
				: Math.Max(1, Convert.ToInt32(result));
		}

		public async Task SaveClusterAsync(
			int programId,
			string gradeLevelName,
			int templateTermSequence,
			string templateTermLabel,
			IEnumerable<int> selectedLearningAreaIds,
			CancellationToken cancellationToken = default)
		{
			gradeLevelName = NormalizeRequired(gradeLevelName);
			templateTermLabel = NormalizeRequired(templateTermLabel);
			templateTermSequence = Math.Max(1, templateTermSequence);

			List<int> selectedIds = selectedLearningAreaIds
				.Where(x => x > 0)
				.Distinct()
				.ToList();

			if (programId <= 0)
			{
				throw new InvalidOperationException("Program is required.");
			}

			if (string.IsNullOrWhiteSpace(gradeLevelName))
			{
				throw new InvalidOperationException("Grade level is required.");
			}

			if (string.IsNullOrWhiteSpace(templateTermLabel))
			{
				throw new InvalidOperationException("Template term label is required.");
			}

			if (selectedIds.Count == 0)
			{
				throw new InvalidOperationException("Select at least one learning area.");
			}

			const string existingLinesSql =
				"""
        SELECT
            ppl.Id,
            ppl.LearningAreaId,
            ppl.TemplateTermLabel,
            ppl.SortOrder,
            ppl.IsActive,
            (
                SELECT COUNT(1)
                FROM Academics_SchoolYearProgramLine deployedLine
                WHERE deployedLine.SourceProgramProspectusLineId = ppl.Id
            ) AS DependentRecordCount
        FROM Academics_ProgramProspectusLine ppl
        WHERE ppl.ProgramId = @ProgramId
          AND ppl.GradeLevelName = @GradeLevelName
          AND ppl.TemplateTermSequence = @TemplateTermSequence;
        """;

			const string insertLineSql =
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
            1
        );
        """;

			const string updateSelectedLineSql =
				"""
        UPDATE Academics_ProgramProspectusLine
        SET
            TemplateTermLabel = @TemplateTermLabel,
            SortOrder = @SortOrder,
            IsActive = 1,
            UpdatedAt = GETDATE()
        WHERE Id = @Id;
        """;

			const string archiveLineSql =
				"""
        UPDATE Academics_ProgramProspectusLine
        SET
            IsActive = 0,
            UpdatedAt = GETDATE()
        WHERE Id = @Id;
        """;

			const string deleteLineSql =
				"""
        DELETE FROM Academics_ProgramProspectusLine
        WHERE Id = @Id;
        """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			await ValidateProgramAsync(programId, connection, cancellationToken);
			await ValidateGradeLevelAsync(gradeLevelName, connection, cancellationToken);

			foreach (int learningAreaId in selectedIds)
			{
				await ValidateLearningAreaAsync(learningAreaId, connection, cancellationToken);
			}

			Dictionary<int, ExistingClusterLine> existingByLearningAreaId = new();

			using (SqlCommand existingCommand = new(existingLinesSql, connection))
			{
				existingCommand.Parameters.AddWithValue("@ProgramId", programId);
				existingCommand.Parameters.AddWithValue("@GradeLevelName", gradeLevelName);
				existingCommand.Parameters.AddWithValue("@TemplateTermSequence", templateTermSequence);

				using SqlDataReader reader = await existingCommand.ExecuteReaderAsync(cancellationToken);

				while (await reader.ReadAsync(cancellationToken))
				{
					int learningAreaId = reader.GetInt32(reader.GetOrdinal("LearningAreaId"));

					existingByLearningAreaId[learningAreaId] = new ExistingClusterLine
					{
						Id = reader.GetInt32(reader.GetOrdinal("Id")),
						LearningAreaId = learningAreaId,
						TemplateTermLabel = reader["TemplateTermLabel"] as string ?? string.Empty,
						SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
						IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
						DependentRecordCount = reader["DependentRecordCount"] == DBNull.Value
							? 0
							: Convert.ToInt32(reader["DependentRecordCount"])
					};
				}
			}

			bool labelChangedOnDeployedLine = existingByLearningAreaId.Values.Any(x =>
				x.DependentRecordCount > 0 &&
				!string.Equals(x.TemplateTermLabel, templateTermLabel, StringComparison.OrdinalIgnoreCase));

			if (labelChangedOnDeployedLine)
			{
				throw new InvalidOperationException("Template term label cannot be changed because this cluster has already been deployed.");
			}

			using SqlTransaction transaction = connection.BeginTransaction();

			try
			{
				for (int i = 0; i < selectedIds.Count; i++)
				{
					int learningAreaId = selectedIds[i];
					int sortOrder = i + 1;

					if (existingByLearningAreaId.TryGetValue(learningAreaId, out ExistingClusterLine? existingLine))
					{
						using SqlCommand updateCommand = new(updateSelectedLineSql, connection, transaction);
						updateCommand.Parameters.AddWithValue("@Id", existingLine.Id);
						updateCommand.Parameters.AddWithValue("@TemplateTermLabel", templateTermLabel);
						updateCommand.Parameters.AddWithValue("@SortOrder", sortOrder);

						await updateCommand.ExecuteNonQueryAsync(cancellationToken);
					}
					else
					{
						using SqlCommand insertCommand = new(insertLineSql, connection, transaction);
						insertCommand.Parameters.AddWithValue("@ProgramId", programId);
						insertCommand.Parameters.AddWithValue("@GradeLevelName", gradeLevelName);
						insertCommand.Parameters.AddWithValue("@TemplateTermSequence", templateTermSequence);
						insertCommand.Parameters.AddWithValue("@TemplateTermLabel", templateTermLabel);
						insertCommand.Parameters.AddWithValue("@LearningAreaId", learningAreaId);
						insertCommand.Parameters.AddWithValue("@SortOrder", sortOrder);

						await insertCommand.ExecuteNonQueryAsync(cancellationToken);
					}
				}

				foreach (ExistingClusterLine existingLine in existingByLearningAreaId.Values)
				{
					if (selectedIds.Contains(existingLine.LearningAreaId))
					{
						continue;
					}

					if (existingLine.DependentRecordCount > 0)
					{
						using SqlCommand archiveCommand = new(archiveLineSql, connection, transaction);
						archiveCommand.Parameters.AddWithValue("@Id", existingLine.Id);

						await archiveCommand.ExecuteNonQueryAsync(cancellationToken);
					}
					else
					{
						using SqlCommand deleteCommand = new(deleteLineSql, connection, transaction);
						deleteCommand.Parameters.AddWithValue("@Id", existingLine.Id);

						await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
					}
				}

				transaction.Commit();
			}
			catch
			{
				transaction.Rollback();
				throw;
			}
		}

		public async Task SetClusterIsActiveAsync(
			int programId,
			string gradeLevelName,
			int templateTermSequence,
			bool isActive,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
        UPDATE Academics_ProgramProspectusLine
        SET
            IsActive = @IsActive,
            UpdatedAt = GETDATE()
        WHERE ProgramId = @ProgramId
          AND GradeLevelName = @GradeLevelName
          AND TemplateTermSequence = @TemplateTermSequence;
        """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@ProgramId", programId);
			command.Parameters.AddWithValue("@GradeLevelName", NormalizeRequired(gradeLevelName));
			command.Parameters.AddWithValue("@TemplateTermSequence", Math.Max(1, templateTermSequence));
			command.Parameters.AddWithValue("@IsActive", isActive);

			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		public async Task DeleteClusterAsync(
			int programId,
			string gradeLevelName,
			int templateTermSequence,
			CancellationToken cancellationToken = default)
		{
			const string countSql =
	"""
    SELECT
        COUNT(DISTINCT ppl.Id) AS LineCount,
        COUNT(deployedLine.Id) AS DependentRecordCount
    FROM Academics_ProgramProspectusLine ppl
    LEFT JOIN Academics_SchoolYearProgramLine deployedLine
        ON deployedLine.SourceProgramProspectusLineId = ppl.Id
    WHERE ppl.ProgramId = @ProgramId
      AND ppl.GradeLevelName = @GradeLevelName
      AND ppl.TemplateTermSequence = @TemplateTermSequence;
    """;

			const string archiveSql =
				"""
        UPDATE Academics_ProgramProspectusLine
        SET
            IsActive = 0,
            UpdatedAt = GETDATE()
        WHERE ProgramId = @ProgramId
          AND GradeLevelName = @GradeLevelName
          AND TemplateTermSequence = @TemplateTermSequence;
        """;

			const string deleteSql =
				"""
        DELETE FROM Academics_ProgramProspectusLine
        WHERE ProgramId = @ProgramId
          AND GradeLevelName = @GradeLevelName
          AND TemplateTermSequence = @TemplateTermSequence;
        """;

			gradeLevelName = NormalizeRequired(gradeLevelName);
			templateTermSequence = Math.Max(1, templateTermSequence);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand countCommand = new(countSql, connection);
			countCommand.Parameters.AddWithValue("@ProgramId", programId);
			countCommand.Parameters.AddWithValue("@GradeLevelName", gradeLevelName);
			countCommand.Parameters.AddWithValue("@TemplateTermSequence", templateTermSequence);

			using SqlDataReader reader = await countCommand.ExecuteReaderAsync(cancellationToken);

			if (!await reader.ReadAsync(cancellationToken))
			{
				throw new InvalidOperationException("Prospectus cluster could not be checked before deletion.");
			}

			int lineCount = reader["LineCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["LineCount"]);
			int dependentRecordCount = reader["DependentRecordCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["DependentRecordCount"]);

			await reader.CloseAsync();

			if (lineCount == 0)
			{
				throw new InvalidOperationException("Prospectus cluster was not found.");
			}

			string sql = dependentRecordCount > 0
				? archiveSql
				: deleteSql;

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@ProgramId", programId);
			command.Parameters.AddWithValue("@GradeLevelName", gradeLevelName);
			command.Parameters.AddWithValue("@TemplateTermSequence", templateTermSequence);

			await command.ExecuteNonQueryAsync(cancellationToken);
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

		private sealed class ExistingClusterLine
		{
			public int Id { get; set; }
			public int LearningAreaId { get; set; }

			public string TemplateTermLabel { get; set; } = string.Empty;

			public int SortOrder { get; set; }
			public bool IsActive { get; set; }

			public int DependentRecordCount { get; set; }
		}
	}


}
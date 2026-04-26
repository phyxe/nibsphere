using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Academics.SchoolYears;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Academics.SchoolYears
{
	public sealed class AcademicsSchoolYearSectionRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public AcademicsSchoolYearSectionRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<AcademicsSchoolYearSection>> GetBySchoolYearIdAsync(
			int schoolYearId,
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    section.Id,
                    section.SchoolYearId,
                    sy.Name AS SchoolYearName,
                    section.SourceSectionTemplateId,
                    section.GradeLevelName,
                    section.SectionName,
                    section.AdviserTeacherId,
                    section.AdviserLastName,
                    section.AdviserFirstName,
                    section.AdviserMiddleName,
                    section.AdviserExtensionName,
                    section.AdviserPosition,
                    section.AdviserDesignation,
                    section.SortOrder,
                    section.IsActive
                FROM Academics_SchoolYearSection section
                INNER JOIN Academics_SchoolYear sy
                    ON section.SchoolYearId = sy.Id
                WHERE section.SchoolYearId = @SchoolYearId
                  AND (@IncludeInactive = 1 OR section.IsActive = 1)
                ORDER BY
                    section.IsActive DESC,
                    section.SortOrder,
                    section.GradeLevelName,
                    section.SectionName,
                    section.Id;
                """;

			List<AcademicsSchoolYearSection> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@SchoolYearId", schoolYearId);
			command.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				items.Add(MapSection(reader));
			}

			return items;
		}

		public async Task<AcademicsSchoolYearSection?> GetByIdAsync(
			int id,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    section.Id,
                    section.SchoolYearId,
                    sy.Name AS SchoolYearName,
                    section.SourceSectionTemplateId,
                    section.GradeLevelName,
                    section.SectionName,
                    section.AdviserTeacherId,
                    section.AdviserLastName,
                    section.AdviserFirstName,
                    section.AdviserMiddleName,
                    section.AdviserExtensionName,
                    section.AdviserPosition,
                    section.AdviserDesignation,
                    section.SortOrder,
                    section.IsActive
                FROM Academics_SchoolYearSection section
                INNER JOIN Academics_SchoolYear sy
                    ON section.SchoolYearId = sy.Id
                WHERE section.Id = @Id;
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@Id", id);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			if (await reader.ReadAsync(cancellationToken))
			{
				return MapSection(reader);
			}

			return null;
		}

		public async Task<List<AcademicsSchoolYearSectionProgram>> GetProgramsBySectionIdAsync(
			int schoolYearSectionId,
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    link.Id,
                    link.SchoolYearSectionId,
                    section.GradeLevelName,
                    section.SectionName,
                    link.SchoolYearProgramId,
                    program.ProgramCode,
                    program.ProgramName,
                    link.SortOrder,
                    link.IsActive
                FROM Academics_SchoolYearSectionProgram link
                INNER JOIN Academics_SchoolYearSection section
                    ON link.SchoolYearSectionId = section.Id
                INNER JOIN Academics_SchoolYearProgram program
                    ON link.SchoolYearProgramId = program.Id
                WHERE link.SchoolYearSectionId = @SchoolYearSectionId
                  AND (@IncludeInactive = 1 OR link.IsActive = 1)
                ORDER BY
                    link.IsActive DESC,
                    link.SortOrder,
                    program.ProgramName,
                    link.Id;
                """;

			List<AcademicsSchoolYearSectionProgram> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@SchoolYearSectionId", schoolYearSectionId);
			command.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				items.Add(MapSectionProgram(reader));
			}

			return items;
		}

		public async Task<int> InsertAsync(
			AcademicsSchoolYearSection section,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                INSERT INTO Academics_SchoolYearSection
                (
                    SchoolYearId,
                    SourceSectionTemplateId,
                    GradeLevelName,
                    SectionName,
                    AdviserTeacherId,
                    AdviserLastName,
                    AdviserFirstName,
                    AdviserMiddleName,
                    AdviserExtensionName,
                    AdviserPosition,
                    AdviserDesignation,
                    SortOrder,
                    IsActive
                )
                VALUES
                (
                    @SchoolYearId,
                    @SourceSectionTemplateId,
                    @GradeLevelName,
                    @SectionName,
                    @AdviserTeacherId,
                    @AdviserLastName,
                    @AdviserFirstName,
                    @AdviserMiddleName,
                    @AdviserExtensionName,
                    @AdviserPosition,
                    @AdviserDesignation,
                    @SortOrder,
                    @IsActive
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			PrepareForSave(section);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddSectionParameters(command, section);

			object? result = await command.ExecuteScalarAsync(cancellationToken);
			return result is int id ? id : 0;
		}

		public async Task<int> CreateFromTemplateAsync(
			int schoolYearId,
			int sectionTemplateId,
			int? adviserTeacherId = null,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                INSERT INTO Academics_SchoolYearSection
                (
                    SchoolYearId,
                    SourceSectionTemplateId,
                    GradeLevelName,
                    SectionName,
                    AdviserTeacherId,
                    AdviserLastName,
                    AdviserFirstName,
                    AdviserMiddleName,
                    AdviserExtensionName,
                    AdviserPosition,
                    AdviserDesignation,
                    SortOrder,
                    IsActive
                )
                SELECT
                    @SchoolYearId,
                    template.Id,
                    template.GradeLevelName,
                    template.SectionName,
                    teacher.Id,
                    teacher.LastName,
                    teacher.FirstName,
                    teacher.MiddleName,
                    teacher.ExtensionName,
                    teacher.Position,
                    teacher.Designation,
                    template.SortOrder,
                    1
                FROM Academics_SectionTemplate template
                LEFT JOIN Academics_Teacher teacher
                    ON teacher.Id = @AdviserTeacherId
                WHERE template.Id = @SectionTemplateId;

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@SchoolYearId", schoolYearId);
			command.Parameters.AddWithValue("@SectionTemplateId", sectionTemplateId);
			command.Parameters.AddWithValue("@AdviserTeacherId", ToDbNullable(adviserTeacherId));

			object? result = await command.ExecuteScalarAsync(cancellationToken);
			return result is int id ? id : 0;
		}

		public async Task UpdateAsync(
			AcademicsSchoolYearSection section,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_SchoolYearSection
                SET
                    SchoolYearId = @SchoolYearId,
                    SourceSectionTemplateId = @SourceSectionTemplateId,
                    GradeLevelName = @GradeLevelName,
                    SectionName = @SectionName,
                    AdviserTeacherId = @AdviserTeacherId,
                    AdviserLastName = @AdviserLastName,
                    AdviserFirstName = @AdviserFirstName,
                    AdviserMiddleName = @AdviserMiddleName,
                    AdviserExtensionName = @AdviserExtensionName,
                    AdviserPosition = @AdviserPosition,
                    AdviserDesignation = @AdviserDesignation,
                    SortOrder = @SortOrder,
                    IsActive = @IsActive,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			PrepareForSave(section);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddSectionParameters(command, section);
			command.Parameters.AddWithValue("@Id", section.Id);

			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		public async Task AssignAdviserAsync(
			int schoolYearSectionId,
			int? adviserTeacherId,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE section
                SET
                    AdviserTeacherId = teacher.Id,
                    AdviserLastName = teacher.LastName,
                    AdviserFirstName = teacher.FirstName,
                    AdviserMiddleName = teacher.MiddleName,
                    AdviserExtensionName = teacher.ExtensionName,
                    AdviserPosition = teacher.Position,
                    AdviserDesignation = teacher.Designation,
                    UpdatedAt = GETDATE()
                FROM Academics_SchoolYearSection section
                LEFT JOIN Academics_Teacher teacher
                    ON teacher.Id = @AdviserTeacherId
                WHERE section.Id = @SchoolYearSectionId;
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@SchoolYearSectionId", schoolYearSectionId);
			command.Parameters.AddWithValue("@AdviserTeacherId", ToDbNullable(adviserTeacherId));

			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		public async Task<int> LinkProgramAsync(
			int schoolYearSectionId,
			int schoolYearProgramId,
			int sortOrder = 0,
			CancellationToken cancellationToken = default)
		{
			const string existingSql =
				"""
                SELECT Id
                FROM Academics_SchoolYearSectionProgram
                WHERE SchoolYearSectionId = @SchoolYearSectionId
                  AND SchoolYearProgramId = @SchoolYearProgramId;
                """;

			const string reactivateSql =
				"""
                UPDATE Academics_SchoolYearSectionProgram
                SET
                    SortOrder = @SortOrder,
                    IsActive = 1,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			const string insertSql =
				"""
                INSERT INTO Academics_SchoolYearSectionProgram
                (
                    SchoolYearSectionId,
                    SchoolYearProgramId,
                    SortOrder,
                    IsActive
                )
                VALUES
                (
                    @SchoolYearSectionId,
                    @SchoolYearProgramId,
                    @SortOrder,
                    1
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlTransaction transaction = connection.BeginTransaction();

			try
			{
				using SqlCommand existingCommand = new(existingSql, connection, transaction);
				existingCommand.Parameters.AddWithValue("@SchoolYearSectionId", schoolYearSectionId);
				existingCommand.Parameters.AddWithValue("@SchoolYearProgramId", schoolYearProgramId);

				object? existingResult = await existingCommand.ExecuteScalarAsync(cancellationToken);

				if (existingResult is int existingId)
				{
					using SqlCommand reactivateCommand = new(reactivateSql, connection, transaction);
					reactivateCommand.Parameters.AddWithValue("@Id", existingId);
					reactivateCommand.Parameters.AddWithValue("@SortOrder", Math.Max(0, sortOrder));

					await reactivateCommand.ExecuteNonQueryAsync(cancellationToken);

					transaction.Commit();

					return existingId;
				}

				using SqlCommand insertCommand = new(insertSql, connection, transaction);
				insertCommand.Parameters.AddWithValue("@SchoolYearSectionId", schoolYearSectionId);
				insertCommand.Parameters.AddWithValue("@SchoolYearProgramId", schoolYearProgramId);
				insertCommand.Parameters.AddWithValue("@SortOrder", Math.Max(0, sortOrder));

				object? insertResult = await insertCommand.ExecuteScalarAsync(cancellationToken);

				transaction.Commit();

				return insertResult is int newId ? newId : 0;
			}
			catch
			{
				transaction.Rollback();
				throw;
			}
		}

		public async Task SetSectionIsActiveAsync(
			int id,
			bool isActive,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_SchoolYearSection
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

		public async Task SetSectionProgramIsActiveAsync(
			int id,
			bool isActive,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_SchoolYearSectionProgram
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

		private static AcademicsSchoolYearSection MapSection(SqlDataReader reader)
		{
			return new AcademicsSchoolYearSection
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				SchoolYearId = reader.GetInt32(reader.GetOrdinal("SchoolYearId")),
				SchoolYearName = reader["SchoolYearName"] as string ?? string.Empty,
				SourceSectionTemplateId = reader["SourceSectionTemplateId"] == DBNull.Value
					? null
					: reader.GetInt32(reader.GetOrdinal("SourceSectionTemplateId")),
				GradeLevelName = reader["GradeLevelName"] as string ?? string.Empty,
				SectionName = reader["SectionName"] as string ?? string.Empty,
				AdviserTeacherId = reader["AdviserTeacherId"] == DBNull.Value
					? null
					: reader.GetInt32(reader.GetOrdinal("AdviserTeacherId")),
				AdviserLastName = reader["AdviserLastName"] as string ?? string.Empty,
				AdviserFirstName = reader["AdviserFirstName"] as string ?? string.Empty,
				AdviserMiddleName = reader["AdviserMiddleName"] as string ?? string.Empty,
				AdviserExtensionName = reader["AdviserExtensionName"] as string ?? string.Empty,
				AdviserPosition = reader["AdviserPosition"] as string ?? string.Empty,
				AdviserDesignation = reader["AdviserDesignation"] as string ?? string.Empty,
				SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
			};
		}

		private static AcademicsSchoolYearSectionProgram MapSectionProgram(SqlDataReader reader)
		{
			return new AcademicsSchoolYearSectionProgram
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				SchoolYearSectionId = reader.GetInt32(reader.GetOrdinal("SchoolYearSectionId")),
				GradeLevelName = reader["GradeLevelName"] as string ?? string.Empty,
				SectionName = reader["SectionName"] as string ?? string.Empty,
				SchoolYearProgramId = reader.GetInt32(reader.GetOrdinal("SchoolYearProgramId")),
				ProgramCode = reader["ProgramCode"] as string ?? string.Empty,
				ProgramName = reader["ProgramName"] as string ?? string.Empty,
				SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
			};
		}

		private static void PrepareForSave(AcademicsSchoolYearSection section)
		{
			section.GradeLevelName = NormalizeRequired(section.GradeLevelName);
			section.SectionName = NormalizeRequired(section.SectionName);

			section.AdviserLastName = Normalize(section.AdviserLastName) ?? string.Empty;
			section.AdviserFirstName = Normalize(section.AdviserFirstName) ?? string.Empty;
			section.AdviserMiddleName = Normalize(section.AdviserMiddleName) ?? string.Empty;
			section.AdviserExtensionName = Normalize(section.AdviserExtensionName) ?? string.Empty;
			section.AdviserPosition = Normalize(section.AdviserPosition) ?? string.Empty;
			section.AdviserDesignation = Normalize(section.AdviserDesignation) ?? string.Empty;

			section.SortOrder = Math.Max(0, section.SortOrder);
		}

		private static void AddSectionParameters(
			SqlCommand command,
			AcademicsSchoolYearSection section)
		{
			command.Parameters.AddWithValue("@SchoolYearId", section.SchoolYearId);
			command.Parameters.AddWithValue("@SourceSectionTemplateId", ToDbNullable(section.SourceSectionTemplateId));
			command.Parameters.AddWithValue("@GradeLevelName", section.GradeLevelName);
			command.Parameters.AddWithValue("@SectionName", section.SectionName);
			command.Parameters.AddWithValue("@AdviserTeacherId", ToDbNullable(section.AdviserTeacherId));
			command.Parameters.AddWithValue("@AdviserLastName", ToDbNullable(section.AdviserLastName));
			command.Parameters.AddWithValue("@AdviserFirstName", ToDbNullable(section.AdviserFirstName));
			command.Parameters.AddWithValue("@AdviserMiddleName", ToDbNullable(section.AdviserMiddleName));
			command.Parameters.AddWithValue("@AdviserExtensionName", ToDbNullable(section.AdviserExtensionName));
			command.Parameters.AddWithValue("@AdviserPosition", ToDbNullable(section.AdviserPosition));
			command.Parameters.AddWithValue("@AdviserDesignation", ToDbNullable(section.AdviserDesignation));
			command.Parameters.AddWithValue("@SortOrder", section.SortOrder);
			command.Parameters.AddWithValue("@IsActive", section.IsActive);
		}

		private static object ToDbNullable(int? value)
		{
			return value.HasValue ? value.Value : DBNull.Value;
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
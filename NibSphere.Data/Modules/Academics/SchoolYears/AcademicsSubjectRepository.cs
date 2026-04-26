using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Academics.Subjects;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Academics.Subjects
{
	public sealed class AcademicsSubjectRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public AcademicsSubjectRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<AcademicsSubject>> GetBySectionAndTermAsync(
			int termId,
			int sectionId,
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    subject.Id,
                    subject.SchoolYearId,
                    sy.Name AS SchoolYearName,
                    subject.TermId,
                    term.Name AS TermName,
                    subject.SectionId,
                    section.GradeLevelName,
                    section.SectionName,
                    subject.LearningAreaId,
                    la.Code AS LearningAreaCode,
                    la.ShortName AS LearningAreaShortName,
                    la.Description AS LearningAreaDescription,
                    subject.SchoolYearProgramLineId,
                    subject.TeacherId,
                    subject.TeacherLastName,
                    subject.TeacherFirstName,
                    subject.TeacherMiddleName,
                    subject.TeacherExtensionName,
                    subject.TeacherPosition,
                    subject.TeacherDesignation,
                    subject.SubjectCode,
                    subject.SubjectName,
                    subject.SortOrder,
                    subject.IsActive
                FROM Academics_Subject subject
                INNER JOIN Academics_SchoolYear sy
                    ON subject.SchoolYearId = sy.Id
                INNER JOIN Academics_SchoolYearTerm term
                    ON subject.TermId = term.Id
                INNER JOIN Academics_SchoolYearSection section
                    ON subject.SectionId = section.Id
                INNER JOIN LearningArea la
                    ON subject.LearningAreaId = la.Id
                WHERE subject.TermId = @TermId
                  AND subject.SectionId = @SectionId
                  AND (@IncludeInactive = 1 OR subject.IsActive = 1)
                ORDER BY
                    subject.IsActive DESC,
                    subject.SortOrder,
                    la.Sort,
                    la.Description,
                    subject.Id;
                """;

			List<AcademicsSubject> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@TermId", termId);
			command.Parameters.AddWithValue("@SectionId", sectionId);
			command.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				items.Add(MapSubject(reader));
			}

			return items;
		}

		public async Task<AcademicsSubject?> GetByIdAsync(
			int id,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    subject.Id,
                    subject.SchoolYearId,
                    sy.Name AS SchoolYearName,
                    subject.TermId,
                    term.Name AS TermName,
                    subject.SectionId,
                    section.GradeLevelName,
                    section.SectionName,
                    subject.LearningAreaId,
                    la.Code AS LearningAreaCode,
                    la.ShortName AS LearningAreaShortName,
                    la.Description AS LearningAreaDescription,
                    subject.SchoolYearProgramLineId,
                    subject.TeacherId,
                    subject.TeacherLastName,
                    subject.TeacherFirstName,
                    subject.TeacherMiddleName,
                    subject.TeacherExtensionName,
                    subject.TeacherPosition,
                    subject.TeacherDesignation,
                    subject.SubjectCode,
                    subject.SubjectName,
                    subject.SortOrder,
                    subject.IsActive
                FROM Academics_Subject subject
                INNER JOIN Academics_SchoolYear sy
                    ON subject.SchoolYearId = sy.Id
                INNER JOIN Academics_SchoolYearTerm term
                    ON subject.TermId = term.Id
                INNER JOIN Academics_SchoolYearSection section
                    ON subject.SectionId = section.Id
                INNER JOIN LearningArea la
                    ON subject.LearningAreaId = la.Id
                WHERE subject.Id = @Id;
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@Id", id);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			if (await reader.ReadAsync(cancellationToken))
			{
				return MapSubject(reader);
			}

			return null;
		}

		public async Task<int> InsertAsync(
			AcademicsSubject subject,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                INSERT INTO Academics_Subject
                (
                    SchoolYearId,
                    TermId,
                    SectionId,
                    LearningAreaId,
                    SchoolYearProgramLineId,
                    TeacherId,
                    TeacherLastName,
                    TeacherFirstName,
                    TeacherMiddleName,
                    TeacherExtensionName,
                    TeacherPosition,
                    TeacherDesignation,
                    SubjectCode,
                    SubjectName,
                    SortOrder,
                    IsActive
                )
                VALUES
                (
                    @SchoolYearId,
                    @TermId,
                    @SectionId,
                    @LearningAreaId,
                    @SchoolYearProgramLineId,
                    @TeacherId,
                    @TeacherLastName,
                    @TeacherFirstName,
                    @TeacherMiddleName,
                    @TeacherExtensionName,
                    @TeacherPosition,
                    @TeacherDesignation,
                    @SubjectCode,
                    @SubjectName,
                    @SortOrder,
                    @IsActive
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			PrepareForSave(subject);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddSubjectParameters(command, subject);

			object? result = await command.ExecuteScalarAsync(cancellationToken);
			return result is int id ? id : 0;
		}

		public async Task UpdateAsync(
			AcademicsSubject subject,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_Subject
                SET
                    SchoolYearId = @SchoolYearId,
                    TermId = @TermId,
                    SectionId = @SectionId,
                    LearningAreaId = @LearningAreaId,
                    SchoolYearProgramLineId = @SchoolYearProgramLineId,
                    TeacherId = @TeacherId,
                    TeacherLastName = @TeacherLastName,
                    TeacherFirstName = @TeacherFirstName,
                    TeacherMiddleName = @TeacherMiddleName,
                    TeacherExtensionName = @TeacherExtensionName,
                    TeacherPosition = @TeacherPosition,
                    TeacherDesignation = @TeacherDesignation,
                    SubjectCode = @SubjectCode,
                    SubjectName = @SubjectName,
                    SortOrder = @SortOrder,
                    IsActive = @IsActive,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			PrepareForSave(subject);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddSubjectParameters(command, subject);
			command.Parameters.AddWithValue("@Id", subject.Id);

			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		public async Task AssignTeacherAsync(
			int subjectId,
			int? teacherId,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE subject
                SET
                    TeacherId = teacher.Id,
                    TeacherLastName = teacher.LastName,
                    TeacherFirstName = teacher.FirstName,
                    TeacherMiddleName = teacher.MiddleName,
                    TeacherExtensionName = teacher.ExtensionName,
                    TeacherPosition = teacher.Position,
                    TeacherDesignation = teacher.Designation,
                    UpdatedAt = GETDATE()
                FROM Academics_Subject subject
                LEFT JOIN Academics_Teacher teacher
                    ON teacher.Id = @TeacherId
                WHERE subject.Id = @SubjectId;
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@SubjectId", subjectId);
			command.Parameters.AddWithValue("@TeacherId", ToDbNullable(teacherId));

			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		public async Task<int> GenerateFromSectionProgramsAsync(
			int termId,
			int sectionId,
			int templateTermSequence,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                INSERT INTO Academics_Subject
                (
                    SchoolYearId,
                    TermId,
                    SectionId,
                    LearningAreaId,
                    SchoolYearProgramLineId,
                    SubjectCode,
                    SubjectName,
                    SortOrder,
                    IsActive
                )
                SELECT
                    section.SchoolYearId,
                    @TermId,
                    section.Id,
                    programLine.LearningAreaId,
                    programLine.Id,
                    programLine.LearningAreaCode,
                    programLine.LearningAreaShortName,
                    programLine.SortOrder,
                    1
                FROM Academics_SchoolYearSection section
                INNER JOIN Academics_SchoolYearTerm term
                    ON term.Id = @TermId
                   AND term.SchoolYearId = section.SchoolYearId
                INNER JOIN Academics_SchoolYearSectionProgram sectionProgram
                    ON sectionProgram.SchoolYearSectionId = section.Id
                   AND sectionProgram.IsActive = 1
                INNER JOIN Academics_SchoolYearProgram schoolYearProgram
                    ON sectionProgram.SchoolYearProgramId = schoolYearProgram.Id
                   AND schoolYearProgram.IsActive = 1
                INNER JOIN Academics_SchoolYearProgramLine programLine
                    ON programLine.SchoolYearProgramId = schoolYearProgram.Id
                   AND programLine.IsActive = 1
                   AND programLine.GradeLevelName = section.GradeLevelName
                   AND programLine.TemplateTermSequence = @TemplateTermSequence
                WHERE section.Id = @SectionId
                  AND section.IsActive = 1
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM Academics_Subject existing
                      WHERE existing.TermId = @TermId
                        AND existing.SectionId = section.Id
                        AND existing.LearningAreaId = programLine.LearningAreaId
                  );
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@TermId", termId);
			command.Parameters.AddWithValue("@SectionId", sectionId);
			command.Parameters.AddWithValue("@TemplateTermSequence", Math.Max(1, templateTermSequence));

			return await command.ExecuteNonQueryAsync(cancellationToken);
		}

		public async Task SetSubjectIsActiveAsync(
			int id,
			bool isActive,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_Subject
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

		public async Task<List<AcademicsSubjectScheduleSlot>> GetScheduleSlotsAsync(
			int subjectId,
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    Id,
                    SubjectId,
                    DayOfWeekNumber,
                    DayOfWeekName,
                    StartTime,
                    EndTime,
                    Room,
                    SortOrder,
                    IsActive
                FROM Academics_SubjectScheduleSlot
                WHERE SubjectId = @SubjectId
                  AND (@IncludeInactive = 1 OR IsActive = 1)
                ORDER BY
                    IsActive DESC,
                    SortOrder,
                    DayOfWeekNumber,
                    StartTime,
                    Id;
                """;

			List<AcademicsSubjectScheduleSlot> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@SubjectId", subjectId);
			command.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				items.Add(MapScheduleSlot(reader));
			}

			return items;
		}

		public async Task<int> InsertScheduleSlotAsync(
			AcademicsSubjectScheduleSlot slot,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                INSERT INTO Academics_SubjectScheduleSlot
                (
                    SubjectId,
                    DayOfWeekNumber,
                    DayOfWeekName,
                    StartTime,
                    EndTime,
                    Room,
                    SortOrder,
                    IsActive
                )
                VALUES
                (
                    @SubjectId,
                    @DayOfWeekNumber,
                    @DayOfWeekName,
                    @StartTime,
                    @EndTime,
                    @Room,
                    @SortOrder,
                    @IsActive
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			PrepareScheduleSlotForSave(slot);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddScheduleSlotParameters(command, slot);

			object? result = await command.ExecuteScalarAsync(cancellationToken);
			return result is int id ? id : 0;
		}

		public async Task UpdateScheduleSlotAsync(
			AcademicsSubjectScheduleSlot slot,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_SubjectScheduleSlot
                SET
                    SubjectId = @SubjectId,
                    DayOfWeekNumber = @DayOfWeekNumber,
                    DayOfWeekName = @DayOfWeekName,
                    StartTime = @StartTime,
                    EndTime = @EndTime,
                    Room = @Room,
                    SortOrder = @SortOrder,
                    IsActive = @IsActive,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			PrepareScheduleSlotForSave(slot);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddScheduleSlotParameters(command, slot);
			command.Parameters.AddWithValue("@Id", slot.Id);

			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		public async Task SetScheduleSlotIsActiveAsync(
			int id,
			bool isActive,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_SubjectScheduleSlot
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

		private static AcademicsSubject MapSubject(SqlDataReader reader)
		{
			return new AcademicsSubject
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				SchoolYearId = reader.GetInt32(reader.GetOrdinal("SchoolYearId")),
				SchoolYearName = reader["SchoolYearName"] as string ?? string.Empty,
				TermId = reader.GetInt32(reader.GetOrdinal("TermId")),
				TermName = reader["TermName"] as string ?? string.Empty,
				SectionId = reader.GetInt32(reader.GetOrdinal("SectionId")),
				GradeLevelName = reader["GradeLevelName"] as string ?? string.Empty,
				SectionName = reader["SectionName"] as string ?? string.Empty,
				LearningAreaId = reader.GetInt32(reader.GetOrdinal("LearningAreaId")),
				LearningAreaCode = reader["LearningAreaCode"] as string ?? string.Empty,
				LearningAreaShortName = reader["LearningAreaShortName"] as string ?? string.Empty,
				LearningAreaDescription = reader["LearningAreaDescription"] as string ?? string.Empty,
				SchoolYearProgramLineId = reader["SchoolYearProgramLineId"] == DBNull.Value
					? null
					: reader.GetInt32(reader.GetOrdinal("SchoolYearProgramLineId")),
				TeacherId = reader["TeacherId"] == DBNull.Value
					? null
					: reader.GetInt32(reader.GetOrdinal("TeacherId")),
				TeacherLastName = reader["TeacherLastName"] as string ?? string.Empty,
				TeacherFirstName = reader["TeacherFirstName"] as string ?? string.Empty,
				TeacherMiddleName = reader["TeacherMiddleName"] as string ?? string.Empty,
				TeacherExtensionName = reader["TeacherExtensionName"] as string ?? string.Empty,
				TeacherPosition = reader["TeacherPosition"] as string ?? string.Empty,
				TeacherDesignation = reader["TeacherDesignation"] as string ?? string.Empty,
				SubjectCode = reader["SubjectCode"] as string ?? string.Empty,
				SubjectName = reader["SubjectName"] as string ?? string.Empty,
				SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
			};
		}

		private static AcademicsSubjectScheduleSlot MapScheduleSlot(SqlDataReader reader)
		{
			return new AcademicsSubjectScheduleSlot
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				SubjectId = reader.GetInt32(reader.GetOrdinal("SubjectId")),
				DayOfWeekNumber = reader.GetInt32(reader.GetOrdinal("DayOfWeekNumber")),
				DayOfWeekName = reader["DayOfWeekName"] as string ?? string.Empty,
				StartTime = reader["StartTime"] == DBNull.Value
					? null
					: reader.GetTimeSpan(reader.GetOrdinal("StartTime")),
				EndTime = reader["EndTime"] == DBNull.Value
					? null
					: reader.GetTimeSpan(reader.GetOrdinal("EndTime")),
				Room = reader["Room"] as string ?? string.Empty,
				SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
			};
		}

		private static void PrepareForSave(AcademicsSubject subject)
		{
			subject.TeacherLastName = Normalize(subject.TeacherLastName) ?? string.Empty;
			subject.TeacherFirstName = Normalize(subject.TeacherFirstName) ?? string.Empty;
			subject.TeacherMiddleName = Normalize(subject.TeacherMiddleName) ?? string.Empty;
			subject.TeacherExtensionName = Normalize(subject.TeacherExtensionName) ?? string.Empty;
			subject.TeacherPosition = Normalize(subject.TeacherPosition) ?? string.Empty;
			subject.TeacherDesignation = Normalize(subject.TeacherDesignation) ?? string.Empty;
			subject.SubjectCode = Normalize(subject.SubjectCode) ?? string.Empty;
			subject.SubjectName = Normalize(subject.SubjectName) ?? string.Empty;
			subject.SortOrder = Math.Max(0, subject.SortOrder);
		}

		private static void PrepareScheduleSlotForSave(AcademicsSubjectScheduleSlot slot)
		{
			slot.DayOfWeekNumber = Math.Clamp(slot.DayOfWeekNumber, 0, 7);
			slot.DayOfWeekName = NormalizeRequired(slot.DayOfWeekName);
			slot.Room = Normalize(slot.Room) ?? string.Empty;
			slot.SortOrder = Math.Max(0, slot.SortOrder);
		}

		private static void AddSubjectParameters(SqlCommand command, AcademicsSubject subject)
		{
			command.Parameters.AddWithValue("@SchoolYearId", subject.SchoolYearId);
			command.Parameters.AddWithValue("@TermId", subject.TermId);
			command.Parameters.AddWithValue("@SectionId", subject.SectionId);
			command.Parameters.AddWithValue("@LearningAreaId", subject.LearningAreaId);
			command.Parameters.AddWithValue("@SchoolYearProgramLineId", ToDbNullable(subject.SchoolYearProgramLineId));
			command.Parameters.AddWithValue("@TeacherId", ToDbNullable(subject.TeacherId));
			command.Parameters.AddWithValue("@TeacherLastName", ToDbNullable(subject.TeacherLastName));
			command.Parameters.AddWithValue("@TeacherFirstName", ToDbNullable(subject.TeacherFirstName));
			command.Parameters.AddWithValue("@TeacherMiddleName", ToDbNullable(subject.TeacherMiddleName));
			command.Parameters.AddWithValue("@TeacherExtensionName", ToDbNullable(subject.TeacherExtensionName));
			command.Parameters.AddWithValue("@TeacherPosition", ToDbNullable(subject.TeacherPosition));
			command.Parameters.AddWithValue("@TeacherDesignation", ToDbNullable(subject.TeacherDesignation));
			command.Parameters.AddWithValue("@SubjectCode", ToDbNullable(subject.SubjectCode));
			command.Parameters.AddWithValue("@SubjectName", ToDbNullable(subject.SubjectName));
			command.Parameters.AddWithValue("@SortOrder", subject.SortOrder);
			command.Parameters.AddWithValue("@IsActive", subject.IsActive);
		}

		private static void AddScheduleSlotParameters(
			SqlCommand command,
			AcademicsSubjectScheduleSlot slot)
		{
			command.Parameters.AddWithValue("@SubjectId", slot.SubjectId);
			command.Parameters.AddWithValue("@DayOfWeekNumber", slot.DayOfWeekNumber);
			command.Parameters.AddWithValue("@DayOfWeekName", slot.DayOfWeekName);
			command.Parameters.AddWithValue("@StartTime", ToDbNullable(slot.StartTime));
			command.Parameters.AddWithValue("@EndTime", ToDbNullable(slot.EndTime));
			command.Parameters.AddWithValue("@Room", ToDbNullable(slot.Room));
			command.Parameters.AddWithValue("@SortOrder", slot.SortOrder);
			command.Parameters.AddWithValue("@IsActive", slot.IsActive);
		}

		private static object ToDbNullable(int? value)
		{
			return value.HasValue ? value.Value : DBNull.Value;
		}

		private static object ToDbNullable(TimeSpan? value)
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
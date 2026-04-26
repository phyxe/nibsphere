using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Academics.Enrollments;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Academics.Enrollments
{
	public sealed class AcademicsEnrollmentRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public AcademicsEnrollmentRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<AcademicsEnrollment>> GetBySchoolYearIdAsync(
			int schoolYearId,
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    enrollment.Id,
                    enrollment.LearnerId,
                    learner.LastName AS LearnerLastName,
                    learner.FirstName AS LearnerFirstName,
                    learner.MiddleName AS LearnerMiddleName,
                    learner.ExtensionName AS LearnerExtensionName,
                    learner.Lrn AS LearnerLrn,
                    enrollment.SchoolYearId,
                    schoolYear.Name AS SchoolYearName,
                    enrollment.SchoolYearProgramId,
                    program.ProgramCode,
                    program.ProgramName,
                    enrollment.SchoolYearSectionId,
                    section.GradeLevelName,
                    section.SectionName,
                    enrollment.EnrollmentStatusId,
                    enrollment.EnrollmentStatusCode,
                    enrollment.EnrollmentStatusName,
                    enrollment.EnrollmentDate,
                    enrollment.EffectiveStartDate,
                    enrollment.EffectiveEndDate,
                    enrollment.IsCurrent,
                    enrollment.IsActive,
                    enrollment.Remarks
                FROM Academics_Enrollment enrollment
                INNER JOIN Learners_Learner learner
                    ON enrollment.LearnerId = learner.Id
                INNER JOIN Academics_SchoolYear schoolYear
                    ON enrollment.SchoolYearId = schoolYear.Id
                INNER JOIN Academics_SchoolYearProgram program
                    ON enrollment.SchoolYearProgramId = program.Id
                INNER JOIN Academics_SchoolYearSection section
                    ON enrollment.SchoolYearSectionId = section.Id
                WHERE enrollment.SchoolYearId = @SchoolYearId
                  AND (@IncludeInactive = 1 OR enrollment.IsActive = 1)
                ORDER BY
                    enrollment.IsCurrent DESC,
                    enrollment.IsActive DESC,
                    section.GradeLevelName,
                    section.SectionName,
                    learner.LastName,
                    learner.FirstName,
                    enrollment.Id;
                """;

			List<AcademicsEnrollment> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@SchoolYearId", schoolYearId);
			command.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				items.Add(MapEnrollment(reader));
			}

			return items;
		}

		public async Task<List<AcademicsEnrollment>> GetByLearnerIdAsync(
			int learnerId,
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    enrollment.Id,
                    enrollment.LearnerId,
                    learner.LastName AS LearnerLastName,
                    learner.FirstName AS LearnerFirstName,
                    learner.MiddleName AS LearnerMiddleName,
                    learner.ExtensionName AS LearnerExtensionName,
                    learner.Lrn AS LearnerLrn,
                    enrollment.SchoolYearId,
                    schoolYear.Name AS SchoolYearName,
                    enrollment.SchoolYearProgramId,
                    program.ProgramCode,
                    program.ProgramName,
                    enrollment.SchoolYearSectionId,
                    section.GradeLevelName,
                    section.SectionName,
                    enrollment.EnrollmentStatusId,
                    enrollment.EnrollmentStatusCode,
                    enrollment.EnrollmentStatusName,
                    enrollment.EnrollmentDate,
                    enrollment.EffectiveStartDate,
                    enrollment.EffectiveEndDate,
                    enrollment.IsCurrent,
                    enrollment.IsActive,
                    enrollment.Remarks
                FROM Academics_Enrollment enrollment
                INNER JOIN Learners_Learner learner
                    ON enrollment.LearnerId = learner.Id
                INNER JOIN Academics_SchoolYear schoolYear
                    ON enrollment.SchoolYearId = schoolYear.Id
                INNER JOIN Academics_SchoolYearProgram program
                    ON enrollment.SchoolYearProgramId = program.Id
                INNER JOIN Academics_SchoolYearSection section
                    ON enrollment.SchoolYearSectionId = section.Id
                WHERE enrollment.LearnerId = @LearnerId
                  AND (@IncludeInactive = 1 OR enrollment.IsActive = 1)
                ORDER BY
                    schoolYear.StartDate DESC,
                    enrollment.IsCurrent DESC,
                    enrollment.Id DESC;
                """;

			List<AcademicsEnrollment> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@LearnerId", learnerId);
			command.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				items.Add(MapEnrollment(reader));
			}

			return items;
		}

		public async Task<AcademicsEnrollment?> GetByIdAsync(
			int id,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    enrollment.Id,
                    enrollment.LearnerId,
                    learner.LastName AS LearnerLastName,
                    learner.FirstName AS LearnerFirstName,
                    learner.MiddleName AS LearnerMiddleName,
                    learner.ExtensionName AS LearnerExtensionName,
                    learner.Lrn AS LearnerLrn,
                    enrollment.SchoolYearId,
                    schoolYear.Name AS SchoolYearName,
                    enrollment.SchoolYearProgramId,
                    program.ProgramCode,
                    program.ProgramName,
                    enrollment.SchoolYearSectionId,
                    section.GradeLevelName,
                    section.SectionName,
                    enrollment.EnrollmentStatusId,
                    enrollment.EnrollmentStatusCode,
                    enrollment.EnrollmentStatusName,
                    enrollment.EnrollmentDate,
                    enrollment.EffectiveStartDate,
                    enrollment.EffectiveEndDate,
                    enrollment.IsCurrent,
                    enrollment.IsActive,
                    enrollment.Remarks
                FROM Academics_Enrollment enrollment
                INNER JOIN Learners_Learner learner
                    ON enrollment.LearnerId = learner.Id
                INNER JOIN Academics_SchoolYear schoolYear
                    ON enrollment.SchoolYearId = schoolYear.Id
                INNER JOIN Academics_SchoolYearProgram program
                    ON enrollment.SchoolYearProgramId = program.Id
                INNER JOIN Academics_SchoolYearSection section
                    ON enrollment.SchoolYearSectionId = section.Id
                WHERE enrollment.Id = @Id;
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@Id", id);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			if (await reader.ReadAsync(cancellationToken))
			{
				return MapEnrollment(reader);
			}

			return null;
		}

		public async Task<int> InsertAsync(
			AcademicsEnrollment enrollment,
			CancellationToken cancellationToken = default)
		{
			const string clearCurrentSql =
				"""
                UPDATE Academics_Enrollment
                SET
                    IsCurrent = 0,
                    UpdatedAt = GETDATE()
                WHERE LearnerId = @LearnerId
                  AND SchoolYearId = @SchoolYearId
                  AND IsCurrent = 1
                  AND IsActive = 1;
                """;

			const string snapshotSql =
				"""
                SELECT
                    section.GradeLevelName,
                    status.Code AS EnrollmentStatusCode,
                    status.Name AS EnrollmentStatusName
                FROM Academics_SchoolYearSection section
                CROSS JOIN Academics_EnrollmentStatus status
                WHERE section.Id = @SchoolYearSectionId
                  AND status.Id = @EnrollmentStatusId;
                """;

			const string insertSql =
				"""
                INSERT INTO Academics_Enrollment
                (
                    LearnerId,
                    SchoolYearId,
                    SchoolYearProgramId,
                    SchoolYearSectionId,
                    GradeLevelName,
                    EnrollmentStatusId,
                    EnrollmentStatusCode,
                    EnrollmentStatusName,
                    EnrollmentDate,
                    EffectiveStartDate,
                    EffectiveEndDate,
                    IsCurrent,
                    IsActive,
                    Remarks
                )
                VALUES
                (
                    @LearnerId,
                    @SchoolYearId,
                    @SchoolYearProgramId,
                    @SchoolYearSectionId,
                    @GradeLevelName,
                    @EnrollmentStatusId,
                    @EnrollmentStatusCode,
                    @EnrollmentStatusName,
                    @EnrollmentDate,
                    @EffectiveStartDate,
                    @EffectiveEndDate,
                    @IsCurrent,
                    @IsActive,
                    @Remarks
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			PrepareForSave(enrollment);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlTransaction transaction = connection.BeginTransaction();

			try
			{
				if (enrollment.IsCurrent && enrollment.IsActive)
				{
					using SqlCommand clearCommand = new(clearCurrentSql, connection, transaction);
					clearCommand.Parameters.AddWithValue("@LearnerId", enrollment.LearnerId);
					clearCommand.Parameters.AddWithValue("@SchoolYearId", enrollment.SchoolYearId);
					await clearCommand.ExecuteNonQueryAsync(cancellationToken);
				}

				using (SqlCommand snapshotCommand = new(snapshotSql, connection, transaction))
				{
					snapshotCommand.Parameters.AddWithValue("@SchoolYearSectionId", enrollment.SchoolYearSectionId);
					snapshotCommand.Parameters.AddWithValue("@EnrollmentStatusId", enrollment.EnrollmentStatusId);

					using SqlDataReader reader = await snapshotCommand.ExecuteReaderAsync(cancellationToken);

					if (await reader.ReadAsync(cancellationToken))
					{
						enrollment.GradeLevelName = reader["GradeLevelName"] as string ?? string.Empty;
						enrollment.EnrollmentStatusCode = reader["EnrollmentStatusCode"] as string ?? string.Empty;
						enrollment.EnrollmentStatusName = reader["EnrollmentStatusName"] as string ?? string.Empty;
					}
					else
					{
						throw new InvalidOperationException("The enrollment section or status could not be found.");
					}
				}

				using SqlCommand insertCommand = new(insertSql, connection, transaction);
				AddEnrollmentParameters(insertCommand, enrollment);

				object? result = await insertCommand.ExecuteScalarAsync(cancellationToken);

				transaction.Commit();

				return result is int id ? id : 0;
			}
			catch
			{
				transaction.Rollback();
				throw;
			}
		}

		public async Task UpdateAsync(
			AcademicsEnrollment enrollment,
			CancellationToken cancellationToken = default)
		{
			const string clearCurrentSql =
				"""
                UPDATE Academics_Enrollment
                SET
                    IsCurrent = 0,
                    UpdatedAt = GETDATE()
                WHERE LearnerId = @LearnerId
                  AND SchoolYearId = @SchoolYearId
                  AND Id <> @Id
                  AND IsCurrent = 1
                  AND IsActive = 1;
                """;

			const string snapshotSql =
				"""
                SELECT
                    section.GradeLevelName,
                    status.Code AS EnrollmentStatusCode,
                    status.Name AS EnrollmentStatusName
                FROM Academics_SchoolYearSection section
                CROSS JOIN Academics_EnrollmentStatus status
                WHERE section.Id = @SchoolYearSectionId
                  AND status.Id = @EnrollmentStatusId;
                """;

			const string updateSql =
				"""
                UPDATE Academics_Enrollment
                SET
                    LearnerId = @LearnerId,
                    SchoolYearId = @SchoolYearId,
                    SchoolYearProgramId = @SchoolYearProgramId,
                    SchoolYearSectionId = @SchoolYearSectionId,
                    GradeLevelName = @GradeLevelName,
                    EnrollmentStatusId = @EnrollmentStatusId,
                    EnrollmentStatusCode = @EnrollmentStatusCode,
                    EnrollmentStatusName = @EnrollmentStatusName,
                    EnrollmentDate = @EnrollmentDate,
                    EffectiveStartDate = @EffectiveStartDate,
                    EffectiveEndDate = @EffectiveEndDate,
                    IsCurrent = @IsCurrent,
                    IsActive = @IsActive,
                    Remarks = @Remarks,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			PrepareForSave(enrollment);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlTransaction transaction = connection.BeginTransaction();

			try
			{
				if (enrollment.IsCurrent && enrollment.IsActive)
				{
					using SqlCommand clearCommand = new(clearCurrentSql, connection, transaction);
					clearCommand.Parameters.AddWithValue("@LearnerId", enrollment.LearnerId);
					clearCommand.Parameters.AddWithValue("@SchoolYearId", enrollment.SchoolYearId);
					clearCommand.Parameters.AddWithValue("@Id", enrollment.Id);
					await clearCommand.ExecuteNonQueryAsync(cancellationToken);
				}

				using (SqlCommand snapshotCommand = new(snapshotSql, connection, transaction))
				{
					snapshotCommand.Parameters.AddWithValue("@SchoolYearSectionId", enrollment.SchoolYearSectionId);
					snapshotCommand.Parameters.AddWithValue("@EnrollmentStatusId", enrollment.EnrollmentStatusId);

					using SqlDataReader reader = await snapshotCommand.ExecuteReaderAsync(cancellationToken);

					if (await reader.ReadAsync(cancellationToken))
					{
						enrollment.GradeLevelName = reader["GradeLevelName"] as string ?? string.Empty;
						enrollment.EnrollmentStatusCode = reader["EnrollmentStatusCode"] as string ?? string.Empty;
						enrollment.EnrollmentStatusName = reader["EnrollmentStatusName"] as string ?? string.Empty;
					}
					else
					{
						throw new InvalidOperationException("The enrollment section or status could not be found.");
					}
				}

				using SqlCommand updateCommand = new(updateSql, connection, transaction);
				AddEnrollmentParameters(updateCommand, enrollment);
				updateCommand.Parameters.AddWithValue("@Id", enrollment.Id);

				await updateCommand.ExecuteNonQueryAsync(cancellationToken);

				transaction.Commit();
			}
			catch
			{
				transaction.Rollback();
				throw;
			}
		}

		public async Task<List<AcademicsEnrollmentSubject>> GetSubjectsByEnrollmentIdAsync(
			int enrollmentId,
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    enrollmentSubject.Id,
                    enrollmentSubject.EnrollmentId,
                    enrollmentSubject.SubjectId,
                    enrollmentSubject.EnrollmentStatusId,
                    enrollmentSubject.EnrollmentStatusCode,
                    enrollmentSubject.EnrollmentStatusName,
                    subject.SubjectCode,
                    subject.SubjectName,
                    learningArea.Code AS LearningAreaCode,
                    learningArea.ShortName AS LearningAreaShortName,
                    learningArea.Description AS LearningAreaDescription,
                    term.Name AS TermName,
                    subject.TeacherLastName,
                    subject.TeacherFirstName,
                    subject.TeacherMiddleName,
                    subject.TeacherExtensionName,
                    enrollmentSubject.EffectiveStartDate,
                    enrollmentSubject.EffectiveEndDate,
                    enrollmentSubject.IsActive
                FROM Academics_EnrollmentSubject enrollmentSubject
                INNER JOIN Academics_Subject subject
                    ON enrollmentSubject.SubjectId = subject.Id
                INNER JOIN LearningArea learningArea
                    ON subject.LearningAreaId = learningArea.Id
                INNER JOIN Academics_SchoolYearTerm term
                    ON subject.TermId = term.Id
                WHERE enrollmentSubject.EnrollmentId = @EnrollmentId
                  AND (@IncludeInactive = 1 OR enrollmentSubject.IsActive = 1)
                ORDER BY
                    enrollmentSubject.IsActive DESC,
                    term.SortOrder,
                    subject.SortOrder,
                    learningArea.Sort,
                    learningArea.Description,
                    enrollmentSubject.Id;
                """;

			List<AcademicsEnrollmentSubject> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@EnrollmentId", enrollmentId);
			command.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				items.Add(MapEnrollmentSubject(reader));
			}

			return items;
		}

		public async Task<int> LinkSubjectAsync(
			int enrollmentId,
			int subjectId,
			int? enrollmentStatusId = null,
			DateTime? effectiveStartDate = null,
			DateTime? effectiveEndDate = null,
			CancellationToken cancellationToken = default)
		{
			const string existingSql =
				"""
                SELECT Id
                FROM Academics_EnrollmentSubject
                WHERE EnrollmentId = @EnrollmentId
                  AND SubjectId = @SubjectId;
                """;

			const string statusSql =
				"""
                SELECT
                    Code,
                    Name
                FROM Academics_EnrollmentStatus
                WHERE Id = @EnrollmentStatusId;
                """;

			const string reactivateSql =
				"""
                UPDATE Academics_EnrollmentSubject
                SET
                    EnrollmentStatusId = @EnrollmentStatusId,
                    EnrollmentStatusCode = @EnrollmentStatusCode,
                    EnrollmentStatusName = @EnrollmentStatusName,
                    EffectiveStartDate = @EffectiveStartDate,
                    EffectiveEndDate = @EffectiveEndDate,
                    IsActive = 1,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			const string insertSql =
				"""
                INSERT INTO Academics_EnrollmentSubject
                (
                    EnrollmentId,
                    SubjectId,
                    EnrollmentStatusId,
                    EnrollmentStatusCode,
                    EnrollmentStatusName,
                    EffectiveStartDate,
                    EffectiveEndDate,
                    IsActive
                )
                VALUES
                (
                    @EnrollmentId,
                    @SubjectId,
                    @EnrollmentStatusId,
                    @EnrollmentStatusCode,
                    @EnrollmentStatusName,
                    @EffectiveStartDate,
                    @EffectiveEndDate,
                    1
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			string? statusCode = null;
			string? statusName = null;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlTransaction transaction = connection.BeginTransaction();

			try
			{
				if (enrollmentStatusId.HasValue)
				{
					using SqlCommand statusCommand = new(statusSql, connection, transaction);
					statusCommand.Parameters.AddWithValue("@EnrollmentStatusId", enrollmentStatusId.Value);

					using SqlDataReader reader = await statusCommand.ExecuteReaderAsync(cancellationToken);

					if (await reader.ReadAsync(cancellationToken))
					{
						statusCode = reader["Code"] as string;
						statusName = reader["Name"] as string;
					}
				}

				using SqlCommand existingCommand = new(existingSql, connection, transaction);
				existingCommand.Parameters.AddWithValue("@EnrollmentId", enrollmentId);
				existingCommand.Parameters.AddWithValue("@SubjectId", subjectId);

				object? existingResult = await existingCommand.ExecuteScalarAsync(cancellationToken);

				if (existingResult is int existingId)
				{
					using SqlCommand reactivateCommand = new(reactivateSql, connection, transaction);
					AddEnrollmentSubjectLinkParameters(
						reactivateCommand,
						enrollmentId,
						subjectId,
						enrollmentStatusId,
						statusCode,
						statusName,
						effectiveStartDate,
						effectiveEndDate);

					reactivateCommand.Parameters.AddWithValue("@Id", existingId);

					await reactivateCommand.ExecuteNonQueryAsync(cancellationToken);

					transaction.Commit();

					return existingId;
				}

				using SqlCommand insertCommand = new(insertSql, connection, transaction);
				AddEnrollmentSubjectLinkParameters(
					insertCommand,
					enrollmentId,
					subjectId,
					enrollmentStatusId,
					statusCode,
					statusName,
					effectiveStartDate,
					effectiveEndDate);

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

		public async Task LinkSubjectsFromSectionAndTermAsync(
			int enrollmentId,
			int termId,
			int sectionId,
			int? enrollmentStatusId = null,
			CancellationToken cancellationToken = default)
		{
			const string statusSql =
				"""
                SELECT
                    Code,
                    Name
                FROM Academics_EnrollmentStatus
                WHERE Id = @EnrollmentStatusId;
                """;

			const string insertSql =
				"""
                INSERT INTO Academics_EnrollmentSubject
                (
                    EnrollmentId,
                    SubjectId,
                    EnrollmentStatusId,
                    EnrollmentStatusCode,
                    EnrollmentStatusName,
                    IsActive
                )
                SELECT
                    @EnrollmentId,
                    subject.Id,
                    @EnrollmentStatusId,
                    @EnrollmentStatusCode,
                    @EnrollmentStatusName,
                    1
                FROM Academics_Subject subject
                WHERE subject.TermId = @TermId
                  AND subject.SectionId = @SectionId
                  AND subject.IsActive = 1
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM Academics_EnrollmentSubject existing
                      WHERE existing.EnrollmentId = @EnrollmentId
                        AND existing.SubjectId = subject.Id
                  );
                """;

			string? statusCode = null;
			string? statusName = null;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlTransaction transaction = connection.BeginTransaction();

			try
			{
				if (enrollmentStatusId.HasValue)
				{
					using SqlCommand statusCommand = new(statusSql, connection, transaction);
					statusCommand.Parameters.AddWithValue("@EnrollmentStatusId", enrollmentStatusId.Value);

					using SqlDataReader reader = await statusCommand.ExecuteReaderAsync(cancellationToken);

					if (await reader.ReadAsync(cancellationToken))
					{
						statusCode = reader["Code"] as string;
						statusName = reader["Name"] as string;
					}
				}

				using SqlCommand insertCommand = new(insertSql, connection, transaction);
				insertCommand.Parameters.AddWithValue("@EnrollmentId", enrollmentId);
				insertCommand.Parameters.AddWithValue("@TermId", termId);
				insertCommand.Parameters.AddWithValue("@SectionId", sectionId);
				insertCommand.Parameters.AddWithValue("@EnrollmentStatusId", ToDbNullable(enrollmentStatusId));
				insertCommand.Parameters.AddWithValue("@EnrollmentStatusCode", ToDbNullable(statusCode));
				insertCommand.Parameters.AddWithValue("@EnrollmentStatusName", ToDbNullable(statusName));

				await insertCommand.ExecuteNonQueryAsync(cancellationToken);

				transaction.Commit();
			}
			catch
			{
				transaction.Rollback();
				throw;
			}
		}

		public async Task SetEnrollmentIsActiveAsync(
			int id,
			bool isActive,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_Enrollment
                SET
                    IsActive = @IsActive,
                    IsCurrent = CASE WHEN @IsActive = 0 THEN 0 ELSE IsCurrent END,
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

		public async Task SetEnrollmentSubjectIsActiveAsync(
			int id,
			bool isActive,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_EnrollmentSubject
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

		private static AcademicsEnrollment MapEnrollment(SqlDataReader reader)
		{
			return new AcademicsEnrollment
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				LearnerId = reader.GetInt32(reader.GetOrdinal("LearnerId")),
				LearnerLastName = reader["LearnerLastName"] as string ?? string.Empty,
				LearnerFirstName = reader["LearnerFirstName"] as string ?? string.Empty,
				LearnerMiddleName = reader["LearnerMiddleName"] as string ?? string.Empty,
				LearnerExtensionName = reader["LearnerExtensionName"] as string ?? string.Empty,
				LearnerLrn = reader["LearnerLrn"] as string ?? string.Empty,
				SchoolYearId = reader.GetInt32(reader.GetOrdinal("SchoolYearId")),
				SchoolYearName = reader["SchoolYearName"] as string ?? string.Empty,
				SchoolYearProgramId = reader.GetInt32(reader.GetOrdinal("SchoolYearProgramId")),
				ProgramCode = reader["ProgramCode"] as string ?? string.Empty,
				ProgramName = reader["ProgramName"] as string ?? string.Empty,
				SchoolYearSectionId = reader.GetInt32(reader.GetOrdinal("SchoolYearSectionId")),
				GradeLevelName = reader["GradeLevelName"] as string ?? string.Empty,
				SectionName = reader["SectionName"] as string ?? string.Empty,
				EnrollmentStatusId = reader.GetInt32(reader.GetOrdinal("EnrollmentStatusId")),
				EnrollmentStatusCode = reader["EnrollmentStatusCode"] as string ?? string.Empty,
				EnrollmentStatusName = reader["EnrollmentStatusName"] as string ?? string.Empty,
				EnrollmentDate = reader["EnrollmentDate"] == DBNull.Value
					? null
					: reader.GetDateTime(reader.GetOrdinal("EnrollmentDate")),
				EffectiveStartDate = reader["EffectiveStartDate"] == DBNull.Value
					? null
					: reader.GetDateTime(reader.GetOrdinal("EffectiveStartDate")),
				EffectiveEndDate = reader["EffectiveEndDate"] == DBNull.Value
					? null
					: reader.GetDateTime(reader.GetOrdinal("EffectiveEndDate")),
				IsCurrent = reader.GetBoolean(reader.GetOrdinal("IsCurrent")),
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
				Remarks = reader["Remarks"] as string ?? string.Empty
			};
		}

		private static AcademicsEnrollmentSubject MapEnrollmentSubject(SqlDataReader reader)
		{
			return new AcademicsEnrollmentSubject
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				EnrollmentId = reader.GetInt32(reader.GetOrdinal("EnrollmentId")),
				SubjectId = reader.GetInt32(reader.GetOrdinal("SubjectId")),
				EnrollmentStatusId = reader["EnrollmentStatusId"] == DBNull.Value
					? null
					: reader.GetInt32(reader.GetOrdinal("EnrollmentStatusId")),
				EnrollmentStatusCode = reader["EnrollmentStatusCode"] as string ?? string.Empty,
				EnrollmentStatusName = reader["EnrollmentStatusName"] as string ?? string.Empty,
				SubjectCode = reader["SubjectCode"] as string ?? string.Empty,
				SubjectName = reader["SubjectName"] as string ?? string.Empty,
				LearningAreaCode = reader["LearningAreaCode"] as string ?? string.Empty,
				LearningAreaShortName = reader["LearningAreaShortName"] as string ?? string.Empty,
				LearningAreaDescription = reader["LearningAreaDescription"] as string ?? string.Empty,
				TermName = reader["TermName"] as string ?? string.Empty,
				TeacherLastName = reader["TeacherLastName"] as string ?? string.Empty,
				TeacherFirstName = reader["TeacherFirstName"] as string ?? string.Empty,
				TeacherMiddleName = reader["TeacherMiddleName"] as string ?? string.Empty,
				TeacherExtensionName = reader["TeacherExtensionName"] as string ?? string.Empty,
				EffectiveStartDate = reader["EffectiveStartDate"] == DBNull.Value
					? null
					: reader.GetDateTime(reader.GetOrdinal("EffectiveStartDate")),
				EffectiveEndDate = reader["EffectiveEndDate"] == DBNull.Value
					? null
					: reader.GetDateTime(reader.GetOrdinal("EffectiveEndDate")),
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
			};
		}

		private static void PrepareForSave(AcademicsEnrollment enrollment)
		{
			enrollment.GradeLevelName = NormalizeRequired(enrollment.GradeLevelName);
			enrollment.EnrollmentStatusCode = NormalizeRequired(enrollment.EnrollmentStatusCode);
			enrollment.EnrollmentStatusName = NormalizeRequired(enrollment.EnrollmentStatusName);
			enrollment.Remarks = Normalize(enrollment.Remarks) ?? string.Empty;

			if (!enrollment.IsActive)
			{
				enrollment.IsCurrent = false;
			}
		}

		private static void AddEnrollmentParameters(
			SqlCommand command,
			AcademicsEnrollment enrollment)
		{
			command.Parameters.AddWithValue("@LearnerId", enrollment.LearnerId);
			command.Parameters.AddWithValue("@SchoolYearId", enrollment.SchoolYearId);
			command.Parameters.AddWithValue("@SchoolYearProgramId", enrollment.SchoolYearProgramId);
			command.Parameters.AddWithValue("@SchoolYearSectionId", enrollment.SchoolYearSectionId);
			command.Parameters.AddWithValue("@GradeLevelName", enrollment.GradeLevelName);
			command.Parameters.AddWithValue("@EnrollmentStatusId", enrollment.EnrollmentStatusId);
			command.Parameters.AddWithValue("@EnrollmentStatusCode", enrollment.EnrollmentStatusCode);
			command.Parameters.AddWithValue("@EnrollmentStatusName", enrollment.EnrollmentStatusName);
			command.Parameters.AddWithValue("@EnrollmentDate", ToDbNullable(enrollment.EnrollmentDate));
			command.Parameters.AddWithValue("@EffectiveStartDate", ToDbNullable(enrollment.EffectiveStartDate));
			command.Parameters.AddWithValue("@EffectiveEndDate", ToDbNullable(enrollment.EffectiveEndDate));
			command.Parameters.AddWithValue("@IsCurrent", enrollment.IsCurrent);
			command.Parameters.AddWithValue("@IsActive", enrollment.IsActive);
			command.Parameters.AddWithValue("@Remarks", ToDbNullable(enrollment.Remarks));
		}

		private static void AddEnrollmentSubjectLinkParameters(
			SqlCommand command,
			int enrollmentId,
			int subjectId,
			int? enrollmentStatusId,
			string? enrollmentStatusCode,
			string? enrollmentStatusName,
			DateTime? effectiveStartDate,
			DateTime? effectiveEndDate)
		{
			command.Parameters.AddWithValue("@EnrollmentId", enrollmentId);
			command.Parameters.AddWithValue("@SubjectId", subjectId);
			command.Parameters.AddWithValue("@EnrollmentStatusId", ToDbNullable(enrollmentStatusId));
			command.Parameters.AddWithValue("@EnrollmentStatusCode", ToDbNullable(enrollmentStatusCode));
			command.Parameters.AddWithValue("@EnrollmentStatusName", ToDbNullable(enrollmentStatusName));
			command.Parameters.AddWithValue("@EffectiveStartDate", ToDbNullable(effectiveStartDate));
			command.Parameters.AddWithValue("@EffectiveEndDate", ToDbNullable(effectiveEndDate));
		}

		private static object ToDbNullable(int? value)
		{
			return value.HasValue ? value.Value : DBNull.Value;
		}

		private static object ToDbNullable(DateTime? value)
		{
			return value.HasValue ? value.Value.Date : DBNull.Value;
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
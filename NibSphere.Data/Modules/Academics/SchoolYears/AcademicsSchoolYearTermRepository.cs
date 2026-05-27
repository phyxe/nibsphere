using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Academics.SchoolYears;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Academics.SchoolYears
{
	public sealed class AcademicsSchoolYearTermRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public AcademicsSchoolYearTermRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<AcademicsSchoolYearTerm>> GetBySchoolYearIdAsync(
			int schoolYearId,
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    term.Id,
                    term.SchoolYearId,
                    term.ParentTermId,
                    parent.Name AS ParentTermName,
                    term.Name,
                    term.ShortName,
                    term.StartDate,
                    term.EndDate,
                    term.SortOrder,
                    term.IsEnrollmentTerm,
                    term.IsGradingTerm,
                    term.IsReportingTerm,
                    term.IsActive,

                    (
                        SELECT COUNT(1)
                        FROM Academics_SchoolYearTerm child
                        WHERE child.ParentTermId = term.Id
                    ) AS ChildTermCount,

                    (
                        SELECT COUNT(1)
                        FROM Academics_Subject subject
                        WHERE subject.TermId = term.Id
                    ) AS DependentRecordCount
                FROM Academics_SchoolYearTerm term
                LEFT JOIN Academics_SchoolYearTerm parent
                    ON term.ParentTermId = parent.Id
                WHERE term.SchoolYearId = @SchoolYearId
                  AND (@IncludeInactive = 1 OR term.IsActive = 1)
                ORDER BY
                    term.IsActive DESC,
                    COALESCE(parent.SortOrder, term.SortOrder),
                    parent.Name,
                    term.ParentTermId,
                    term.SortOrder,
                    term.Name,
                    term.Id;
                """;

			List<AcademicsSchoolYearTerm> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@SchoolYearId", schoolYearId);
			command.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				items.Add(Map(reader));
			}

			return items;
		}

		public async Task<List<AcademicsSchoolYearTerm>> GetParentOptionsBySchoolYearIdAsync(
			int schoolYearId,
			int? excludeTermId = null,
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    term.Id,
                    term.SchoolYearId,
                    term.ParentTermId,
                    parent.Name AS ParentTermName,
                    term.Name,
                    term.ShortName,
                    term.StartDate,
                    term.EndDate,
                    term.SortOrder,
                    term.IsEnrollmentTerm,
                    term.IsGradingTerm,
                    term.IsReportingTerm,
                    term.IsActive,

                    (
                        SELECT COUNT(1)
                        FROM Academics_SchoolYearTerm child
                        WHERE child.ParentTermId = term.Id
                    ) AS ChildTermCount,

                    (
                        SELECT COUNT(1)
                        FROM Academics_Subject subject
                        WHERE subject.TermId = term.Id
                    ) AS DependentRecordCount
                FROM Academics_SchoolYearTerm term
                LEFT JOIN Academics_SchoolYearTerm parent
                    ON term.ParentTermId = parent.Id
                WHERE term.SchoolYearId = @SchoolYearId
                  AND term.ParentTermId IS NULL
                  AND (@ExcludeTermId IS NULL OR term.Id <> @ExcludeTermId)
                  AND (@IncludeInactive = 1 OR term.IsActive = 1)
                ORDER BY
                    term.IsActive DESC,
                    term.SortOrder,
                    term.Name,
                    term.Id;
                """;

			List<AcademicsSchoolYearTerm> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@SchoolYearId", schoolYearId);
			command.Parameters.AddWithValue("@ExcludeTermId", ToDbNullable(excludeTermId));
			command.Parameters.AddWithValue("@IncludeInactive", includeInactive ? 1 : 0);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				items.Add(Map(reader));
			}

			return items;
		}

		public async Task<AcademicsSchoolYearTerm?> GetByIdAsync(
			int id,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    term.Id,
                    term.SchoolYearId,
                    term.ParentTermId,
                    parent.Name AS ParentTermName,
                    term.Name,
                    term.ShortName,
                    term.StartDate,
                    term.EndDate,
                    term.SortOrder,
                    term.IsEnrollmentTerm,
                    term.IsGradingTerm,
                    term.IsReportingTerm,
                    term.IsActive,

                    (
                        SELECT COUNT(1)
                        FROM Academics_SchoolYearTerm child
                        WHERE child.ParentTermId = term.Id
                    ) AS ChildTermCount,

                    (
                        SELECT COUNT(1)
                        FROM Academics_Subject subject
                        WHERE subject.TermId = term.Id
                    ) AS DependentRecordCount
                FROM Academics_SchoolYearTerm term
                LEFT JOIN Academics_SchoolYearTerm parent
                    ON term.ParentTermId = parent.Id
                WHERE term.Id = @Id;
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
			AcademicsSchoolYearTerm term,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                INSERT INTO Academics_SchoolYearTerm
                (
                    SchoolYearId,
                    ParentTermId,
                    Name,
                    ShortName,
                    StartDate,
                    EndDate,
                    SortOrder,
                    IsEnrollmentTerm,
                    IsGradingTerm,
                    IsReportingTerm,
                    IsActive
                )
                VALUES
                (
                    @SchoolYearId,
                    @ParentTermId,
                    @Name,
                    @ShortName,
                    @StartDate,
                    @EndDate,
                    @SortOrder,
                    @IsEnrollmentTerm,
                    @IsGradingTerm,
                    @IsReportingTerm,
                    @IsActive
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			PrepareForSave(term);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			await ValidateForSaveAsync(term, connection, cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddParameters(command, term);

			object? result = await command.ExecuteScalarAsync(cancellationToken);
			return result is int id ? id : 0;
		}

		public async Task UpdateAsync(
			AcademicsSchoolYearTerm term,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_SchoolYearTerm
                SET
                    SchoolYearId = @SchoolYearId,
                    ParentTermId = @ParentTermId,
                    Name = @Name,
                    ShortName = @ShortName,
                    StartDate = @StartDate,
                    EndDate = @EndDate,
                    SortOrder = @SortOrder,
                    IsEnrollmentTerm = @IsEnrollmentTerm,
                    IsGradingTerm = @IsGradingTerm,
                    IsReportingTerm = @IsReportingTerm,
                    IsActive = @IsActive,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			PrepareForSave(term);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			await ValidateForSaveAsync(term, connection, cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddParameters(command, term);
			command.Parameters.AddWithValue("@Id", term.Id);

			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		public async Task SetIsActiveAsync(
			int id,
			bool isActive,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_SchoolYearTerm
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

		private static AcademicsSchoolYearTerm Map(SqlDataReader reader)
		{
			int childTermCount = GetOptionalInt32(reader, "ChildTermCount");
			int dependentRecordCount = GetOptionalInt32(reader, "DependentRecordCount");

			return new AcademicsSchoolYearTerm
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				SchoolYearId = reader.GetInt32(reader.GetOrdinal("SchoolYearId")),
				ParentTermId = reader["ParentTermId"] == DBNull.Value
					? null
					: reader.GetInt32(reader.GetOrdinal("ParentTermId")),
				ParentTermName = reader["ParentTermName"] as string ?? string.Empty,
				Name = reader["Name"] as string ?? string.Empty,
				ShortName = reader["ShortName"] as string ?? string.Empty,
				StartDate = reader["StartDate"] == DBNull.Value
					? null
					: reader.GetDateTime(reader.GetOrdinal("StartDate")),
				EndDate = reader["EndDate"] == DBNull.Value
					? null
					: reader.GetDateTime(reader.GetOrdinal("EndDate")),
				SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
				IsEnrollmentTerm = reader.GetBoolean(reader.GetOrdinal("IsEnrollmentTerm")),
				IsGradingTerm = reader.GetBoolean(reader.GetOrdinal("IsGradingTerm")),
				IsReportingTerm = reader.GetBoolean(reader.GetOrdinal("IsReportingTerm")),
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
				ChildTermCount = childTermCount,
				DependentRecordCount = dependentRecordCount,
				IsEditable = dependentRecordCount == 0,
				IsDeletable = childTermCount == 0 && dependentRecordCount == 0
			};
		}

		private static void PrepareForSave(AcademicsSchoolYearTerm term)
		{
			term.Name = NormalizeRequired(term.Name);
			term.ShortName = Normalize(term.ShortName) ?? string.Empty;
			term.SortOrder = Math.Max(0, term.SortOrder);

			if (term.ParentTermId.HasValue && term.ParentTermId.Value <= 0)
			{
				term.ParentTermId = null;
			}

			if (term.Id > 0 && term.ParentTermId == term.Id)
			{
				throw new InvalidOperationException("A term cannot be its own parent.");
			}

			if (string.IsNullOrWhiteSpace(term.Name))
			{
				throw new InvalidOperationException("Term name is required.");
			}

			if (!term.StartDate.HasValue)
			{
				throw new InvalidOperationException("Term start date is required.");
			}

			if (!term.EndDate.HasValue)
			{
				throw new InvalidOperationException("Term end date is required.");
			}

			if (term.EndDate.Value.Date < term.StartDate.Value.Date)
			{
				throw new InvalidOperationException("Term end date cannot be earlier than the start date.");
			}
		}

		private static async Task ValidateForSaveAsync(
			AcademicsSchoolYearTerm term,
			SqlConnection connection,
			CancellationToken cancellationToken)
		{
			await ValidateAgainstSchoolYearAsync(term, connection, cancellationToken);

			if (term.ParentTermId.HasValue)
			{
				await ValidateAgainstParentTermAsync(term, connection, cancellationToken);
			}
		}

		private static async Task ValidateAgainstSchoolYearAsync(
			AcademicsSchoolYearTerm term,
			SqlConnection connection,
			CancellationToken cancellationToken)
		{
			const string sql =
				"""
                SELECT StartDate, EndDate
                FROM Academics_SchoolYear
                WHERE Id = @SchoolYearId;
                """;

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@SchoolYearId", term.SchoolYearId);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			if (!await reader.ReadAsync(cancellationToken))
			{
				throw new InvalidOperationException("The selected school year was not found.");
			}

			DateTime? schoolYearStartDate = reader["StartDate"] == DBNull.Value
				? null
				: reader.GetDateTime(reader.GetOrdinal("StartDate"));

			DateTime? schoolYearEndDate = reader["EndDate"] == DBNull.Value
				? null
				: reader.GetDateTime(reader.GetOrdinal("EndDate"));

			if (!schoolYearStartDate.HasValue || !schoolYearEndDate.HasValue)
			{
				throw new InvalidOperationException("The selected school year must have a start date and end date before terms can be added.");
			}

			if (term.StartDate!.Value.Date < schoolYearStartDate.Value.Date)
			{
				throw new InvalidOperationException("Term start date cannot be earlier than the school year start date.");
			}

			if (term.EndDate!.Value.Date > schoolYearEndDate.Value.Date)
			{
				throw new InvalidOperationException("Term end date cannot be later than the school year end date.");
			}
		}

		private static async Task ValidateAgainstParentTermAsync(
			AcademicsSchoolYearTerm term,
			SqlConnection connection,
			CancellationToken cancellationToken)
		{
			const string sql =
				"""
                SELECT
                    SchoolYearId,
                    ParentTermId,
                    StartDate,
                    EndDate
                FROM Academics_SchoolYearTerm
                WHERE Id = @ParentTermId;
                """;

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@ParentTermId", term.ParentTermId!.Value);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			if (!await reader.ReadAsync(cancellationToken))
			{
				throw new InvalidOperationException("The selected parent term was not found.");
			}

			int parentSchoolYearId = reader.GetInt32(reader.GetOrdinal("SchoolYearId"));

			int? parentParentTermId = reader["ParentTermId"] == DBNull.Value
				? null
				: reader.GetInt32(reader.GetOrdinal("ParentTermId"));

			DateTime? parentStartDate = reader["StartDate"] == DBNull.Value
				? null
				: reader.GetDateTime(reader.GetOrdinal("StartDate"));

			DateTime? parentEndDate = reader["EndDate"] == DBNull.Value
				? null
				: reader.GetDateTime(reader.GetOrdinal("EndDate"));

			if (parentSchoolYearId != term.SchoolYearId)
			{
				throw new InvalidOperationException("Parent term must belong to the same school year.");
			}

			if (parentParentTermId.HasValue)
			{
				throw new InvalidOperationException("Only top-level terms can be used as parent terms.");
			}

			if (!parentStartDate.HasValue || !parentEndDate.HasValue)
			{
				throw new InvalidOperationException("The selected parent term must have a start date and end date.");
			}

			if (term.StartDate!.Value.Date < parentStartDate.Value.Date)
			{
				throw new InvalidOperationException("Child term start date cannot be earlier than the parent term start date.");
			}

			if (term.EndDate!.Value.Date > parentEndDate.Value.Date)
			{
				throw new InvalidOperationException("Child term end date cannot be later than the parent term end date.");
			}
		}

		private static void AddParameters(SqlCommand command, AcademicsSchoolYearTerm term)
		{
			command.Parameters.AddWithValue("@SchoolYearId", term.SchoolYearId);
			command.Parameters.AddWithValue("@ParentTermId", ToDbNullable(term.ParentTermId));
			command.Parameters.AddWithValue("@Name", term.Name);
			command.Parameters.AddWithValue("@ShortName", ToDbNullable(term.ShortName));
			command.Parameters.AddWithValue("@StartDate", ToDbNullable(term.StartDate));
			command.Parameters.AddWithValue("@EndDate", ToDbNullable(term.EndDate));
			command.Parameters.AddWithValue("@SortOrder", term.SortOrder);
			command.Parameters.AddWithValue("@IsEnrollmentTerm", term.IsEnrollmentTerm);
			command.Parameters.AddWithValue("@IsGradingTerm", term.IsGradingTerm);
			command.Parameters.AddWithValue("@IsReportingTerm", term.IsReportingTerm);
			command.Parameters.AddWithValue("@IsActive", term.IsActive);
		}

		private static int GetOptionalInt32(SqlDataReader reader, string columnName)
		{
			for (int i = 0; i < reader.FieldCount; i++)
			{
				if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
				{
					return reader[columnName] == DBNull.Value ? 0 : Convert.ToInt32(reader[columnName]);
				}
			}

			return 0;
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
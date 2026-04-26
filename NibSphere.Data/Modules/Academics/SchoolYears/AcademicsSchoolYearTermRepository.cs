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
                    term.IsActive
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
                    term.IsActive
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

			if (term.ParentTermId == term.Id)
			{
				term.ParentTermId = null;
			}

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

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
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
			};
		}

		private static void PrepareForSave(AcademicsSchoolYearTerm term)
		{
			term.Name = NormalizeRequired(term.Name);
			term.ShortName = Normalize(term.ShortName) ?? string.Empty;
			term.SortOrder = Math.Max(0, term.SortOrder);
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
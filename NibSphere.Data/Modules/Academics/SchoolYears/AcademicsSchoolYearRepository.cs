using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Academics.SchoolYears;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Academics.SchoolYears
{
	public sealed class AcademicsSchoolYearRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public AcademicsSchoolYearRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<AcademicsSchoolYear>> GetAllAsync(
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    sy.Id,
                    sy.Name,
                    sy.StartDate,
                    sy.EndDate,
                    sy.IsCurrent,
                    sy.IsActive,

                    (
                        SELECT COUNT(1)
                        FROM Academics_SchoolYearTerm term
                        WHERE term.SchoolYearId = sy.Id
                    ) AS TermCount,

                    (
                        (
                            SELECT COUNT(1)
                            FROM Academics_SchoolYearProgram program
                            WHERE program.SchoolYearId = sy.Id
                        )
                        +
                        (
                            SELECT COUNT(1)
                            FROM Academics_SchoolYearSection section
                            WHERE section.SchoolYearId = sy.Id
                        )
                        +
                        (
                            SELECT COUNT(1)
                            FROM Academics_Subject subject
                            WHERE subject.SchoolYearId = sy.Id
                        )
                        +
                        (
                            SELECT COUNT(1)
                            FROM Academics_Enrollment enrollment
                            WHERE enrollment.SchoolYearId = sy.Id
                        )
                    ) AS DependentRecordCount
                FROM Academics_SchoolYear sy
                WHERE @IncludeInactive = 1
                   OR sy.IsActive = 1
                ORDER BY
                    sy.IsCurrent DESC,
                    sy.IsActive DESC,
                    sy.StartDate DESC,
                    sy.Name DESC,
                    sy.Id DESC;
                """;

			List<AcademicsSchoolYear> items = new();

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

		public async Task<AcademicsSchoolYear?> GetByIdAsync(
			int id,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    sy.Id,
                    sy.Name,
                    sy.StartDate,
                    sy.EndDate,
                    sy.IsCurrent,
                    sy.IsActive,

                    (
                        SELECT COUNT(1)
                        FROM Academics_SchoolYearTerm term
                        WHERE term.SchoolYearId = sy.Id
                    ) AS TermCount,

                    (
                        (
                            SELECT COUNT(1)
                            FROM Academics_SchoolYearProgram program
                            WHERE program.SchoolYearId = sy.Id
                        )
                        +
                        (
                            SELECT COUNT(1)
                            FROM Academics_SchoolYearSection section
                            WHERE section.SchoolYearId = sy.Id
                        )
                        +
                        (
                            SELECT COUNT(1)
                            FROM Academics_Subject subject
                            WHERE subject.SchoolYearId = sy.Id
                        )
                        +
                        (
                            SELECT COUNT(1)
                            FROM Academics_Enrollment enrollment
                            WHERE enrollment.SchoolYearId = sy.Id
                        )
                    ) AS DependentRecordCount
                FROM Academics_SchoolYear sy
                WHERE sy.Id = @Id;
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
			AcademicsSchoolYear schoolYear,
			CancellationToken cancellationToken = default)
		{
			const string clearCurrentSql =
				"""
                UPDATE Academics_SchoolYear
                SET
                    IsCurrent = 0,
                    UpdatedAt = GETDATE()
                WHERE IsCurrent = 1;
                """;

			const string insertSql =
				"""
                INSERT INTO Academics_SchoolYear
                (
                    Name,
                    StartDate,
                    EndDate,
                    IsCurrent,
                    IsActive
                )
                VALUES
                (
                    @Name,
                    @StartDate,
                    @EndDate,
                    @IsCurrent,
                    @IsActive
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			PrepareForSave(schoolYear);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlTransaction transaction = connection.BeginTransaction();

			try
			{
				if (schoolYear.IsCurrent)
				{
					using SqlCommand clearCommand = new(clearCurrentSql, connection, transaction);
					await clearCommand.ExecuteNonQueryAsync(cancellationToken);
				}

				using SqlCommand insertCommand = new(insertSql, connection, transaction);
				AddParameters(insertCommand, schoolYear);

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
			AcademicsSchoolYear schoolYear,
			CancellationToken cancellationToken = default)
		{
			const string clearCurrentSql =
				"""
                UPDATE Academics_SchoolYear
                SET
                    IsCurrent = 0,
                    UpdatedAt = GETDATE()
                WHERE IsCurrent = 1
                  AND Id <> @Id;
                """;

			const string updateSql =
				"""
                UPDATE Academics_SchoolYear
                SET
                    Name = @Name,
                    StartDate = @StartDate,
                    EndDate = @EndDate,
                    IsCurrent = @IsCurrent,
                    IsActive = @IsActive,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			PrepareForSave(schoolYear);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlTransaction transaction = connection.BeginTransaction();

			try
			{
				if (schoolYear.IsCurrent)
				{
					using SqlCommand clearCommand = new(clearCurrentSql, connection, transaction);
					clearCommand.Parameters.AddWithValue("@Id", schoolYear.Id);
					await clearCommand.ExecuteNonQueryAsync(cancellationToken);
				}

				using SqlCommand updateCommand = new(updateSql, connection, transaction);
				AddParameters(updateCommand, schoolYear);
				updateCommand.Parameters.AddWithValue("@Id", schoolYear.Id);

				await updateCommand.ExecuteNonQueryAsync(cancellationToken);

				transaction.Commit();
			}
			catch
			{
				transaction.Rollback();
				throw;
			}
		}

		public async Task SetCurrentAsync(
			int id,
			CancellationToken cancellationToken = default)
		{
			const string clearCurrentSql =
				"""
                UPDATE Academics_SchoolYear
                SET
                    IsCurrent = 0,
                    UpdatedAt = GETDATE()
                WHERE IsCurrent = 1;
                """;

			const string setCurrentSql =
				"""
                UPDATE Academics_SchoolYear
                SET
                    IsCurrent = 1,
                    IsActive = 1,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlTransaction transaction = connection.BeginTransaction();

			try
			{
				using SqlCommand clearCommand = new(clearCurrentSql, connection, transaction);
				await clearCommand.ExecuteNonQueryAsync(cancellationToken);

				using SqlCommand setCommand = new(setCurrentSql, connection, transaction);
				setCommand.Parameters.AddWithValue("@Id", id);
				await setCommand.ExecuteNonQueryAsync(cancellationToken);

				transaction.Commit();
			}
			catch
			{
				transaction.Rollback();
				throw;
			}
		}

		public async Task SetIsActiveAsync(
			int id,
			bool isActive,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_SchoolYear
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

		private static AcademicsSchoolYear Map(SqlDataReader reader)
		{
			int termCount = GetOptionalInt32(reader, "TermCount");
			int dependentRecordCount = GetOptionalInt32(reader, "DependentRecordCount");

			return new AcademicsSchoolYear
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				Name = reader["Name"] as string ?? string.Empty,
				StartDate = reader["StartDate"] == DBNull.Value
					? null
					: reader.GetDateTime(reader.GetOrdinal("StartDate")),
				EndDate = reader["EndDate"] == DBNull.Value
					? null
					: reader.GetDateTime(reader.GetOrdinal("EndDate")),
				IsCurrent = reader.GetBoolean(reader.GetOrdinal("IsCurrent")),
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
				TermCount = termCount,
				DependentRecordCount = dependentRecordCount,
				IsEditable = dependentRecordCount == 0,
				IsDeletable = termCount == 0 && dependentRecordCount == 0
			};
		}

		private static void PrepareForSave(AcademicsSchoolYear schoolYear)
		{
			schoolYear.Name = NormalizeRequired(schoolYear.Name);

			if (string.IsNullOrWhiteSpace(schoolYear.Name))
			{
				throw new InvalidOperationException("School year name is required.");
			}

			if (!schoolYear.StartDate.HasValue)
			{
				throw new InvalidOperationException("School year start date is required.");
			}

			if (!schoolYear.EndDate.HasValue)
			{
				throw new InvalidOperationException("School year end date is required.");
			}

			if (schoolYear.EndDate.Value.Date < schoolYear.StartDate.Value.Date)
			{
				throw new InvalidOperationException("School year end date cannot be earlier than the start date.");
			}

			if (!schoolYear.IsActive)
			{
				schoolYear.IsCurrent = false;
			}
		}

		private static void AddParameters(SqlCommand command, AcademicsSchoolYear schoolYear)
		{
			command.Parameters.AddWithValue("@Name", schoolYear.Name);
			command.Parameters.AddWithValue("@StartDate", ToDbNullable(schoolYear.StartDate));
			command.Parameters.AddWithValue("@EndDate", ToDbNullable(schoolYear.EndDate));
			command.Parameters.AddWithValue("@IsCurrent", schoolYear.IsCurrent);
			command.Parameters.AddWithValue("@IsActive", schoolYear.IsActive);
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

		private static object ToDbNullable(DateTime? value)
		{
			return value.HasValue ? value.Value.Date : DBNull.Value;
		}

		private static string NormalizeRequired(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
		}
	}
}
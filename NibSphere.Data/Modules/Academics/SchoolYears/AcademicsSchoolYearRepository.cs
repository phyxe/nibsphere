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
                    Id,
                    Name,
                    StartDate,
                    EndDate,
                    IsCurrent,
                    IsActive
                FROM Academics_SchoolYear
                WHERE @IncludeInactive = 1
                   OR IsActive = 1
                ORDER BY
                    IsCurrent DESC,
                    IsActive DESC,
                    StartDate DESC,
                    Name DESC,
                    Id DESC;
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
                    Id,
                    Name,
                    StartDate,
                    EndDate,
                    IsCurrent,
                    IsActive
                FROM Academics_SchoolYear
                WHERE Id = @Id;
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
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
			};
		}

		private static void PrepareForSave(AcademicsSchoolYear schoolYear)
		{
			schoolYear.Name = NormalizeRequired(schoolYear.Name);

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
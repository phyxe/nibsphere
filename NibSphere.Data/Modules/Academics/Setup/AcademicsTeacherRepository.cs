using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Academics.Setup;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Academics.Setup
{
	public sealed class AcademicsTeacherRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public AcademicsTeacherRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<AcademicsTeacher>> GetAllAsync(
			bool includeInactive = false,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                SELECT
                    Id,
                    LastName,
                    FirstName,
                    MiddleName,
                    ExtensionName,
                    Position,
                    Designation,
                    IsActive
                FROM Academics_Teacher
                WHERE @IncludeInactive = 1
                   OR IsActive = 1
                ORDER BY
                    IsActive DESC,
                    LastName,
                    FirstName,
                    MiddleName,
                    Id;
                """;

			List<AcademicsTeacher> items = new();

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

		public async Task<int> InsertAsync(
			AcademicsTeacher teacher,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                INSERT INTO Academics_Teacher
                (
                    LastName,
                    FirstName,
                    MiddleName,
                    ExtensionName,
                    Position,
                    Designation,
                    IsActive
                )
                VALUES
                (
                    @LastName,
                    @FirstName,
                    @MiddleName,
                    @ExtensionName,
                    @Position,
                    @Designation,
                    @IsActive
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			PrepareForSave(teacher);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddParameters(command, teacher);

			object? result = await command.ExecuteScalarAsync(cancellationToken);
			return result is int id ? id : 0;
		}

		public async Task UpdateAsync(
			AcademicsTeacher teacher,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_Teacher
                SET
                    LastName = @LastName,
                    FirstName = @FirstName,
                    MiddleName = @MiddleName,
                    ExtensionName = @ExtensionName,
                    Position = @Position,
                    Designation = @Designation,
                    IsActive = @IsActive,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			PrepareForSave(teacher);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlCommand command = new(sql, connection);
			AddParameters(command, teacher);
			command.Parameters.AddWithValue("@Id", teacher.Id);

			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		public async Task SetIsActiveAsync(
			int id,
			bool isActive,
			CancellationToken cancellationToken = default)
		{
			const string sql =
				"""
                UPDATE Academics_Teacher
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

		private static AcademicsTeacher Map(SqlDataReader reader)
		{
			return new AcademicsTeacher
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				LastName = reader["LastName"] as string ?? string.Empty,
				FirstName = reader["FirstName"] as string ?? string.Empty,
				MiddleName = reader["MiddleName"] as string ?? string.Empty,
				ExtensionName = reader["ExtensionName"] as string ?? string.Empty,
				Position = reader["Position"] as string ?? string.Empty,
				Designation = reader["Designation"] as string ?? string.Empty,
				IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
			};
		}

		private static void PrepareForSave(AcademicsTeacher teacher)
		{
			teacher.LastName = NormalizeRequired(teacher.LastName);
			teacher.FirstName = NormalizeRequired(teacher.FirstName);
			teacher.MiddleName = Normalize(teacher.MiddleName) ?? string.Empty;
			teacher.ExtensionName = Normalize(teacher.ExtensionName) ?? string.Empty;
			teacher.Position = Normalize(teacher.Position) ?? string.Empty;
			teacher.Designation = Normalize(teacher.Designation) ?? string.Empty;
		}

		private static void AddParameters(SqlCommand command, AcademicsTeacher teacher)
		{
			command.Parameters.AddWithValue("@LastName", teacher.LastName);
			command.Parameters.AddWithValue("@FirstName", teacher.FirstName);
			command.Parameters.AddWithValue("@MiddleName", ToDbNullable(teacher.MiddleName));
			command.Parameters.AddWithValue("@ExtensionName", ToDbNullable(teacher.ExtensionName));
			command.Parameters.AddWithValue("@Position", ToDbNullable(teacher.Position));
			command.Parameters.AddWithValue("@Designation", ToDbNullable(teacher.Designation));
			command.Parameters.AddWithValue("@IsActive", teacher.IsActive);
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
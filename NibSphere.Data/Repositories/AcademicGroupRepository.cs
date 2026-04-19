using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Models;
using NibSphere.Data.Database;

namespace NibSphere.Data.Repositories
{
	public class AcademicGroupRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public AcademicGroupRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<AcademicGroup>> GetAllAsync()
		{
			const string sql =
				"""
                SELECT
                    Id,
                    Name,
                    Sort
                FROM AcademicGroup
                ORDER BY Sort, Name, Id;
                """;

			List<AcademicGroup> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			using SqlDataReader reader = await command.ExecuteReaderAsync();

			while (await reader.ReadAsync())
			{
				items.Add(new AcademicGroup
				{
					Id = reader.GetInt32(reader.GetOrdinal("Id")),
					Name = reader.GetString(reader.GetOrdinal("Name")),
					Sort = reader.GetInt32(reader.GetOrdinal("Sort"))
				});
			}

			return items;
		}

		public async Task<int> InsertAsync(AcademicGroup academicGroup)
		{
			const string sql =
				"""
                INSERT INTO AcademicGroup
                (
                    Name,
                    Sort
                )
                VALUES
                (
                    @Name,
                    @Sort
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			PrepareForSave(academicGroup);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			AddParameters(command, academicGroup);

			object? result = await command.ExecuteScalarAsync();
			return result is int id ? id : 0;
		}

		public async Task UpdateAsync(AcademicGroup academicGroup)
		{
			const string sql =
				"""
                UPDATE AcademicGroup
                SET
                    Name = @Name,
                    Sort = @Sort,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			PrepareForSave(academicGroup);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			AddParameters(command, academicGroup);
			command.Parameters.AddWithValue("@Id", academicGroup.Id);

			await command.ExecuteNonQueryAsync();
		}

		public async Task DeleteAsync(int id)
		{
			const string sql =
				"""
                DELETE FROM AcademicGroup
                WHERE Id = @Id;
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			command.Parameters.AddWithValue("@Id", id);

			await command.ExecuteNonQueryAsync();
		}

		private static void PrepareForSave(AcademicGroup academicGroup)
		{
			academicGroup.Name = NormalizeRequired(academicGroup.Name);
		}

		private static void AddParameters(SqlCommand command, AcademicGroup academicGroup)
		{
			command.Parameters.AddWithValue("@Name", academicGroup.Name);
			command.Parameters.AddWithValue("@Sort", academicGroup.Sort);
		}

		private static string NormalizeRequired(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
		}
	}
}
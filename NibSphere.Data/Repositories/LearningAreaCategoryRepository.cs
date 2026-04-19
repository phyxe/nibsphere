using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Models;
using NibSphere.Data.Database;

namespace NibSphere.Data.Repositories
{
	public class LearningAreaCategoryRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public LearningAreaCategoryRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<LearningAreaCategory>> GetAllAsync()
		{
			const string sql =
				"""
                SELECT
                    Id,
                    Name,
                    Sort
                FROM LearningAreaCategory
                ORDER BY Sort, Name, Id;
                """;

			List<LearningAreaCategory> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			using SqlDataReader reader = await command.ExecuteReaderAsync();

			while (await reader.ReadAsync())
			{
				items.Add(new LearningAreaCategory
				{
					Id = reader.GetInt32(reader.GetOrdinal("Id")),
					Name = reader.GetString(reader.GetOrdinal("Name")),
					Sort = reader.GetInt32(reader.GetOrdinal("Sort"))
				});
			}

			return items;
		}

		public async Task<int> InsertAsync(LearningAreaCategory category)
		{
			const string sql =
				"""
                INSERT INTO LearningAreaCategory
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

			PrepareForSave(category);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			AddParameters(command, category);

			object? result = await command.ExecuteScalarAsync();
			return result is int id ? id : 0;
		}

		public async Task UpdateAsync(LearningAreaCategory category)
		{
			const string sql =
				"""
                UPDATE LearningAreaCategory
                SET
                    Name = @Name,
                    Sort = @Sort,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			PrepareForSave(category);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			AddParameters(command, category);
			command.Parameters.AddWithValue("@Id", category.Id);

			await command.ExecuteNonQueryAsync();
		}

		public async Task DeleteAsync(int id)
		{
			const string sql =
				"""
                DELETE FROM LearningAreaCategory
                WHERE Id = @Id;
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			command.Parameters.AddWithValue("@Id", id);

			await command.ExecuteNonQueryAsync();
		}

		private static void PrepareForSave(LearningAreaCategory category)
		{
			category.Name = NormalizeRequired(category.Name);
		}

		private static void AddParameters(SqlCommand command, LearningAreaCategory category)
		{
			command.Parameters.AddWithValue("@Name", category.Name);
			command.Parameters.AddWithValue("@Sort", category.Sort);
		}

		private static string NormalizeRequired(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
		}
	}
}
using AFCore.Interfaces;
using AFCore.Models;
using AFData.Database;
using Microsoft.Data.SqlClient;

namespace AFData.Repositories
{
	public class LearningAreaRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public LearningAreaRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<LearningArea>> GetAllAsync()
		{
			const string sql =
				"""
                SELECT
                    Id,
                    Category,
                    Code,
                    Description,
                    Sort
                FROM LearningArea
                ORDER BY Sort, Description, Id;
                """;

			List<LearningArea> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			using SqlDataReader reader = await command.ExecuteReaderAsync();

			while (await reader.ReadAsync())
			{
				items.Add(new LearningArea
				{
					Id = reader.GetInt32(reader.GetOrdinal("Id")),
					Category = reader.GetString(reader.GetOrdinal("Category")),
					Code = reader.GetString(reader.GetOrdinal("Code")),
					Description = reader.GetString(reader.GetOrdinal("Description")),
					Sort = reader.GetInt32(reader.GetOrdinal("Sort"))
				});
			}

			return items;
		}

		public async Task<int> InsertAsync(LearningArea learningArea)
		{
			const string sql =
				"""
                INSERT INTO LearningArea
                (
                    Category,
                    Code,
                    Description,
                    Sort
                )
                VALUES
                (
                    @Category,
                    @Code,
                    @Description,
                    @Sort
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			command.Parameters.AddWithValue("@Category", learningArea.Category);
			command.Parameters.AddWithValue("@Code", learningArea.Code);
			command.Parameters.AddWithValue("@Description", learningArea.Description);
			command.Parameters.AddWithValue("@Sort", learningArea.Sort);

			object? result = await command.ExecuteScalarAsync();
			return result is int id ? id : 0;
		}
	}
}
using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Models;
using NibSphere.Data.Database;

namespace NibSphere.Data.Repositories
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
                    la.Id,
                    la.Category,
                    la.Code,
                    la.ShortName,
                    la.Description,
                    la.AcademicGroupId,
                    ag.Name AS AcademicGroupName,
                    la.CategoryId,
                    lac.Name AS CategoryName,
                    la.Sort
                FROM LearningArea la
                LEFT JOIN AcademicGroup ag
                    ON la.AcademicGroupId = ag.Id
                LEFT JOIN LearningAreaCategory lac
                    ON la.CategoryId = lac.Id
                ORDER BY
                    la.Sort,
                    COALESCE(NULLIF(la.ShortName, ''), la.Description),
                    la.Description,
                    la.Id;
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
					Category = reader["Category"] as string ?? string.Empty,
					Code = reader.GetString(reader.GetOrdinal("Code")),
					ShortName = reader["ShortName"] as string ?? string.Empty,
					Description = reader.GetString(reader.GetOrdinal("Description")),
					AcademicGroupId = reader["AcademicGroupId"] == DBNull.Value
						? null
						: reader.GetInt32(reader.GetOrdinal("AcademicGroupId")),
					AcademicGroupName = reader["AcademicGroupName"] as string ?? string.Empty,
					CategoryId = reader["CategoryId"] == DBNull.Value
						? null
						: reader.GetInt32(reader.GetOrdinal("CategoryId")),
					CategoryName = reader["CategoryName"] as string ?? string.Empty,
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
                    ShortName,
                    Description,
                    AcademicGroupId,
                    CategoryId,
                    Sort
                )
                VALUES
                (
                    @Category,
                    @Code,
                    @ShortName,
                    @Description,
                    @AcademicGroupId,
                    @CategoryId,
                    @Sort
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			PrepareLearningAreaForSave(learningArea);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			AddParameters(command, learningArea);

			object? result = await command.ExecuteScalarAsync();
			return result is int id ? id : 0;
		}

		public async Task UpdateAsync(LearningArea learningArea)
		{
			const string sql =
				"""
                UPDATE LearningArea
                SET
                    Category = @Category,
                    Code = @Code,
                    ShortName = @ShortName,
                    Description = @Description,
                    AcademicGroupId = @AcademicGroupId,
                    CategoryId = @CategoryId,
                    Sort = @Sort,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			PrepareLearningAreaForSave(learningArea);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			AddParameters(command, learningArea);
			command.Parameters.AddWithValue("@Id", learningArea.Id);

			await command.ExecuteNonQueryAsync();
		}

		public async Task DeleteAsync(int id)
		{
			const string sql =
				"""
                DELETE FROM LearningArea
                WHERE Id = @Id;
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			command.Parameters.AddWithValue("@Id", id);

			await command.ExecuteNonQueryAsync();
		}

		private static void PrepareLearningAreaForSave(LearningArea learningArea)
		{
			learningArea.Code = NormalizeRequired(learningArea.Code);
			learningArea.ShortName = Normalize(learningArea.ShortName) ?? string.Empty;
			learningArea.Description = NormalizeRequired(learningArea.Description);

			// Keep the legacy text Category column populated for backward compatibility.
			// Prefer the existing text value, then fall back to CategoryName if present.
			learningArea.Category = Normalize(learningArea.Category)
				?? Normalize(learningArea.CategoryName)
				?? string.Empty;

			learningArea.AcademicGroupName = Normalize(learningArea.AcademicGroupName) ?? string.Empty;
			learningArea.CategoryName = Normalize(learningArea.CategoryName) ?? string.Empty;
		}

		private static void AddParameters(SqlCommand command, LearningArea learningArea)
		{
			command.Parameters.AddWithValue("@Category", learningArea.Category);
			command.Parameters.AddWithValue("@Code", learningArea.Code);
			command.Parameters.AddWithValue("@ShortName", (object?)Normalize(learningArea.ShortName) ?? DBNull.Value);
			command.Parameters.AddWithValue("@Description", learningArea.Description);
			command.Parameters.AddWithValue("@AcademicGroupId", (object?)learningArea.AcademicGroupId ?? DBNull.Value);
			command.Parameters.AddWithValue("@CategoryId", (object?)learningArea.CategoryId ?? DBNull.Value);
			command.Parameters.AddWithValue("@Sort", learningArea.Sort);
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
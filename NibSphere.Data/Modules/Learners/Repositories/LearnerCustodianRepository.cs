using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Learners.Models;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Learners.Repositories
{
	public class LearnerCustodianRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public LearnerCustodianRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<LearnerCustodian>> GetByLearnerIdAsync(int learnerId)
		{
			const string sql =
				"""
				SELECT
				    Id,
				    LearnerId,
				    CustodianId,
				    RelationshipType,
				    RelationshipLabel,
				    HasCustody,
				    LivesWithLearner,
				    SortOrder
				FROM Learners_LearnerCustodian
				WHERE LearnerId = @LearnerId
				ORDER BY
				    SortOrder,
				    Id;
				""";

			List<LearnerCustodian> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@LearnerId", learnerId);

			using SqlDataReader reader = await command.ExecuteReaderAsync();

			while (await reader.ReadAsync())
			{
				items.Add(MapLearnerCustodian(reader));
			}

			return items;
		}

		public async Task<int> InsertAsync(LearnerCustodian item)
		{
			const string sql =
				"""
				INSERT INTO Learners_LearnerCustodian
				(
				    LearnerId,
				    CustodianId,
				    RelationshipType,
				    RelationshipLabel,
				    HasCustody,
				    LivesWithLearner,
				    SortOrder
				)
				VALUES
				(
				    @LearnerId,
				    @CustodianId,
				    @RelationshipType,
				    @RelationshipLabel,
				    @HasCustody,
				    @LivesWithLearner,
				    @SortOrder
				);

				SELECT CAST(SCOPE_IDENTITY() AS INT);
				""";

			PrepareForSave(item);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			AddParameters(command, item);

			object? result = await command.ExecuteScalarAsync();
			return result is int id ? id : 0;
		}

		public async Task UpdateAsync(LearnerCustodian item)
		{
			const string sql =
				"""
				UPDATE Learners_LearnerCustodian
				SET
				    LearnerId = @LearnerId,
				    CustodianId = @CustodianId,
				    RelationshipType = @RelationshipType,
				    RelationshipLabel = @RelationshipLabel,
				    HasCustody = @HasCustody,
				    LivesWithLearner = @LivesWithLearner,
				    SortOrder = @SortOrder,
				    UpdatedAt = GETDATE()
				WHERE Id = @Id;
				""";

			PrepareForSave(item);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			AddParameters(command, item);
			command.Parameters.AddWithValue("@Id", item.Id);

			await command.ExecuteNonQueryAsync();
		}

		public async Task DeleteAsync(int id)
		{
			const string sql =
				"""
				DELETE FROM Learners_LearnerCustodian
				WHERE Id = @Id;
				""";

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@Id", id);

			await command.ExecuteNonQueryAsync();
		}

		public async Task DeleteByLearnerIdAsync(int learnerId)
		{
			const string sql =
				"""
				DELETE FROM Learners_LearnerCustodian
				WHERE LearnerId = @LearnerId;
				""";

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@LearnerId", learnerId);

			await command.ExecuteNonQueryAsync();
		}

		private static LearnerCustodian MapLearnerCustodian(SqlDataReader reader)
		{
			return new LearnerCustodian
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				LearnerId = reader.GetInt32(reader.GetOrdinal("LearnerId")),
				CustodianId = reader.GetInt32(reader.GetOrdinal("CustodianId")),
				RelationshipType = reader["RelationshipType"] as string ?? string.Empty,
				RelationshipLabel = reader["RelationshipLabel"] as string ?? string.Empty,
				HasCustody = reader.GetBoolean(reader.GetOrdinal("HasCustody")),
				LivesWithLearner = reader.GetBoolean(reader.GetOrdinal("LivesWithLearner")),
				SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder"))
			};
		}

		private static void AddParameters(SqlCommand command, LearnerCustodian item)
		{
			command.Parameters.AddWithValue("@LearnerId", item.LearnerId);
			command.Parameters.AddWithValue("@CustodianId", item.CustodianId);
			command.Parameters.AddWithValue("@RelationshipType", item.RelationshipType);
			command.Parameters.AddWithValue("@RelationshipLabel", item.RelationshipLabel);
			command.Parameters.AddWithValue("@HasCustody", item.HasCustody);
			command.Parameters.AddWithValue("@LivesWithLearner", item.LivesWithLearner);
			command.Parameters.AddWithValue("@SortOrder", item.SortOrder);
		}

		private static void PrepareForSave(LearnerCustodian item)
		{
			item.RelationshipType = NormalizeRequired(item.RelationshipType);
			item.RelationshipLabel = NormalizeRequired(item.RelationshipLabel);
		}

		private static string NormalizeRequired(string value)
		{
			return string.IsNullOrWhiteSpace(value)
				? string.Empty
				: value.Trim();
		}
	}
}
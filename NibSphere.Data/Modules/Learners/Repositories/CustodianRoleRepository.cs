using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Learners.Models;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Learners.Repositories
{
	public class CustodianRoleRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public CustodianRoleRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<CustodianRole>> GetAllAsync()
		{
			const string sql =
				"""
				SELECT
				    Id,
				    RelationshipType,
				    RelationshipLabel,
				    SortOrder
				FROM Learners_CustodianRole
				ORDER BY
				    SortOrder,
				    RelationshipType,
				    RelationshipLabel,
				    Id;
				""";

			List<CustodianRole> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			using SqlDataReader reader = await command.ExecuteReaderAsync();

			while (await reader.ReadAsync())
			{
				items.Add(new CustodianRole
				{
					Id = reader.GetInt32(reader.GetOrdinal("Id")),
					RelationshipType = reader["RelationshipType"] as string ?? string.Empty,
					RelationshipLabel = reader["RelationshipLabel"] as string ?? string.Empty,
					SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder"))
				});
			}

			return items;
		}

		public async Task<int> InsertAsync(CustodianRole role)
		{
			const string sql =
				"""
				INSERT INTO Learners_CustodianRole
				(
				    RelationshipType,
				    RelationshipLabel,
				    SortOrder
				)
				VALUES
				(
				    @RelationshipType,
				    @RelationshipLabel,
				    @SortOrder
				);

				SELECT CAST(SCOPE_IDENTITY() AS INT);
				""";

			PrepareForSave(role);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			AddParameters(command, role);

			object? result = await command.ExecuteScalarAsync();
			return result is int id ? id : 0;
		}

		public async Task UpdateAsync(CustodianRole role)
		{
			const string sql =
				"""
				UPDATE Learners_CustodianRole
				SET
				    RelationshipType = @RelationshipType,
				    RelationshipLabel = @RelationshipLabel,
				    SortOrder = @SortOrder,
				    UpdatedAt = GETDATE()
				WHERE Id = @Id;
				""";

			PrepareForSave(role);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			AddParameters(command, role);
			command.Parameters.AddWithValue("@Id", role.Id);

			await command.ExecuteNonQueryAsync();
		}

		public async Task DeleteAsync(int id)
		{
			const string sql =
				"""
				DELETE FROM Learners_CustodianRole
				WHERE Id = @Id;
				""";

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@Id", id);

			await command.ExecuteNonQueryAsync();
		}

		private static void AddParameters(SqlCommand command, CustodianRole role)
		{
			command.Parameters.AddWithValue("@RelationshipType", role.RelationshipType);
			command.Parameters.AddWithValue("@RelationshipLabel", role.RelationshipLabel);
			command.Parameters.AddWithValue("@SortOrder", role.SortOrder);
		}

		private static void PrepareForSave(CustodianRole role)
		{
			role.RelationshipType = NormalizeRequired(role.RelationshipType);
			role.RelationshipLabel = NormalizeRequired(role.RelationshipLabel);
		}

		private static string NormalizeRequired(string value)
		{
			return string.IsNullOrWhiteSpace(value)
				? string.Empty
				: value.Trim();
		}
	}
}
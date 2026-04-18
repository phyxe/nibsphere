using AFCore.Interfaces;
using AFCore.Models;
using AFData.Database;
using Microsoft.Data.SqlClient;

namespace AFData.Repositories
{
	public class SchoolProfileRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public SchoolProfileRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<SchoolProfile?> GetSchoolProfileAsync()
		{
			const string sql =
				"""
                SELECT TOP 1
                    Id,
                    SchoolName,
                    SchoolId,
                    Region,
                    Division,
                    District,
                    SchoolHeadName,
                    SchoolHeadPosition
                FROM SchoolProfile
                ORDER BY Id;
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			using SqlDataReader reader = await command.ExecuteReaderAsync();

			if (!await reader.ReadAsync())
			{
				return null;
			}

			return new SchoolProfile
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				SchoolName = reader.GetString(reader.GetOrdinal("SchoolName")),
				SchoolId = reader["SchoolId"] as string,
				Region = reader["Region"] as string,
				Division = reader["Division"] as string,
				District = reader["District"] as string,
				SchoolHeadName = reader["SchoolHeadName"] as string,
				SchoolHeadPosition = reader["SchoolHeadPosition"] as string
			};
		}

		public async Task<int> InsertSchoolProfileAsync(SchoolProfile schoolProfile)
		{
			const string sql =
				"""
                INSERT INTO SchoolProfile
                (
                    SchoolName,
                    SchoolId,
                    Region,
                    Division,
                    District,
                    SchoolHeadName,
                    SchoolHeadPosition
                )
                VALUES
                (
                    @SchoolName,
                    @SchoolId,
                    @Region,
                    @Division,
                    @District,
                    @SchoolHeadName,
                    @SchoolHeadPosition
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			AddSchoolProfileParameters(command, schoolProfile);

			object? result = await command.ExecuteScalarAsync();
			return result is int id ? id : 0;
		}

		public async Task UpdateSchoolProfileAsync(SchoolProfile schoolProfile)
		{
			const string sql =
				"""
                UPDATE SchoolProfile
                SET
                    SchoolName = @SchoolName,
                    SchoolId = @SchoolId,
                    Region = @Region,
                    Division = @Division,
                    District = @District,
                    SchoolHeadName = @SchoolHeadName,
                    SchoolHeadPosition = @SchoolHeadPosition,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			AddSchoolProfileParameters(command, schoolProfile);
			command.Parameters.AddWithValue("@Id", schoolProfile.Id);

			await command.ExecuteNonQueryAsync();
		}

		private static void AddSchoolProfileParameters(SqlCommand command, SchoolProfile schoolProfile)
		{
			command.Parameters.AddWithValue("@SchoolName", schoolProfile.SchoolName);
			command.Parameters.AddWithValue("@SchoolId", (object?)schoolProfile.SchoolId ?? DBNull.Value);
			command.Parameters.AddWithValue("@Region", (object?)schoolProfile.Region ?? DBNull.Value);
			command.Parameters.AddWithValue("@Division", (object?)schoolProfile.Division ?? DBNull.Value);
			command.Parameters.AddWithValue("@District", (object?)schoolProfile.District ?? DBNull.Value);
			command.Parameters.AddWithValue("@SchoolHeadName", (object?)schoolProfile.SchoolHeadName ?? DBNull.Value);
			command.Parameters.AddWithValue("@SchoolHeadPosition", (object?)schoolProfile.SchoolHeadPosition ?? DBNull.Value);
		}
	}
}
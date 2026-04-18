using AFCore.Interfaces;
using AFCore.Models;
using AFData.Database;
using Microsoft.Data.SqlClient;

namespace AFData.Repositories
{
	public class AppUserProfileRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public AppUserProfileRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<AppUserProfile?> GetPrimaryUserProfileAsync()
		{
			const string sql =
				"""
                SELECT TOP 1
                    Id,
                    FullName,
                    PositionTitle,
                    EmailAddress,
                    ContactNumber,
                    SignaturePath,
                    IsPrimary
                FROM AppUserProfile
                WHERE IsPrimary = 1
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

			return new AppUserProfile
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				FullName = reader.GetString(reader.GetOrdinal("FullName")),
				PositionTitle = reader["PositionTitle"] as string,
				EmailAddress = reader["EmailAddress"] as string,
				ContactNumber = reader["ContactNumber"] as string,
				SignaturePath = reader["SignaturePath"] as string,
				IsPrimary = reader.GetBoolean(reader.GetOrdinal("IsPrimary"))
			};
		}

		public async Task<int> InsertPrimaryUserProfileAsync(AppUserProfile userProfile)
		{
			const string sql =
				"""
                INSERT INTO AppUserProfile
                (
                    FullName,
                    PositionTitle,
                    EmailAddress,
                    ContactNumber,
                    SignaturePath,
                    IsPrimary
                )
                VALUES
                (
                    @FullName,
                    @PositionTitle,
                    @EmailAddress,
                    @ContactNumber,
                    @SignaturePath,
                    @IsPrimary
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			AddParameters(command, userProfile);

			object? result = await command.ExecuteScalarAsync();
			return result is int id ? id : 0;
		}

		public async Task UpdateUserProfileAsync(AppUserProfile userProfile)
		{
			const string sql =
				"""
                UPDATE AppUserProfile
                SET
                    FullName = @FullName,
                    PositionTitle = @PositionTitle,
                    EmailAddress = @EmailAddress,
                    ContactNumber = @ContactNumber,
                    SignaturePath = @SignaturePath,
                    IsPrimary = @IsPrimary,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			AddParameters(command, userProfile);
			command.Parameters.AddWithValue("@Id", userProfile.Id);

			await command.ExecuteNonQueryAsync();
		}

		private static void AddParameters(SqlCommand command, AppUserProfile userProfile)
		{
			command.Parameters.AddWithValue("@FullName", userProfile.FullName);
			command.Parameters.AddWithValue("@PositionTitle", (object?)userProfile.PositionTitle ?? DBNull.Value);
			command.Parameters.AddWithValue("@EmailAddress", (object?)userProfile.EmailAddress ?? DBNull.Value);
			command.Parameters.AddWithValue("@ContactNumber", (object?)userProfile.ContactNumber ?? DBNull.Value);
			command.Parameters.AddWithValue("@SignaturePath", (object?)userProfile.SignaturePath ?? DBNull.Value);
			command.Parameters.AddWithValue("@IsPrimary", userProfile.IsPrimary);
		}
	}
}
using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Models;
using NibSphere.Data.Database;

namespace NibSphere.Data.Repositories
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
                    UserUid,
                    AppInstanceUid,
                    FirstName,
                    LastName,
                    MiddleName,
                    ExtensionName,
                    FullName,
                    PositionTitle,
                    EmailAddress,
                    ContactNumber,
                    ProfileImagePath,
                    SignaturePath,
                    ThemePreference,
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
				UserUid = reader["UserUid"] == DBNull.Value ? null : reader.GetGuid(reader.GetOrdinal("UserUid")),
				AppInstanceUid = reader["AppInstanceUid"] == DBNull.Value ? null : reader.GetGuid(reader.GetOrdinal("AppInstanceUid")),
				FirstName = reader["FirstName"] as string,
				LastName = reader["LastName"] as string,
				MiddleName = reader["MiddleName"] as string,
				ExtensionName = reader["ExtensionName"] as string,
				FullName = reader.GetString(reader.GetOrdinal("FullName")),
				PositionTitle = reader["PositionTitle"] as string,
				EmailAddress = reader["EmailAddress"] as string,
				ContactNumber = reader["ContactNumber"] as string,
				ProfileImagePath = reader["ProfileImagePath"] as string,
				SignaturePath = reader["SignaturePath"] as string,
				ThemePreference = reader["ThemePreference"] as string,
				IsPrimary = reader.GetBoolean(reader.GetOrdinal("IsPrimary"))
			};
		}

		public async Task<int> InsertPrimaryUserProfileAsync(AppUserProfile userProfile)
		{
			const string sql =
				"""
                INSERT INTO AppUserProfile
                (
                    UserUid,
                    AppInstanceUid,
                    FirstName,
                    LastName,
                    MiddleName,
                    ExtensionName,
                    FullName,
                    PositionTitle,
                    EmailAddress,
                    ContactNumber,
                    ProfileImagePath,
                    SignaturePath,
                    ThemePreference,
                    IsPrimary
                )
                VALUES
                (
                    @UserUid,
                    @AppInstanceUid,
                    @FirstName,
                    @LastName,
                    @MiddleName,
                    @ExtensionName,
                    @FullName,
                    @PositionTitle,
                    @EmailAddress,
                    @ContactNumber,
                    @ProfileImagePath,
                    @SignaturePath,
                    @ThemePreference,
                    @IsPrimary
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			PrepareUserProfileForSave(userProfile);

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
                    UserUid = @UserUid,
                    AppInstanceUid = @AppInstanceUid,
                    FirstName = @FirstName,
                    LastName = @LastName,
                    MiddleName = @MiddleName,
                    ExtensionName = @ExtensionName,
                    FullName = @FullName,
                    PositionTitle = @PositionTitle,
                    EmailAddress = @EmailAddress,
                    ContactNumber = @ContactNumber,
                    ProfileImagePath = @ProfileImagePath,
                    SignaturePath = @SignaturePath,
                    ThemePreference = @ThemePreference,
                    IsPrimary = @IsPrimary,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			PrepareUserProfileForSave(userProfile);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			AddParameters(command, userProfile);
			command.Parameters.AddWithValue("@Id", userProfile.Id);

			await command.ExecuteNonQueryAsync();
		}

		private static void PrepareUserProfileForSave(AppUserProfile userProfile)
		{
			if (userProfile.UserUid == null || userProfile.UserUid == Guid.Empty)
			{
				userProfile.UserUid = Guid.NewGuid();
			}

			if (userProfile.AppInstanceUid == null || userProfile.AppInstanceUid == Guid.Empty)
			{
				userProfile.AppInstanceUid = Guid.NewGuid();
			}

			userProfile.FirstName = Normalize(userProfile.FirstName);
			userProfile.LastName = Normalize(userProfile.LastName);
			userProfile.MiddleName = Normalize(userProfile.MiddleName);
			userProfile.ExtensionName = Normalize(userProfile.ExtensionName);
			userProfile.PositionTitle = Normalize(userProfile.PositionTitle);
			userProfile.EmailAddress = Normalize(userProfile.EmailAddress);
			userProfile.ContactNumber = Normalize(userProfile.ContactNumber);
			userProfile.ProfileImagePath = Normalize(userProfile.ProfileImagePath);
			userProfile.SignaturePath = Normalize(userProfile.SignaturePath);
			userProfile.ThemePreference = Normalize(userProfile.ThemePreference);

			string computedFullName = userProfile.BuildFullName();
			userProfile.FullName = string.IsNullOrWhiteSpace(computedFullName)
				? string.Empty
				: computedFullName;
		}

		private static void AddParameters(SqlCommand command, AppUserProfile userProfile)
		{
			command.Parameters.AddWithValue("@UserUid", (object?)userProfile.UserUid ?? DBNull.Value);
			command.Parameters.AddWithValue("@AppInstanceUid", (object?)userProfile.AppInstanceUid ?? DBNull.Value);
			command.Parameters.AddWithValue("@FirstName", (object?)userProfile.FirstName ?? DBNull.Value);
			command.Parameters.AddWithValue("@LastName", (object?)userProfile.LastName ?? DBNull.Value);
			command.Parameters.AddWithValue("@MiddleName", (object?)userProfile.MiddleName ?? DBNull.Value);
			command.Parameters.AddWithValue("@ExtensionName", (object?)userProfile.ExtensionName ?? DBNull.Value);
			command.Parameters.AddWithValue("@FullName", userProfile.FullName);
			command.Parameters.AddWithValue("@PositionTitle", (object?)userProfile.PositionTitle ?? DBNull.Value);
			command.Parameters.AddWithValue("@EmailAddress", (object?)userProfile.EmailAddress ?? DBNull.Value);
			command.Parameters.AddWithValue("@ContactNumber", (object?)userProfile.ContactNumber ?? DBNull.Value);
			command.Parameters.AddWithValue("@ProfileImagePath", (object?)userProfile.ProfileImagePath ?? DBNull.Value);
			command.Parameters.AddWithValue("@SignaturePath", (object?)userProfile.SignaturePath ?? DBNull.Value);
			command.Parameters.AddWithValue("@ThemePreference", (object?)userProfile.ThemePreference ?? DBNull.Value);
			command.Parameters.AddWithValue("@IsPrimary", userProfile.IsPrimary);
		}

		private static string? Normalize(string? value)
		{
			return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
		}
	}
}
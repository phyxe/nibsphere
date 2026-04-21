using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Learners.Models;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Learners.Repositories
{
	public class CustodianRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public CustodianRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<Custodian>> GetAllAsync()
		{
			const string sql =
				"""
				SELECT
				    Id,
				    FirstName,
				    MiddleName,
				    LastName,
				    ExtensionName,
				    MobileNumber,
				    Email
				FROM Learners_Custodian
				ORDER BY
				    LastName,
				    FirstName,
				    MiddleName,
				    ExtensionName,
				    Id;
				""";

			List<Custodian> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			using SqlDataReader reader = await command.ExecuteReaderAsync();

			while (await reader.ReadAsync())
			{
				items.Add(MapCustodian(reader));
			}

			return items;
		}

		public async Task<Custodian?> GetByIdAsync(int id)
		{
			const string sql =
				"""
				SELECT
				    Id,
				    FirstName,
				    MiddleName,
				    LastName,
				    ExtensionName,
				    MobileNumber,
				    Email
				FROM Learners_Custodian
				WHERE Id = @Id;
				""";

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@Id", id);

			using SqlDataReader reader = await command.ExecuteReaderAsync();

			if (await reader.ReadAsync())
			{
				return MapCustodian(reader);
			}

			return null;
		}

		public async Task<List<Custodian>> FindPotentialMatchesAsync(
			string lastName,
			string firstName,
			string? middleName)
		{
			const string sql =
				"""
				SELECT
				    Id,
				    FirstName,
				    MiddleName,
				    LastName,
				    ExtensionName,
				    MobileNumber,
				    Email
				FROM Learners_Custodian
				WHERE
				    LastName = @LastName
				    AND FirstName = @FirstName
				    AND ISNULL(MiddleName, '') = ISNULL(@MiddleName, '')
				ORDER BY
				    LastName,
				    FirstName,
				    MiddleName,
				    Id;
				""";

			List<Custodian> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@LastName", NormalizeRequired(lastName));
			command.Parameters.AddWithValue("@FirstName", NormalizeRequired(firstName));
			command.Parameters.AddWithValue("@MiddleName", (object?)Normalize(middleName) ?? DBNull.Value);

			using SqlDataReader reader = await command.ExecuteReaderAsync();

			while (await reader.ReadAsync())
			{
				items.Add(MapCustodian(reader));
			}

			return items;
		}

		public async Task<int> InsertAsync(Custodian custodian)
		{
			const string sql =
				"""
				INSERT INTO Learners_Custodian
				(
				    FirstName,
				    MiddleName,
				    LastName,
				    ExtensionName,
				    MobileNumber,
				    Email
				)
				VALUES
				(
				    @FirstName,
				    @MiddleName,
				    @LastName,
				    @ExtensionName,
				    @MobileNumber,
				    @Email
				);

				SELECT CAST(SCOPE_IDENTITY() AS INT);
				""";

			PrepareForSave(custodian);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			AddParameters(command, custodian);

			object? result = await command.ExecuteScalarAsync();
			return result is int id ? id : 0;
		}

		public async Task UpdateAsync(Custodian custodian)
		{
			const string sql =
				"""
				UPDATE Learners_Custodian
				SET
				    FirstName = @FirstName,
				    MiddleName = @MiddleName,
				    LastName = @LastName,
				    ExtensionName = @ExtensionName,
				    MobileNumber = @MobileNumber,
				    Email = @Email,
				    UpdatedAt = GETDATE()
				WHERE Id = @Id;
				""";

			PrepareForSave(custodian);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			AddParameters(command, custodian);
			command.Parameters.AddWithValue("@Id", custodian.Id);

			await command.ExecuteNonQueryAsync();
		}

		public async Task DeleteAsync(int id)
		{
			const string sql =
				"""
				DELETE FROM Learners_Custodian
				WHERE Id = @Id;
				""";

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@Id", id);

			await command.ExecuteNonQueryAsync();
		}

		private static Custodian MapCustodian(SqlDataReader reader)
		{
			return new Custodian
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				FirstName = reader["FirstName"] as string ?? string.Empty,
				MiddleName = reader["MiddleName"] as string ?? string.Empty,
				LastName = reader["LastName"] as string ?? string.Empty,
				ExtensionName = reader["ExtensionName"] as string ?? string.Empty,
				MobileNumber = reader["MobileNumber"] as string ?? string.Empty,
				Email = reader["Email"] as string ?? string.Empty
			};
		}

		private static void AddParameters(SqlCommand command, Custodian custodian)
		{
			command.Parameters.AddWithValue("@FirstName", custodian.FirstName);
			command.Parameters.AddWithValue("@MiddleName", (object?)Normalize(custodian.MiddleName) ?? DBNull.Value);
			command.Parameters.AddWithValue("@LastName", custodian.LastName);
			command.Parameters.AddWithValue("@ExtensionName", (object?)Normalize(custodian.ExtensionName) ?? DBNull.Value);
			command.Parameters.AddWithValue("@MobileNumber", (object?)Normalize(custodian.MobileNumber) ?? DBNull.Value);
			command.Parameters.AddWithValue("@Email", (object?)Normalize(custodian.Email) ?? DBNull.Value);
		}

		private static void PrepareForSave(Custodian custodian)
		{
			custodian.FirstName = NormalizeRequired(custodian.FirstName);
			custodian.MiddleName = Normalize(custodian.MiddleName) ?? string.Empty;
			custodian.LastName = NormalizeRequired(custodian.LastName);
			custodian.ExtensionName = Normalize(custodian.ExtensionName) ?? string.Empty;
			custodian.MobileNumber = Normalize(custodian.MobileNumber) ?? string.Empty;
			custodian.Email = Normalize(custodian.Email) ?? string.Empty;
		}

		private static string? Normalize(string? value)
		{
			return string.IsNullOrWhiteSpace(value)
				? null
				: value.Trim();
		}

		private static string NormalizeRequired(string value)
		{
			return string.IsNullOrWhiteSpace(value)
				? string.Empty
				: value.Trim();
		}
	}
}
using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Learners.Models;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Learners.Repositories
{
	public class LearnerRepository
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public LearnerRepository(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public async Task<List<Learner>> GetAllAsync()
		{
			const string sql =
				"""
				SELECT
				    Id,
				    FirstName,
				    MiddleName,
				    LastName,
				    ExtensionName,
				    Birthday,
				    Sex,
				    Pronoun,
				    Lrn,
				    ReligiousAffiliation,
				    HouseStreetSitioPurok,
				    Barangay,
				    Municipality,
				    Province,
				    MobileNumber,
				    Email,
				    ProfilePicturePath
				FROM Learners_Learner
				ORDER BY
				    LastName,
				    FirstName,
				    MiddleName,
				    ExtensionName,
				    Id;
				""";

			List<Learner> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			using SqlDataReader reader = await command.ExecuteReaderAsync();

			while (await reader.ReadAsync())
			{
				items.Add(MapLearner(reader));
			}

			return items;
		}

		public async Task<Learner?> GetByIdAsync(int id)
		{
			const string sql =
				"""
				SELECT
				    Id,
				    FirstName,
				    MiddleName,
				    LastName,
				    ExtensionName,
				    Birthday,
				    Sex,
				    Pronoun,
				    Lrn,
				    ReligiousAffiliation,
				    HouseStreetSitioPurok,
				    Barangay,
				    Municipality,
				    Province,
				    MobileNumber,
				    Email,
				    ProfilePicturePath
				FROM Learners_Learner
				WHERE Id = @Id;
				""";

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@Id", id);

			using SqlDataReader reader = await command.ExecuteReaderAsync();

			if (await reader.ReadAsync())
			{
				return MapLearner(reader);
			}

			return null;
		}

		public async Task<List<Learner>> FindPotentialMatchesAsync(
			string? lrn,
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
				    Birthday,
				    Sex,
				    Pronoun,
				    Lrn,
				    ReligiousAffiliation,
				    HouseStreetSitioPurok,
				    Barangay,
				    Municipality,
				    Province,
				    MobileNumber,
				    Email,
				    ProfilePicturePath
				FROM Learners_Learner
				WHERE
				    (
				        @Lrn IS NOT NULL
				        AND Lrn = @Lrn
				    )
				    OR
				    (
				        LastName = @LastName
				        AND FirstName = @FirstName
				        AND ISNULL(MiddleName, '') = ISNULL(@MiddleName, '')
				    )
				ORDER BY
				    LastName,
				    FirstName,
				    MiddleName,
				    Id;
				""";

			List<Learner> items = new();

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@Lrn", (object?)Normalize(lrn) ?? DBNull.Value);
			command.Parameters.AddWithValue("@LastName", NormalizeRequired(lastName));
			command.Parameters.AddWithValue("@FirstName", NormalizeRequired(firstName));
			command.Parameters.AddWithValue("@MiddleName", (object?)Normalize(middleName) ?? DBNull.Value);

			using SqlDataReader reader = await command.ExecuteReaderAsync();

			while (await reader.ReadAsync())
			{
				items.Add(MapLearner(reader));
			}

			return items;
		}

		public async Task<int> InsertAsync(Learner learner)
		{
			const string sql =
				"""
				INSERT INTO Learners_Learner
				(
				    FirstName,
				    MiddleName,
				    LastName,
				    ExtensionName,
				    Birthday,
				    Sex,
				    Pronoun,
				    Lrn,
				    ReligiousAffiliation,
				    HouseStreetSitioPurok,
				    Barangay,
				    Municipality,
				    Province,
				    MobileNumber,
				    Email,
				    ProfilePicturePath
				)
				VALUES
				(
				    @FirstName,
				    @MiddleName,
				    @LastName,
				    @ExtensionName,
				    @Birthday,
				    @Sex,
				    @Pronoun,
				    @Lrn,
				    @ReligiousAffiliation,
				    @HouseStreetSitioPurok,
				    @Barangay,
				    @Municipality,
				    @Province,
				    @MobileNumber,
				    @Email,
				    @ProfilePicturePath
				);

				SELECT CAST(SCOPE_IDENTITY() AS INT);
				""";

			PrepareForSave(learner);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			AddParameters(command, learner);

			object? result = await command.ExecuteScalarAsync();
			return result is int id ? id : 0;
		}

		public async Task UpdateAsync(Learner learner)
		{
			const string sql =
				"""
				UPDATE Learners_Learner
				SET
				    FirstName = @FirstName,
				    MiddleName = @MiddleName,
				    LastName = @LastName,
				    ExtensionName = @ExtensionName,
				    Birthday = @Birthday,
				    Sex = @Sex,
				    Pronoun = @Pronoun,
				    Lrn = @Lrn,
				    ReligiousAffiliation = @ReligiousAffiliation,
				    HouseStreetSitioPurok = @HouseStreetSitioPurok,
				    Barangay = @Barangay,
				    Municipality = @Municipality,
				    Province = @Province,
				    MobileNumber = @MobileNumber,
				    Email = @Email,
				    ProfilePicturePath = @ProfilePicturePath,
				    UpdatedAt = GETDATE()
				WHERE Id = @Id;
				""";

			PrepareForSave(learner);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			AddParameters(command, learner);
			command.Parameters.AddWithValue("@Id", learner.Id);

			await command.ExecuteNonQueryAsync();
		}

		public async Task DeleteAsync(int id)
		{
			const string sql =
				"""
				DELETE FROM Learners_Learner
				WHERE Id = @Id;
				""";

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@Id", id);

			await command.ExecuteNonQueryAsync();
		}

		private static Learner MapLearner(SqlDataReader reader)
		{
			return new Learner
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				FirstName = reader["FirstName"] as string ?? string.Empty,
				MiddleName = reader["MiddleName"] as string ?? string.Empty,
				LastName = reader["LastName"] as string ?? string.Empty,
				ExtensionName = reader["ExtensionName"] as string ?? string.Empty,
				Birthday = reader["Birthday"] == DBNull.Value
					? null
					: reader.GetDateTime(reader.GetOrdinal("Birthday")),
				Sex = reader["Sex"] as string ?? string.Empty,
				Pronoun = reader["Pronoun"] as string ?? string.Empty,
				Lrn = reader["Lrn"] as string ?? string.Empty,
				ReligiousAffiliation = reader["ReligiousAffiliation"] as string ?? string.Empty,
				HouseStreetSitioPurok = reader["HouseStreetSitioPurok"] as string ?? string.Empty,
				Barangay = reader["Barangay"] as string ?? string.Empty,
				Municipality = reader["Municipality"] as string ?? string.Empty,
				Province = reader["Province"] as string ?? string.Empty,
				MobileNumber = reader["MobileNumber"] as string ?? string.Empty,
				Email = reader["Email"] as string ?? string.Empty,
				ProfilePicturePath = reader["ProfilePicturePath"] as string ?? string.Empty
			};
		}

		private static void AddParameters(SqlCommand command, Learner learner)
		{
			command.Parameters.AddWithValue("@FirstName", learner.FirstName);
			command.Parameters.AddWithValue("@MiddleName", (object?)Normalize(learner.MiddleName) ?? DBNull.Value);
			command.Parameters.AddWithValue("@LastName", learner.LastName);
			command.Parameters.AddWithValue("@ExtensionName", (object?)Normalize(learner.ExtensionName) ?? DBNull.Value);
			command.Parameters.AddWithValue("@Birthday", (object?)learner.Birthday ?? DBNull.Value);
			command.Parameters.AddWithValue("@Sex", (object?)Normalize(learner.Sex) ?? DBNull.Value);
			command.Parameters.AddWithValue("@Pronoun", (object?)Normalize(learner.Pronoun) ?? DBNull.Value);
			command.Parameters.AddWithValue("@Lrn", (object?)Normalize(learner.Lrn) ?? DBNull.Value);
			command.Parameters.AddWithValue("@ReligiousAffiliation", (object?)Normalize(learner.ReligiousAffiliation) ?? DBNull.Value);
			command.Parameters.AddWithValue("@HouseStreetSitioPurok", (object?)Normalize(learner.HouseStreetSitioPurok) ?? DBNull.Value);
			command.Parameters.AddWithValue("@Barangay", (object?)Normalize(learner.Barangay) ?? DBNull.Value);
			command.Parameters.AddWithValue("@Municipality", (object?)Normalize(learner.Municipality) ?? DBNull.Value);
			command.Parameters.AddWithValue("@Province", (object?)Normalize(learner.Province) ?? DBNull.Value);
			command.Parameters.AddWithValue("@MobileNumber", (object?)Normalize(learner.MobileNumber) ?? DBNull.Value);
			command.Parameters.AddWithValue("@Email", (object?)Normalize(learner.Email) ?? DBNull.Value);
			command.Parameters.AddWithValue("@ProfilePicturePath", (object?)Normalize(learner.ProfilePicturePath) ?? DBNull.Value);
		}

		private static void PrepareForSave(Learner learner)
		{
			learner.FirstName = NormalizeRequired(learner.FirstName);
			learner.MiddleName = Normalize(learner.MiddleName) ?? string.Empty;
			learner.LastName = NormalizeRequired(learner.LastName);
			learner.ExtensionName = Normalize(learner.ExtensionName) ?? string.Empty;
			learner.Sex = Normalize(learner.Sex) ?? string.Empty;
			learner.Pronoun = Normalize(learner.Pronoun) ?? string.Empty;
			learner.Lrn = Normalize(learner.Lrn) ?? string.Empty;
			learner.ReligiousAffiliation = Normalize(learner.ReligiousAffiliation) ?? string.Empty;
			learner.HouseStreetSitioPurok = Normalize(learner.HouseStreetSitioPurok) ?? string.Empty;
			learner.Barangay = Normalize(learner.Barangay) ?? string.Empty;
			learner.Municipality = Normalize(learner.Municipality) ?? string.Empty;
			learner.Province = Normalize(learner.Province) ?? string.Empty;
			learner.MobileNumber = Normalize(learner.MobileNumber) ?? string.Empty;
			learner.Email = Normalize(learner.Email) ?? string.Empty;
			learner.ProfilePicturePath = Normalize(learner.ProfilePicturePath) ?? string.Empty;
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
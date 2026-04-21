using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Learners.Models;
using NibSphere.Core.Modules.Learners.Profile;
using NibSphere.Data.Database;

namespace NibSphere.Data.Modules.Learners.Profile
{
	public sealed class LearnerProfileService
	{
		private readonly LocalDbConnectionFactory _connectionFactory;

		public LearnerProfileService(IAppPaths appPaths)
		{
			_connectionFactory = new LocalDbConnectionFactory(appPaths);
		}

		public LearnerProfileRecord CreateNew()
		{
			return new LearnerProfileRecord
			{
				Mode = LearnerProfileMode.Add,
				Learner = new Learner(),
				Custodians = new List<LearnerCustodianCardItem>()
			};
		}

		public async Task<LearnerProfileRecord?> GetByLearnerIdAsync(
			int learnerId,
			LearnerProfileMode mode = LearnerProfileMode.View,
			CancellationToken cancellationToken = default)
		{
			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			Learner? learner = await GetLearnerAsync(connection, learnerId, cancellationToken);

			if (learner == null)
			{
				return null;
			}

			List<LearnerCustodianCardItem> custodians =
				await GetCustodianCardsAsync(connection, learnerId, cancellationToken);

			return new LearnerProfileRecord
			{
				Mode = mode,
				Learner = learner,
				Custodians = custodians
			};
		}

		public async Task<int> SaveAsync(
			LearnerProfileRecord profile,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(profile);
			ArgumentNullException.ThrowIfNull(profile.Learner);

			PrepareLearnerForSave(profile.Learner);
			PrepareCustodianCardsForSave(profile.Custodians);

			if (string.IsNullOrWhiteSpace(profile.Learner.FirstName))
			{
				throw new InvalidOperationException("Learner First Name is required.");
			}

			if (string.IsNullOrWhiteSpace(profile.Learner.LastName))
			{
				throw new InvalidOperationException("Learner Last Name is required.");
			}

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync(cancellationToken);

			using SqlTransaction transaction = connection.BeginTransaction();

			try
			{
				if (profile.Learner.Id <= 0)
				{
					profile.Learner.Id = await InsertLearnerAsync(
						connection,
						transaction,
						profile.Learner,
						cancellationToken);
				}
				else
				{
					await UpdateLearnerAsync(
						connection,
						transaction,
						profile.Learner,
						cancellationToken);
				}

				await DeleteLearnerCustodiansAsync(
					connection,
					transaction,
					profile.Learner.Id,
					cancellationToken);

				foreach (LearnerCustodianCardItem card in profile.Custodians
					.Where(HasMeaningfulCustodianData)
					.OrderBy(x => x.SortOrder)
					.ThenBy(x => x.RelationshipType, StringComparer.OrdinalIgnoreCase)
					.ThenBy(x => x.RelationshipLabel, StringComparer.OrdinalIgnoreCase))
				{
					PrepareCustodianForSave(card.Custodian);

					if (string.IsNullOrWhiteSpace(card.Custodian.FirstName) ||
						string.IsNullOrWhiteSpace(card.Custodian.LastName))
					{
						throw new InvalidOperationException(
							"Each custodian must have at least a First Name and Last Name.");
					}

					if (string.IsNullOrWhiteSpace(card.RelationshipType))
					{
						throw new InvalidOperationException(
							"Each custodian card must have a Relationship Type.");
					}

					if (string.IsNullOrWhiteSpace(card.RelationshipLabel))
					{
						throw new InvalidOperationException(
							"Each custodian card must have a Relationship Label.");
					}

					if (card.Custodian.Id <= 0)
					{
						card.Custodian.Id = await InsertCustodianAsync(
							connection,
							transaction,
							card.Custodian,
							cancellationToken);
					}
					else
					{
						await UpdateCustodianAsync(
							connection,
							transaction,
							card.Custodian,
							cancellationToken);
					}

					await InsertLearnerCustodianAsync(
						connection,
						transaction,
						profile.Learner.Id,
						card,
						cancellationToken);
				}

				await transaction.CommitAsync(cancellationToken);
				return profile.Learner.Id;
			}
			catch
			{
				await transaction.RollbackAsync(cancellationToken);
				throw;
			}
		}

		private static async Task<Learner?> GetLearnerAsync(
			SqlConnection connection,
			int learnerId,
			CancellationToken cancellationToken)
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

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@Id", learnerId);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			if (!await reader.ReadAsync(cancellationToken))
			{
				return null;
			}

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

		private static async Task<List<LearnerCustodianCardItem>> GetCustodianCardsAsync(
			SqlConnection connection,
			int learnerId,
			CancellationToken cancellationToken)
		{
			const string sql =
				"""
				SELECT
				    lc.Id AS LearnerCustodianId,
				    lc.LearnerId,
				    lc.CustodianId,
				    lc.RelationshipType,
				    lc.RelationshipLabel,
				    lc.HasCustody,
				    lc.LivesWithLearner,
				    lc.SortOrder,
				    c.FirstName,
				    c.MiddleName,
				    c.LastName,
				    c.ExtensionName,
				    c.MobileNumber,
				    c.Email
				FROM Learners_LearnerCustodian lc
				INNER JOIN Learners_Custodian c
				    ON lc.CustodianId = c.Id
				WHERE lc.LearnerId = @LearnerId
				ORDER BY
				    lc.SortOrder,
				    lc.Id;
				""";

			List<LearnerCustodianCardItem> items = new();

			using SqlCommand command = new(sql, connection);
			command.Parameters.AddWithValue("@LearnerId", learnerId);

			using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				items.Add(new LearnerCustodianCardItem
				{
					LearnerCustodianId = reader.GetInt32(reader.GetOrdinal("LearnerCustodianId")),
					LearnerId = reader.GetInt32(reader.GetOrdinal("LearnerId")),
					RelationshipType = reader["RelationshipType"] as string ?? string.Empty,
					RelationshipLabel = reader["RelationshipLabel"] as string ?? string.Empty,
					HasCustody = reader.GetBoolean(reader.GetOrdinal("HasCustody")),
					LivesWithLearner = reader.GetBoolean(reader.GetOrdinal("LivesWithLearner")),
					SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
					Custodian = new Custodian
					{
						Id = reader.GetInt32(reader.GetOrdinal("CustodianId")),
						FirstName = reader["FirstName"] as string ?? string.Empty,
						MiddleName = reader["MiddleName"] as string ?? string.Empty,
						LastName = reader["LastName"] as string ?? string.Empty,
						ExtensionName = reader["ExtensionName"] as string ?? string.Empty,
						MobileNumber = reader["MobileNumber"] as string ?? string.Empty,
						Email = reader["Email"] as string ?? string.Empty
					}
				});
			}

			return items;
		}

		private static async Task<int> InsertLearnerAsync(
			SqlConnection connection,
			SqlTransaction transaction,
			Learner learner,
			CancellationToken cancellationToken)
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

			using SqlCommand command = new(sql, connection, transaction);
			AddLearnerParameters(command, learner);

			object? result = await command.ExecuteScalarAsync(cancellationToken);
			return result is int id ? id : 0;
		}

		private static async Task UpdateLearnerAsync(
			SqlConnection connection,
			SqlTransaction transaction,
			Learner learner,
			CancellationToken cancellationToken)
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

			using SqlCommand command = new(sql, connection, transaction);
			AddLearnerParameters(command, learner);
			command.Parameters.AddWithValue("@Id", learner.Id);

			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		private static async Task<int> InsertCustodianAsync(
			SqlConnection connection,
			SqlTransaction transaction,
			Custodian custodian,
			CancellationToken cancellationToken)
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

			using SqlCommand command = new(sql, connection, transaction);
			AddCustodianParameters(command, custodian);

			object? result = await command.ExecuteScalarAsync(cancellationToken);
			return result is int id ? id : 0;
		}

		private static async Task UpdateCustodianAsync(
			SqlConnection connection,
			SqlTransaction transaction,
			Custodian custodian,
			CancellationToken cancellationToken)
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

			using SqlCommand command = new(sql, connection, transaction);
			AddCustodianParameters(command, custodian);
			command.Parameters.AddWithValue("@Id", custodian.Id);

			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		private static async Task InsertLearnerCustodianAsync(
			SqlConnection connection,
			SqlTransaction transaction,
			int learnerId,
			LearnerCustodianCardItem card,
			CancellationToken cancellationToken)
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

			using SqlCommand command = new(sql, connection, transaction);
			command.Parameters.AddWithValue("@LearnerId", learnerId);
			command.Parameters.AddWithValue("@CustodianId", card.Custodian.Id);
			command.Parameters.AddWithValue("@RelationshipType", card.RelationshipType);
			command.Parameters.AddWithValue("@RelationshipLabel", card.RelationshipLabel);
			command.Parameters.AddWithValue("@HasCustody", card.HasCustody);
			command.Parameters.AddWithValue("@LivesWithLearner", card.LivesWithLearner);
			command.Parameters.AddWithValue("@SortOrder", card.SortOrder);

			object? result = await command.ExecuteScalarAsync(cancellationToken);
			card.LearnerCustodianId = result is int id ? id : 0;
			card.LearnerId = learnerId;
		}

		private static async Task DeleteLearnerCustodiansAsync(
			SqlConnection connection,
			SqlTransaction transaction,
			int learnerId,
			CancellationToken cancellationToken)
		{
			const string sql =
				"""
				DELETE FROM Learners_LearnerCustodian
				WHERE LearnerId = @LearnerId;
				""";

			using SqlCommand command = new(sql, connection, transaction);
			command.Parameters.AddWithValue("@LearnerId", learnerId);

			await command.ExecuteNonQueryAsync(cancellationToken);
		}

		private static void AddLearnerParameters(SqlCommand command, Learner learner)
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

		private static void AddCustodianParameters(SqlCommand command, Custodian custodian)
		{
			command.Parameters.AddWithValue("@FirstName", custodian.FirstName);
			command.Parameters.AddWithValue("@MiddleName", (object?)Normalize(custodian.MiddleName) ?? DBNull.Value);
			command.Parameters.AddWithValue("@LastName", custodian.LastName);
			command.Parameters.AddWithValue("@ExtensionName", (object?)Normalize(custodian.ExtensionName) ?? DBNull.Value);
			command.Parameters.AddWithValue("@MobileNumber", (object?)Normalize(custodian.MobileNumber) ?? DBNull.Value);
			command.Parameters.AddWithValue("@Email", (object?)Normalize(custodian.Email) ?? DBNull.Value);
		}

		private static void PrepareLearnerForSave(Learner learner)
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

		private static void PrepareCustodianCardsForSave(IEnumerable<LearnerCustodianCardItem>? cards)
		{
			if (cards == null)
			{
				return;
			}

			int sortOrder = 1;

			foreach (LearnerCustodianCardItem card in cards)
			{
				card.RelationshipType = Normalize(card.RelationshipType) ?? string.Empty;
				card.RelationshipLabel = Normalize(card.RelationshipLabel) ?? string.Empty;

				if (card.SortOrder <= 0)
				{
					card.SortOrder = sortOrder;
				}

				sortOrder++;
			}
		}

		private static void PrepareCustodianForSave(Custodian custodian)
		{
			custodian.FirstName = NormalizeRequired(custodian.FirstName);
			custodian.MiddleName = Normalize(custodian.MiddleName) ?? string.Empty;
			custodian.LastName = NormalizeRequired(custodian.LastName);
			custodian.ExtensionName = Normalize(custodian.ExtensionName) ?? string.Empty;
			custodian.MobileNumber = Normalize(custodian.MobileNumber) ?? string.Empty;
			custodian.Email = Normalize(custodian.Email) ?? string.Empty;
		}

		private static bool HasMeaningfulCustodianData(LearnerCustodianCardItem? card)
		{
			if (card == null)
			{
				return false;
			}

			return
				card.Custodian.Id > 0 ||
				!string.IsNullOrWhiteSpace(card.Custodian.FirstName) ||
				!string.IsNullOrWhiteSpace(card.Custodian.LastName) ||
				!string.IsNullOrWhiteSpace(card.RelationshipType) ||
				!string.IsNullOrWhiteSpace(card.RelationshipLabel);
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
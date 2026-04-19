using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Models;
using NibSphere.Data.Database;

namespace NibSphere.Data.Repositories
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
                    SchoolUid,
                    SchoolName,
                    SchoolId,
                    SchoolAcronym,
                    Region,
                    Division,
                    District,
                    ProvinceCode,
                    ProvinceName,
                    MunicipalityCityCode,
                    MunicipalityCityName,
                    BarangayCode,
                    BarangayName,
                    AddressLine,
                    SchoolLogoPath,
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
				SchoolUid = reader["SchoolUid"] == DBNull.Value ? null : reader.GetGuid(reader.GetOrdinal("SchoolUid")),
				SchoolName = reader.GetString(reader.GetOrdinal("SchoolName")),
				SchoolId = reader["SchoolId"] as string,
				SchoolAcronym = reader["SchoolAcronym"] as string,
				Region = reader["Region"] as string,
				Division = reader["Division"] as string,
				District = reader["District"] as string,
				ProvinceCode = reader["ProvinceCode"] as string,
				ProvinceName = reader["ProvinceName"] as string,
				MunicipalityCityCode = reader["MunicipalityCityCode"] as string,
				MunicipalityCityName = reader["MunicipalityCityName"] as string,
				BarangayCode = reader["BarangayCode"] as string,
				BarangayName = reader["BarangayName"] as string,
				AddressLine = reader["AddressLine"] as string,
				SchoolLogoPath = reader["SchoolLogoPath"] as string,
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
                    SchoolUid,
                    SchoolName,
                    SchoolId,
                    SchoolAcronym,
                    Region,
                    Division,
                    District,
                    ProvinceCode,
                    ProvinceName,
                    MunicipalityCityCode,
                    MunicipalityCityName,
                    BarangayCode,
                    BarangayName,
                    AddressLine,
                    SchoolLogoPath,
                    SchoolHeadName,
                    SchoolHeadPosition
                )
                VALUES
                (
                    @SchoolUid,
                    @SchoolName,
                    @SchoolId,
                    @SchoolAcronym,
                    @Region,
                    @Division,
                    @District,
                    @ProvinceCode,
                    @ProvinceName,
                    @MunicipalityCityCode,
                    @MunicipalityCityName,
                    @BarangayCode,
                    @BarangayName,
                    @AddressLine,
                    @SchoolLogoPath,
                    @SchoolHeadName,
                    @SchoolHeadPosition
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

			PrepareSchoolProfileForSave(schoolProfile);

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
                    SchoolUid = @SchoolUid,
                    SchoolName = @SchoolName,
                    SchoolId = @SchoolId,
                    SchoolAcronym = @SchoolAcronym,
                    Region = @Region,
                    Division = @Division,
                    District = @District,
                    ProvinceCode = @ProvinceCode,
                    ProvinceName = @ProvinceName,
                    MunicipalityCityCode = @MunicipalityCityCode,
                    MunicipalityCityName = @MunicipalityCityName,
                    BarangayCode = @BarangayCode,
                    BarangayName = @BarangayName,
                    AddressLine = @AddressLine,
                    SchoolLogoPath = @SchoolLogoPath,
                    SchoolHeadName = @SchoolHeadName,
                    SchoolHeadPosition = @SchoolHeadPosition,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;
                """;

			PrepareSchoolProfileForSave(schoolProfile);

			using SqlConnection connection = _connectionFactory.CreateAppConnection();
			await connection.OpenAsync();

			using SqlCommand command = new SqlCommand(sql, connection);
			AddSchoolProfileParameters(command, schoolProfile);
			command.Parameters.AddWithValue("@Id", schoolProfile.Id);

			await command.ExecuteNonQueryAsync();
		}

		private static void PrepareSchoolProfileForSave(SchoolProfile schoolProfile)
		{
			if (schoolProfile.SchoolUid == null || schoolProfile.SchoolUid == Guid.Empty)
			{
				schoolProfile.SchoolUid = Guid.NewGuid();
			}

			schoolProfile.SchoolName = NormalizeRequired(schoolProfile.SchoolName);
			schoolProfile.SchoolId = Normalize(schoolProfile.SchoolId);
			schoolProfile.SchoolAcronym = Normalize(schoolProfile.SchoolAcronym);
			schoolProfile.Region = Normalize(schoolProfile.Region);
			schoolProfile.Division = Normalize(schoolProfile.Division);
			schoolProfile.District = Normalize(schoolProfile.District);
			schoolProfile.ProvinceCode = Normalize(schoolProfile.ProvinceCode);
			schoolProfile.ProvinceName = Normalize(schoolProfile.ProvinceName);
			schoolProfile.MunicipalityCityCode = Normalize(schoolProfile.MunicipalityCityCode);
			schoolProfile.MunicipalityCityName = Normalize(schoolProfile.MunicipalityCityName);
			schoolProfile.BarangayCode = Normalize(schoolProfile.BarangayCode);
			schoolProfile.BarangayName = Normalize(schoolProfile.BarangayName);
			schoolProfile.AddressLine = Normalize(schoolProfile.AddressLine);
			schoolProfile.SchoolLogoPath = Normalize(schoolProfile.SchoolLogoPath);
			schoolProfile.SchoolHeadName = Normalize(schoolProfile.SchoolHeadName);
			schoolProfile.SchoolHeadPosition = Normalize(schoolProfile.SchoolHeadPosition);
		}

		private static void AddSchoolProfileParameters(SqlCommand command, SchoolProfile schoolProfile)
		{
			command.Parameters.AddWithValue("@SchoolUid", (object?)schoolProfile.SchoolUid ?? DBNull.Value);
			command.Parameters.AddWithValue("@SchoolName", schoolProfile.SchoolName);
			command.Parameters.AddWithValue("@SchoolId", (object?)schoolProfile.SchoolId ?? DBNull.Value);
			command.Parameters.AddWithValue("@SchoolAcronym", (object?)schoolProfile.SchoolAcronym ?? DBNull.Value);
			command.Parameters.AddWithValue("@Region", (object?)schoolProfile.Region ?? DBNull.Value);
			command.Parameters.AddWithValue("@Division", (object?)schoolProfile.Division ?? DBNull.Value);
			command.Parameters.AddWithValue("@District", (object?)schoolProfile.District ?? DBNull.Value);
			command.Parameters.AddWithValue("@ProvinceCode", (object?)schoolProfile.ProvinceCode ?? DBNull.Value);
			command.Parameters.AddWithValue("@ProvinceName", (object?)schoolProfile.ProvinceName ?? DBNull.Value);
			command.Parameters.AddWithValue("@MunicipalityCityCode", (object?)schoolProfile.MunicipalityCityCode ?? DBNull.Value);
			command.Parameters.AddWithValue("@MunicipalityCityName", (object?)schoolProfile.MunicipalityCityName ?? DBNull.Value);
			command.Parameters.AddWithValue("@BarangayCode", (object?)schoolProfile.BarangayCode ?? DBNull.Value);
			command.Parameters.AddWithValue("@BarangayName", (object?)schoolProfile.BarangayName ?? DBNull.Value);
			command.Parameters.AddWithValue("@AddressLine", (object?)schoolProfile.AddressLine ?? DBNull.Value);
			command.Parameters.AddWithValue("@SchoolLogoPath", (object?)schoolProfile.SchoolLogoPath ?? DBNull.Value);
			command.Parameters.AddWithValue("@SchoolHeadName", (object?)schoolProfile.SchoolHeadName ?? DBNull.Value);
			command.Parameters.AddWithValue("@SchoolHeadPosition", (object?)schoolProfile.SchoolHeadPosition ?? DBNull.Value);
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
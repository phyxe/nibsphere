namespace NibSphere.Core.Models
{
	public class SchoolProfile
	{
		public int Id { get; set; }

		public Guid? SchoolUid { get; set; }

		public string SchoolName { get; set; } = string.Empty;
		public string? SchoolId { get; set; }
		public string? SchoolAcronym { get; set; }

		public string? Region { get; set; }
		public string? Division { get; set; }
		public string? District { get; set; }

		public string? ProvinceCode { get; set; }
		public string? ProvinceName { get; set; }

		public string? MunicipalityCityCode { get; set; }
		public string? MunicipalityCityName { get; set; }

		public string? BarangayCode { get; set; }
		public string? BarangayName { get; set; }

		public string? AddressLine { get; set; }

		public string? SchoolLogoPath { get; set; }

		public string? SchoolHeadName { get; set; }
		public string? SchoolHeadPosition { get; set; }
	}
}
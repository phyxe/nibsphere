namespace AFCore.Models
{
	public class SchoolProfile
	{
		public int Id { get; set; }
		public string SchoolName { get; set; } = string.Empty;
		public string? SchoolAddress { get; set; }
		public string? SchoolLogoPath { get; set; }
		public string? SchoolHeadName { get; set; }
		public string? SchoolHeadPosition { get; set; }
	}
}
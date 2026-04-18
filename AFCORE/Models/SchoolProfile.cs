namespace AFCore.Models
{
	public class SchoolProfile
	{
		public int Id { get; set; }

		public string SchoolName { get; set; } = string.Empty;
		public string? SchoolId { get; set; }

		public string? Region { get; set; }
		public string? Division { get; set; }
		public string? District { get; set; }

		public string? SchoolHeadName { get; set; }
		public string? SchoolHeadPosition { get; set; }
	}
}
namespace NibSphere.Core.Models
{
	public class AppUserProfile
	{
		public int Id { get; set; }
		public string FullName { get; set; } = string.Empty;
		public string? PositionTitle { get; set; }
		public string? EmailAddress { get; set; }
		public string? ContactNumber { get; set; }
		public string? SignaturePath { get; set; }
		public string? ThemePreference { get; set; }
		public bool IsPrimary { get; set; }
	}
}
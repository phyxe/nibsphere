namespace NibSphere.Core.Models
{
	public class LearningArea
	{
		public int Id { get; set; }

		// Legacy text category kept temporarily so the existing SettingsView flow
		// does not break while the new Learning Areas view is being built.
		public string Category { get; set; } = string.Empty;

		public string Code { get; set; } = string.Empty;
		public string ShortName { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;

		public int? AcademicGroupId { get; set; }
		public string AcademicGroupName { get; set; } = string.Empty;

		public int? CategoryId { get; set; }
		public string CategoryName { get; set; } = string.Empty;

		public int Sort { get; set; }
	}
}
namespace NibSphere.Core.Modules.Academics.Setup
{
	public sealed class AcademicsSectionTemplate
	{
		public int Id { get; set; }

		public string GradeLevelName { get; set; } = string.Empty;
		public string SectionName { get; set; } = string.Empty;

		public int SortOrder { get; set; }
		public bool IsActive { get; set; } = true;

		public string DisplayName => $"{GradeLevelName} - {SectionName}".Trim();
	}
}
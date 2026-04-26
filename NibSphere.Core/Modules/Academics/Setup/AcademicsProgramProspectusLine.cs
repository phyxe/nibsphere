namespace NibSphere.Core.Modules.Academics.Setup
{
	public sealed class AcademicsProgramProspectusLine
	{
		public int Id { get; set; }

		public int ProgramId { get; set; }

		public string GradeLevelName { get; set; } = string.Empty;

		public int TemplateTermSequence { get; set; }
		public string TemplateTermLabel { get; set; } = string.Empty;

		public int LearningAreaId { get; set; }
		public string LearningAreaCode { get; set; } = string.Empty;
		public string LearningAreaShortName { get; set; } = string.Empty;
		public string LearningAreaDescription { get; set; } = string.Empty;

		public int SortOrder { get; set; }
		public bool IsActive { get; set; } = true;
	}
}
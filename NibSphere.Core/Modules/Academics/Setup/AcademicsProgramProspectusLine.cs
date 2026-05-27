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

		public int DependentRecordCount { get; set; }

		public bool IsEditable { get; set; } = true;
		public bool IsDeletable { get; set; } = true;

		public string LearningAreaDisplayName
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(LearningAreaShortName))
				{
					return LearningAreaShortName;
				}

				if (!string.IsNullOrWhiteSpace(LearningAreaDescription))
				{
					return LearningAreaDescription;
				}

				return LearningAreaCode;
			}
		}

		public string TermDisplayName => $"{TemplateTermSequence}. {TemplateTermLabel}".Trim();

		public string DisplayName => $"{GradeLevelName} - {TermDisplayName} - {LearningAreaDisplayName}".Trim();
	}
}
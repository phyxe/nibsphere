namespace NibSphere.Core.Modules.Academics.SchoolYears
{
	public sealed class AcademicsSchoolYearProgramLine
	{
		public int Id { get; set; }

		public int SchoolYearProgramId { get; set; }

		public int SourceProgramProspectusLineId { get; set; }

		public string GradeLevelName { get; set; } = string.Empty;

		public int TemplateTermSequence { get; set; }
		public string TemplateTermLabel { get; set; } = string.Empty;

		public int LearningAreaId { get; set; }

		public string LearningAreaCode { get; set; } = string.Empty;
		public string LearningAreaShortName { get; set; } = string.Empty;
		public string LearningAreaDescription { get; set; } = string.Empty;

		public int SortOrder { get; set; }

		public bool IsActive { get; set; } = true;

		public string LearningAreaDisplayName
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(LearningAreaShortName))
				{
					return LearningAreaShortName;
				}

				if (!string.IsNullOrWhiteSpace(LearningAreaCode))
				{
					return LearningAreaCode;
				}

				return LearningAreaDescription;
			}
		}
	}
}
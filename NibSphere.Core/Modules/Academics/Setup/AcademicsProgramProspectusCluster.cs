namespace NibSphere.Core.Modules.Academics.Setup
{
	public sealed class AcademicsProgramProspectusCluster
	{
		public int ProgramId { get; set; }

		public string GradeLevelName { get; set; } = string.Empty;

		public int TemplateTermSequence { get; set; }
		public string TemplateTermLabel { get; set; } = string.Empty;

		public int SortOrder { get; set; }

		public bool IsActive { get; set; } = true;

		public int LearningAreaCount { get; set; }
		public int ActiveLearningAreaCount { get; set; }

		public int DependentRecordCount { get; set; }

		public bool IsEditable { get; set; } = true;
		public bool IsDeletable { get; set; } = true;

		public List<AcademicsProgramProspectusClusterLearningArea> LearningAreas { get; set; } = new();

		public string DisplayName
		{
			get
			{
				if (string.IsNullOrWhiteSpace(GradeLevelName))
				{
					return TemplateTermLabel;
				}

				if (string.IsNullOrWhiteSpace(TemplateTermLabel))
				{
					return GradeLevelName;
				}

				return $"{GradeLevelName}, {TemplateTermLabel}";
			}
		}

		public string SummaryDisplay
		{
			get
			{
				string learningAreaText = LearningAreaCount == 1
					? "1 learning area"
					: $"{LearningAreaCount} learning areas";

				return $"{learningAreaText} • Sequence {TemplateTermSequence}";
			}
		}
	}
}
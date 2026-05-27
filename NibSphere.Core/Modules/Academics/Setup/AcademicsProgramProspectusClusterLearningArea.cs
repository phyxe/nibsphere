namespace NibSphere.Core.Modules.Academics.Setup
{
	public sealed class AcademicsProgramProspectusClusterLearningArea
	{
		public int ProspectusLineId { get; set; }

		public int LearningAreaId { get; set; }

		public string LearningAreaCode { get; set; } = string.Empty;
		public string LearningAreaShortName { get; set; } = string.Empty;
		public string LearningAreaDescription { get; set; } = string.Empty;

		public int SortOrder { get; set; }

		public bool IsActive { get; set; } = true;

		public int DependentRecordCount { get; set; }

		public string DisplayName
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(LearningAreaShortName))
				{
					return $"{LearningAreaShortName} - {LearningAreaDescription}".Trim();
				}

				if (!string.IsNullOrWhiteSpace(LearningAreaDescription))
				{
					return LearningAreaDescription;
				}

				return LearningAreaCode;
			}
		}
	}
}
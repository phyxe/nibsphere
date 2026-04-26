namespace NibSphere.Core.Modules.Academics.SchoolYears
{
	public sealed class AcademicsSchoolYearSection
	{
		public int Id { get; set; }

		public int SchoolYearId { get; set; }
		public string SchoolYearName { get; set; } = string.Empty;

		public int? SourceSectionTemplateId { get; set; }

		public string GradeLevelName { get; set; } = string.Empty;
		public string SectionName { get; set; } = string.Empty;

		public int? AdviserTeacherId { get; set; }

		public string AdviserLastName { get; set; } = string.Empty;
		public string AdviserFirstName { get; set; } = string.Empty;
		public string AdviserMiddleName { get; set; } = string.Empty;
		public string AdviserExtensionName { get; set; } = string.Empty;

		public string AdviserPosition { get; set; } = string.Empty;
		public string AdviserDesignation { get; set; } = string.Empty;

		public int SortOrder { get; set; }

		public bool IsActive { get; set; } = true;

		public string DisplayName => $"{GradeLevelName} - {SectionName}".Trim();

		public string AdviserFullNameDisplay
		{
			get
			{
				if (string.IsNullOrWhiteSpace(AdviserLastName) &&
					string.IsNullOrWhiteSpace(AdviserFirstName))
				{
					return string.Empty;
				}

				string fullName = $"{AdviserLastName}, {AdviserFirstName}".Trim().Trim(',');

				if (!string.IsNullOrWhiteSpace(AdviserMiddleName))
				{
					fullName += $" {AdviserMiddleName.Trim()}";
				}

				if (!string.IsNullOrWhiteSpace(AdviserExtensionName))
				{
					fullName += $" {AdviserExtensionName.Trim()}";
				}

				return fullName.Trim();
			}
		}
	}
}
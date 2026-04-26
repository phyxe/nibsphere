namespace NibSphere.Core.Modules.Academics.SchoolYears
{
	public sealed class AcademicsSchoolYearSectionProgram
	{
		public int Id { get; set; }

		public int SchoolYearSectionId { get; set; }

		public string GradeLevelName { get; set; } = string.Empty;
		public string SectionName { get; set; } = string.Empty;

		public int SchoolYearProgramId { get; set; }

		public string ProgramCode { get; set; } = string.Empty;
		public string ProgramName { get; set; } = string.Empty;

		public int SortOrder { get; set; }

		public bool IsActive { get; set; } = true;

		public string SectionDisplayName => $"{GradeLevelName} - {SectionName}".Trim();

		public string ProgramDisplayName
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(ProgramCode))
				{
					return $"{ProgramCode} - {ProgramName}".Trim();
				}

				return ProgramName;
			}
		}
	}
}
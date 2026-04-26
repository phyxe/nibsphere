namespace NibSphere.Core.Modules.Academics.SchoolYears
{
	public sealed class AcademicsSchoolYearProgram
	{
		public int Id { get; set; }

		public int SchoolYearId { get; set; }
		public string SchoolYearName { get; set; } = string.Empty;

		public int SourceProgramId { get; set; }

		public string ProgramCode { get; set; } = string.Empty;
		public string ProgramName { get; set; } = string.Empty;
		public string ProgramDescription { get; set; } = string.Empty;

		public int SortOrder { get; set; }

		public bool IsActive { get; set; } = true;

		public string DisplayName
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
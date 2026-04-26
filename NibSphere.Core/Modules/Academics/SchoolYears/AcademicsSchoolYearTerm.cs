namespace NibSphere.Core.Modules.Academics.SchoolYears
{
	public sealed class AcademicsSchoolYearTerm
	{
		public int Id { get; set; }

		public int SchoolYearId { get; set; }

		public int? ParentTermId { get; set; }
		public string ParentTermName { get; set; } = string.Empty;

		public string Name { get; set; } = string.Empty;
		public string ShortName { get; set; } = string.Empty;

		public DateTime? StartDate { get; set; }
		public DateTime? EndDate { get; set; }

		public int SortOrder { get; set; }

		public bool IsEnrollmentTerm { get; set; }
		public bool IsGradingTerm { get; set; } = true;
		public bool IsReportingTerm { get; set; } = true;

		public bool IsActive { get; set; } = true;

		public string DisplayName
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(ShortName))
				{
					return ShortName;
				}

				return Name;
			}
		}
	}
}
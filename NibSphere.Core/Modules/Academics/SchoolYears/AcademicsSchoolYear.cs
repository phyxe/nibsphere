namespace NibSphere.Core.Modules.Academics.SchoolYears
{
	public sealed class AcademicsSchoolYear
	{
		public int Id { get; set; }

		public string Name { get; set; } = string.Empty;

		public DateTime? StartDate { get; set; }
		public DateTime? EndDate { get; set; }

		public bool IsCurrent { get; set; }
		public bool IsActive { get; set; } = true;

		public int TermCount { get; set; }
		public int DependentRecordCount { get; set; }

		public bool IsEditable { get; set; } = true;
		public bool IsDeletable { get; set; } = true;
	}
}
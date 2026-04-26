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
	}
}
namespace NibSphere.Core.Modules.Academics.Setup
{
	public sealed class AcademicsGradeLevel
	{
		public int Id { get; set; }

		public string Name { get; set; } = string.Empty;
		public string ShortName { get; set; } = string.Empty;

		public int SortOrder { get; set; }
		public bool IsActive { get; set; } = true;

		public int DependentRecordCount { get; set; }

		public bool IsEditable { get; set; } = true;
		public bool IsDeletable { get; set; } = true;

		public string DisplayName
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(ShortName))
				{
					return $"{Name} ({ShortName})";
				}

				return Name;
			}
		}
	}
}
namespace NibSphere.Core.Modules.Academics.Setup
{
	public sealed class AcademicsTeacher
	{
		public int Id { get; set; }

		public string LastName { get; set; } = string.Empty;
		public string FirstName { get; set; } = string.Empty;
		public string MiddleName { get; set; } = string.Empty;
		public string ExtensionName { get; set; } = string.Empty;

		public string Position { get; set; } = string.Empty;
		public string Designation { get; set; } = string.Empty;

		public bool IsActive { get; set; } = true;

		public string FullNameDisplay
		{
			get
			{
				string fullName = $"{LastName}, {FirstName}".Trim().Trim(',');

				if (!string.IsNullOrWhiteSpace(MiddleName))
				{
					fullName += $" {MiddleName.Trim()}";
				}

				if (!string.IsNullOrWhiteSpace(ExtensionName))
				{
					fullName += $" {ExtensionName.Trim()}";
				}

				return fullName.Trim();
			}
		}
	}
}
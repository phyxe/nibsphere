namespace NibSphere.Core.Models
{
	public class AppUserProfile
	{
		public int Id { get; set; }

		public Guid? UserUid { get; set; }
		public Guid? AppInstanceUid { get; set; }

		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public string? MiddleName { get; set; }
		public string? ExtensionName { get; set; }

		public string FullName { get; set; } = string.Empty;
		public string? PositionTitle { get; set; }
		public string? EmailAddress { get; set; }
		public string? ContactNumber { get; set; }

		public string? ProfileImagePath { get; set; }
		public string? SignaturePath { get; set; }

		public string? ThemePreference { get; set; }
		public bool IsPrimary { get; set; }

		public string BuildFullName()
		{
			List<string> parts = new();

			if (!string.IsNullOrWhiteSpace(FirstName))
			{
				parts.Add(FirstName.Trim());
			}

			if (!string.IsNullOrWhiteSpace(MiddleName))
			{
				parts.Add(MiddleName.Trim());
			}

			if (!string.IsNullOrWhiteSpace(LastName))
			{
				parts.Add(LastName.Trim());
			}

			string fullName = string.Join(" ", parts);

			if (!string.IsNullOrWhiteSpace(ExtensionName))
			{
				fullName = string.IsNullOrWhiteSpace(fullName)
					? ExtensionName.Trim()
					: $"{fullName} {ExtensionName.Trim()}";
			}

			return fullName.Trim();
		}

		public string GetDisplayFullName()
		{
			if (!string.IsNullOrWhiteSpace(FullName))
			{
				return FullName.Trim();
			}

			return BuildFullName();
		}
	}
}
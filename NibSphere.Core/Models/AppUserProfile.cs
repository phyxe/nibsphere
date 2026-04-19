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
			string? firstName = Normalize(FirstName);
			string? middleName = Normalize(MiddleName);
			string? lastName = Normalize(LastName);
			string? extensionName = Normalize(ExtensionName);

			List<string> mainParts = new();

			if (!string.IsNullOrWhiteSpace(firstName))
			{
				mainParts.Add(firstName);
			}

			if (!string.IsNullOrWhiteSpace(middleName))
			{
				mainParts.Add($"{char.ToUpperInvariant(middleName[0])}.");
			}

			if (!string.IsNullOrWhiteSpace(lastName))
			{
				mainParts.Add(lastName);
			}

			string fullName = string.Join(" ", mainParts).Trim();

			if (!string.IsNullOrWhiteSpace(extensionName))
			{
				fullName = string.IsNullOrWhiteSpace(fullName)
					? extensionName
					: $"{fullName}, {extensionName}";
			}

			return fullName;
		}

		public string GetDisplayFullName()
		{
			if (!string.IsNullOrWhiteSpace(FullName))
			{
				return FullName.Trim();
			}

			return BuildFullName();
		}

		private static string? Normalize(string? value)
		{
			return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
		}
	}
}
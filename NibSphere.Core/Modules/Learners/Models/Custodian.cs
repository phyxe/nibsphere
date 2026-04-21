namespace NibSphere.Core.Modules.Learners.Models
{
	public class Custodian
	{
		public int Id { get; set; }

		public string FirstName { get; set; } = string.Empty;
		public string MiddleName { get; set; } = string.Empty;
		public string LastName { get; set; } = string.Empty;
		public string ExtensionName { get; set; } = string.Empty;

		public string MobileNumber { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;

		public string BuildFullName()
		{
			List<string> parts = new();

			if (!string.IsNullOrWhiteSpace(LastName))
			{
				parts.Add(LastName.Trim());
			}

			if (!string.IsNullOrWhiteSpace(FirstName))
			{
				parts.Add(FirstName.Trim());
			}

			if (!string.IsNullOrWhiteSpace(MiddleName))
			{
				parts.Add(MiddleName.Trim());
			}

			string coreName = string.Join(", ", parts.Take(1)) +
				(parts.Count > 1 ? " " + string.Join(" ", parts.Skip(1)) : string.Empty);

			if (!string.IsNullOrWhiteSpace(ExtensionName))
			{
				coreName = string.IsNullOrWhiteSpace(coreName)
					? ExtensionName.Trim()
					: $"{coreName} {ExtensionName.Trim()}";
			}

			return coreName.Trim();
		}
	}
}
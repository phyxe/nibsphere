using NibSphere.Core.Modules.Learners.Models;

namespace NibSphere.Core.Modules.Learners.Profile
{
	public class LearnerProfileRecord
	{
		public LearnerProfileMode Mode { get; set; }

		public Learner Learner { get; set; } = new();

		public List<LearnerCustodianCardItem> Custodians { get; set; } = new();

		public string BuildHeaderDisplayName()
		{
			string firstName = Learner.FirstName?.Trim() ?? string.Empty;
			string middleName = Learner.MiddleName?.Trim() ?? string.Empty;
			string lastName = Learner.LastName?.Trim() ?? string.Empty;
			string extensionName = Learner.ExtensionName?.Trim() ?? string.Empty;

			string middleInitial = string.IsNullOrWhiteSpace(middleName)
				? string.Empty
				: $"{middleName[0]}.";

			List<string> parts = new();

			if (!string.IsNullOrWhiteSpace(firstName))
			{
				parts.Add(firstName);
			}

			if (!string.IsNullOrWhiteSpace(middleInitial))
			{
				parts.Add(middleInitial);
			}

			if (!string.IsNullOrWhiteSpace(lastName))
			{
				parts.Add(lastName);
			}

			string fullName = string.Join(" ", parts);

			if (!string.IsNullOrWhiteSpace(extensionName))
			{
				fullName = string.IsNullOrWhiteSpace(fullName)
					? extensionName
					: $"{fullName} {extensionName}";
			}

			return string.IsNullOrWhiteSpace(fullName)
				? "LEARNER PROFILE"
				: fullName.ToUpperInvariant();
		}
	}
}
using NibSphere.Core.Modules.Learners.Models;

namespace NibSphere.Core.Modules.Learners.Profile
{
	public class LearnerCustodianCardItem
	{
		public int LearnerCustodianId { get; set; }

		public int LearnerId { get; set; }

		public Custodian Custodian { get; set; } = new();

		public string RelationshipType { get; set; } = string.Empty;

		public string RelationshipLabel { get; set; } = string.Empty;

		public bool HasCustody { get; set; }

		public bool LivesWithLearner { get; set; }

		public int SortOrder { get; set; }

		public string BuildCardTitle()
		{
			string fullName = Custodian.BuildFullName();

			if (string.IsNullOrWhiteSpace(fullName))
			{
				return string.IsNullOrWhiteSpace(RelationshipLabel)
					? "New Custodian"
					: RelationshipLabel.Trim();
			}

			return string.IsNullOrWhiteSpace(RelationshipLabel)
				? fullName
				: $"{RelationshipLabel}: {fullName}";
		}
	}
}
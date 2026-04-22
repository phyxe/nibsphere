using NibSphere.Core.Modules.Learners.Models;
using NibSphere.Core.Modules.Learners.Profile;

namespace NibSphere.Core.Modules.Learners.Importing
{
	public sealed class LearnersImportCustodianInput
	{
		public int BlockNumber { get; set; }

		public Custodian Custodian { get; set; } = new();

		public string RelationshipType { get; set; } = string.Empty;
		public string RelationshipLabel { get; set; } = string.Empty;

		public bool HasCustody { get; set; }
		public bool LivesWithLearner { get; set; }

		public bool HasAnyData()
		{
			return
				Custodian.Id > 0 ||
				!string.IsNullOrWhiteSpace(Custodian.FirstName) ||
				!string.IsNullOrWhiteSpace(Custodian.MiddleName) ||
				!string.IsNullOrWhiteSpace(Custodian.LastName) ||
				!string.IsNullOrWhiteSpace(Custodian.ExtensionName) ||
				!string.IsNullOrWhiteSpace(Custodian.MobileNumber) ||
				!string.IsNullOrWhiteSpace(Custodian.Email) ||
				!string.IsNullOrWhiteSpace(RelationshipType) ||
				!string.IsNullOrWhiteSpace(RelationshipLabel) ||
				HasCustody ||
				LivesWithLearner;
		}

		public LearnerCustodianCardItem ToCardItem(int sortOrder)
		{
			return new LearnerCustodianCardItem
			{
				Custodian = Custodian,
				RelationshipType = RelationshipType,
				RelationshipLabel = RelationshipLabel,
				HasCustody = HasCustody,
				LivesWithLearner = LivesWithLearner,
				SortOrder = sortOrder
			};
		}
	}
}
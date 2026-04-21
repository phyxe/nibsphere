namespace NibSphere.Core.Modules.Learners.Models
{
	public class LearnerCustodian
	{
		public int Id { get; set; }

		public int LearnerId { get; set; }
		public int CustodianId { get; set; }

		public string RelationshipType { get; set; } = string.Empty;
		public string RelationshipLabel { get; set; } = string.Empty;

		public bool HasCustody { get; set; }
		public bool LivesWithLearner { get; set; }

		public int SortOrder { get; set; }
	}
}
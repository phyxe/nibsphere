namespace NibSphere.Core.Modules.Learners.Importing
{
	public sealed class LearnersImportCustodianColumnMap
	{
		public int BlockNumber { get; set; }

		public string? FirstNameColumnHeader { get; set; }
		public string? MiddleNameColumnHeader { get; set; }
		public string? LastNameColumnHeader { get; set; }
		public string? ExtensionNameColumnHeader { get; set; }

		public string? MobileNumberColumnHeader { get; set; }
		public string? EmailColumnHeader { get; set; }

		public string? RelationshipTypeColumnHeader { get; set; }
		public string? RelationshipLabelColumnHeader { get; set; }

		public string? HasCustodyColumnHeader { get; set; }
		public string? LivesWithLearnerColumnHeader { get; set; }

		public bool HasAnyMappedColumn()
		{
			return
				!string.IsNullOrWhiteSpace(FirstNameColumnHeader) ||
				!string.IsNullOrWhiteSpace(MiddleNameColumnHeader) ||
				!string.IsNullOrWhiteSpace(LastNameColumnHeader) ||
				!string.IsNullOrWhiteSpace(ExtensionNameColumnHeader) ||
				!string.IsNullOrWhiteSpace(MobileNumberColumnHeader) ||
				!string.IsNullOrWhiteSpace(EmailColumnHeader) ||
				!string.IsNullOrWhiteSpace(RelationshipTypeColumnHeader) ||
				!string.IsNullOrWhiteSpace(RelationshipLabelColumnHeader) ||
				!string.IsNullOrWhiteSpace(HasCustodyColumnHeader) ||
				!string.IsNullOrWhiteSpace(LivesWithLearnerColumnHeader);
		}
	}
}
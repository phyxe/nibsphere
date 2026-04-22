namespace NibSphere.Core.Modules.Learners.Importing
{
	public sealed class LearnersImportResult
	{
		public int ImportedLearnerCount { get; set; }

		public int ImportedCustodianCount { get; set; }

		public int SkippedRowCount { get; set; }

		public int DuplicateSkippedCount { get; set; }

		public int SkippedCustodianBlockCount { get; set; }

		public IReadOnlyList<string> Messages { get; set; } =
			Array.Empty<string>();
	}
}
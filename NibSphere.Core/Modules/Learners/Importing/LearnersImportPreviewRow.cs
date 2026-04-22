using NibSphere.Core.Modules.Learners.Models;

namespace NibSphere.Core.Modules.Learners.Importing
{
	public sealed class LearnersImportPreviewRow
	{
		public int SourceRowNumber { get; set; }

		public LearnersImportRowData? RowData { get; set; }

		public bool CanImport { get; set; }

		public bool HasPotentialDuplicate { get; set; }

		public int SkippedCustodianBlockCount { get; set; }

		public IReadOnlyList<Learner> PotentialDuplicateLearners { get; set; } =
			Array.Empty<Learner>();

		public IReadOnlyList<string> Errors { get; set; } =
			Array.Empty<string>();

		public IReadOnlyList<string> Warnings { get; set; } =
			Array.Empty<string>();
	}
}
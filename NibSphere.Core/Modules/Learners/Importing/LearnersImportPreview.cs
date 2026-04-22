using NibSphere.Core.Importing;

namespace NibSphere.Core.Modules.Learners.Importing
{
	public sealed class LearnersImportPreview
	{
		public ImportTableDocument? Document { get; set; }

		public ImportTableSheet? Sheet { get; set; }

		public IReadOnlyList<LearnersImportPreviewRow> Rows { get; set; } =
			Array.Empty<LearnersImportPreviewRow>();

		public int TotalRowCount => Rows.Count;

		public int ReadyToImportCount => Rows.Count(x => x.CanImport);

		public int ErrorRowCount => Rows.Count(x => x.Errors.Count > 0);

		public int DuplicateRowCount => Rows.Count(x => x.HasPotentialDuplicate);

		public int SkippedCustodianBlockCount => Rows.Sum(x => x.SkippedCustodianBlockCount);
	}
}
namespace NibSphere.Core.Importing
{
	public enum ImportSimulationIssueSeverity
	{
		Info,
		Warning,
		Error
	}

	public sealed class ImportSimulationIssue
	{
		public ImportSimulationIssueSeverity Severity { get; set; }
		public string Message { get; set; } = string.Empty;
	}

	public sealed class ImportPreviewRow<TTarget>
	{
		public int SourceRowNumber { get; set; }
		public TTarget? Item { get; set; }
		public bool CanPost { get; set; }
		public IReadOnlyDictionary<string, string?> SourceValues { get; set; } =
			new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

		public IReadOnlyList<ImportSimulationIssue> Issues { get; set; } =
			Array.Empty<ImportSimulationIssue>();
	}

	public sealed class ImportSimulationResult<TTarget>
	{
		public IReadOnlyList<ImportPreviewRow<TTarget>> PreviewRows { get; set; } =
			Array.Empty<ImportPreviewRow<TTarget>>();

		public int TotalRows => PreviewRows.Count;

		public int ValidRowCount =>
			PreviewRows.Count(x => x.CanPost);

		public int ErrorRowCount =>
			PreviewRows.Count(x => x.Issues.Any(issue => issue.Severity == ImportSimulationIssueSeverity.Error));
	}

	public sealed class ImportFinalizeResult
	{
		public int CreatedCount { get; set; }
		public int UpdatedCount { get; set; }
		public int SkippedCount { get; set; }
		public IReadOnlyList<string> Messages { get; set; } = Array.Empty<string>();
	}
}
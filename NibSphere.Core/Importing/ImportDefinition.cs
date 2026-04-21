namespace NibSphere.Core.Importing
{
	public sealed class ImportSimulationRequest
	{
		public required ImportTableDocument Document { get; init; }
		public required ImportTableSheet Sheet { get; init; }

		public IReadOnlyList<ImportColumnMapping> ColumnMappings { get; init; } =
			Array.Empty<ImportColumnMapping>();

		public IDictionary<string, object> Items { get; } =
			new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
	}

	public abstract class ImportDefinition<TTarget>
	{
		public string ModuleKey { get; protected init; } = string.Empty;
		public string Title { get; protected init; } = string.Empty;

		public IReadOnlyList<ImportFileKind> AllowedFileKinds { get; protected init; } =
			new[] { ImportFileKind.Csv, ImportFileKind.ExcelXlsx };

		public IReadOnlyList<ImportFieldDefinition<TTarget>> Fields { get; protected init; } =
			Array.Empty<ImportFieldDefinition<TTarget>>();

		public virtual Task PrepareAsync(
			ImportSimulationRequest request,
			CancellationToken cancellationToken = default)
		{
			return Task.CompletedTask;
		}

		public abstract Task<ImportSimulationResult<TTarget>> SimulateAsync(
			ImportSimulationRequest request,
			CancellationToken cancellationToken = default);

		public abstract Task<ImportFinalizeResult> FinalizeAsync(
			ImportSimulationResult<TTarget> simulationResult,
			CancellationToken cancellationToken = default);
	}

	public interface IImportFileReaderService
	{
		bool CanRead(string filePath);
		ImportFileKind DetectFileKind(string filePath);

		Task<ImportTableDocument> ReadAsync(
			string filePath,
			CancellationToken cancellationToken = default);
	}
}
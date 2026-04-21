namespace NibSphere.Core.Importing
{
	public sealed class ImportTableDocument
	{
		public string FilePath { get; set; } = string.Empty;
		public ImportFileKind FileKind { get; set; }
		public IReadOnlyList<ImportTableSheet> Sheets { get; set; } = Array.Empty<ImportTableSheet>();

		public ImportTableSheet GetRequiredSheet(string? sheetName = null)
		{
			if (Sheets.Count == 0)
			{
				throw new InvalidOperationException("The import document does not contain any sheets.");
			}

			if (string.IsNullOrWhiteSpace(sheetName))
			{
				return Sheets[0];
			}

			ImportTableSheet? match = Sheets.FirstOrDefault(x =>
				string.Equals(x.Name, sheetName, StringComparison.OrdinalIgnoreCase));

			return match ?? throw new InvalidOperationException($"Sheet '{sheetName}' was not found.");
		}
	}

	public sealed class ImportTableSheet
	{
		public string Name { get; set; } = string.Empty;
		public IReadOnlyList<string> Headers { get; set; } = Array.Empty<string>();
		public IReadOnlyList<ImportTableRow> Rows { get; set; } = Array.Empty<ImportTableRow>();

		public int GetRequiredHeaderIndex(string header)
		{
			for (int index = 0; index < Headers.Count; index++)
			{
				if (string.Equals(Headers[index], header, StringComparison.OrdinalIgnoreCase))
				{
					return index;
				}
			}

			throw new InvalidOperationException($"Header '{header}' was not found.");
		}
	}

	public sealed class ImportTableRow
	{
		public int RowNumber { get; set; }
		public IReadOnlyList<string?> Cells { get; set; } = Array.Empty<string?>();

		public string? GetCell(int index)
		{
			if (index < 0 || index >= Cells.Count)
			{
				return null;
			}

			return Cells[index];
		}
	}
}
using ClosedXML.Excel;
using CsvHelper;
using NibSphere.Core.Importing;
using System.Globalization;
using System.IO;

namespace NibSphere.Data.Importing
{
	public class ImportFileReaderService : IImportFileReaderService
	{
		public bool CanRead(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				return false;
			}

			string extension = Path.GetExtension(filePath);

			return string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase) ||
				   string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase);
		}

		public ImportFileKind DetectFileKind(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				throw new ArgumentException("File path is required.", nameof(filePath));
			}

			string extension = Path.GetExtension(filePath);

			if (string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
			{
				return ImportFileKind.Csv;
			}

			if (string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
			{
				return ImportFileKind.ExcelXlsx;
			}

			throw new NotSupportedException($"Unsupported import file type: {extension}");
		}

		public async Task<ImportTableDocument> ReadAsync(
			string filePath,
			CancellationToken cancellationToken = default)
		{
			if (!File.Exists(filePath))
			{
				throw new FileNotFoundException("Import file was not found.", filePath);
			}

			return DetectFileKind(filePath) switch
			{
				ImportFileKind.Csv => await ReadCsvAsync(filePath, cancellationToken),
				ImportFileKind.ExcelXlsx => await ReadExcelAsync(filePath, cancellationToken),
				_ => throw new NotSupportedException("Unsupported import file type.")
			};
		}

		private static async Task<ImportTableDocument> ReadCsvAsync(
			string filePath,
			CancellationToken cancellationToken)
		{
			List<ImportTableRow> rows = new();

			using StreamReader streamReader = new StreamReader(filePath);
			using CsvReader csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);

			if (!await csvReader.ReadAsync())
			{
				return new ImportTableDocument
				{
					FilePath = filePath,
					FileKind = ImportFileKind.Csv,
					Sheets = new[]
					{
						new ImportTableSheet
						{
							Name = Path.GetFileNameWithoutExtension(filePath),
							Headers = Array.Empty<string>(),
							Rows = Array.Empty<ImportTableRow>()
						}
					}
				};
			}

			csvReader.ReadHeader();

			string[] rawHeaders = csvReader.HeaderRecord ?? Array.Empty<string>();
			IReadOnlyList<string> headers = NormalizeHeaders(rawHeaders);

			int rowNumber = 2;

			while (await csvReader.ReadAsync())
			{
				cancellationToken.ThrowIfCancellationRequested();

				List<string?> cells = new(headers.Count);

				for (int index = 0; index < headers.Count; index++)
				{
					string? value = csvReader.GetField(index);
					cells.Add(string.IsNullOrWhiteSpace(value) ? null : value.Trim());
				}

				if (cells.All(string.IsNullOrWhiteSpace))
				{
					rowNumber++;
					continue;
				}

				rows.Add(new ImportTableRow
				{
					RowNumber = rowNumber,
					Cells = cells
				});

				rowNumber++;
			}

			return new ImportTableDocument
			{
				FilePath = filePath,
				FileKind = ImportFileKind.Csv,
				Sheets = new[]
				{
					new ImportTableSheet
					{
						Name = Path.GetFileNameWithoutExtension(filePath),
						Headers = headers,
						Rows = rows
					}
				}
			};
		}

		private static Task<ImportTableDocument> ReadExcelAsync(
			string filePath,
			CancellationToken cancellationToken)
		{
			List<ImportTableSheet> sheets = new();

			using XLWorkbook workbook = new XLWorkbook(filePath);

			foreach (IXLWorksheet worksheet in workbook.Worksheets)
			{
				cancellationToken.ThrowIfCancellationRequested();

				IXLRange? usedRange = worksheet.RangeUsed();

				if (usedRange == null)
				{
					sheets.Add(new ImportTableSheet
					{
						Name = worksheet.Name,
						Headers = Array.Empty<string>(),
						Rows = Array.Empty<ImportTableRow>()
					});

					continue;
				}

				IXLRangeRow headerRow = usedRange.FirstRowUsed();

				int firstColumnNumber = usedRange.FirstColumn().ColumnNumber();
				int lastColumnNumber = usedRange.LastColumn().ColumnNumber();
				int lastRowNumber = usedRange.LastRow().RowNumber();

				List<string?> rawHeaders = new();

				for (int columnNumber = firstColumnNumber; columnNumber <= lastColumnNumber; columnNumber++)
				{
					rawHeaders.Add(headerRow.Cell(columnNumber).GetString());
				}

				IReadOnlyList<string> headers = NormalizeHeaders(rawHeaders);

				List<ImportTableRow> rows = new();

				for (int rowNumber = headerRow.RowNumber() + 1; rowNumber <= lastRowNumber; rowNumber++)
				{
					IXLRow row = worksheet.Row(rowNumber);
					List<string?> cells = new(headers.Count);
					bool hasValue = false;

					for (int columnNumber = firstColumnNumber; columnNumber <= lastColumnNumber; columnNumber++)
					{
						string value = row.Cell(columnNumber).GetFormattedString();
						string? normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

						if (!string.IsNullOrWhiteSpace(normalized))
						{
							hasValue = true;
						}

						cells.Add(normalized);
					}

					if (!hasValue)
					{
						continue;
					}

					rows.Add(new ImportTableRow
					{
						RowNumber = rowNumber,
						Cells = cells
					});
				}

				sheets.Add(new ImportTableSheet
				{
					Name = worksheet.Name,
					Headers = headers,
					Rows = rows
				});
			}

			return Task.FromResult(new ImportTableDocument
			{
				FilePath = filePath,
				FileKind = ImportFileKind.ExcelXlsx,
				Sheets = sheets
			});
		}

		private static IReadOnlyList<string> NormalizeHeaders(IEnumerable<string?> rawHeaders)
		{
			List<string> headers = new();
			HashSet<string> used = new(StringComparer.OrdinalIgnoreCase);

			int columnNumber = 1;

			foreach (string? rawHeader in rawHeaders)
			{
				string baseHeader = string.IsNullOrWhiteSpace(rawHeader)
					? $"Column {columnNumber}"
					: rawHeader.Trim();

				string candidate = baseHeader;
				int suffix = 2;

				while (!used.Add(candidate))
				{
					candidate = $"{baseHeader} ({suffix})";
					suffix++;
				}

				headers.Add(candidate);
				columnNumber++;
			}

			return headers;
		}
	}
}
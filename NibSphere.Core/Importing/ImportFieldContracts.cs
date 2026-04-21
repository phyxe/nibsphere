using System.Globalization;

namespace NibSphere.Core.Importing
{
	public enum ImportFileKind
	{
		Csv,
		ExcelXlsx
	}

	public enum ImportFieldDataType
	{
		Text,
		Integer,
		Decimal,
		Boolean,
		Date,
		LookupText
	}

	public sealed class ImportColumnMapping
	{
		public string FieldKey { get; set; } = string.Empty;
		public string? SourceColumnHeader { get; set; }
		public int? SourceColumnIndex { get; set; }
	}

	public sealed class ImportFieldParseContext
	{
		public int SourceRowNumber { get; set; }
		public string FieldKey { get; set; } = string.Empty;
		public string FieldLabel { get; set; } = string.Empty;
		public IReadOnlyDictionary<string, string?> SourceValues { get; set; } =
			new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

		public IDictionary<string, object> Items { get; } =
			new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
	}

	public sealed class ImportFieldParseResult
	{
		public bool IsSuccess { get; private set; }
		public object? Value { get; private set; }
		public string? Message { get; private set; }

		public static ImportFieldParseResult Success(object? value)
		{
			return new ImportFieldParseResult
			{
				IsSuccess = true,
				Value = value
			};
		}

		public static ImportFieldParseResult Fail(string message)
		{
			return new ImportFieldParseResult
			{
				IsSuccess = false,
				Message = message
			};
		}
	}

	public sealed class ImportFieldDefinition<TTarget>
	{
		public string Key { get; set; } = string.Empty;
		public string Label { get; set; } = string.Empty;
		public bool IsRequired { get; set; }
		public ImportFieldDataType DataType { get; set; } = ImportFieldDataType.Text;
		public IReadOnlyList<string> SourceAliases { get; set; } = Array.Empty<string>();

		public Func<string?, ImportFieldParseContext, CancellationToken, Task<ImportFieldParseResult>>? ParseAsync { get; set; }

		public Action<TTarget, object?> AssignValue { get; set; } = (_, _) => { };

		public Task<ImportFieldParseResult> ParseValueAsync(
			string? rawValue,
			ImportFieldParseContext context,
			CancellationToken cancellationToken = default)
		{
			if (ParseAsync != null)
			{
				return ParseAsync(rawValue, context, cancellationToken);
			}

			return Task.FromResult(ParseDefault(rawValue));
		}

		private ImportFieldParseResult ParseDefault(string? rawValue)
		{
			string? trimmed = string.IsNullOrWhiteSpace(rawValue) ? null : rawValue.Trim();

			if (trimmed == null)
			{
				return IsRequired
					? ImportFieldParseResult.Fail($"{Label} is required.")
					: ImportFieldParseResult.Success(null);
			}

			switch (DataType)
			{
				case ImportFieldDataType.Text:
				case ImportFieldDataType.LookupText:
					return ImportFieldParseResult.Success(trimmed);

				case ImportFieldDataType.Integer:
					if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
					{
						return ImportFieldParseResult.Success(intValue);
					}
					return ImportFieldParseResult.Fail($"{Label} must be a whole number.");

				case ImportFieldDataType.Decimal:
					if (decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decimalValue))
					{
						return ImportFieldParseResult.Success(decimalValue);
					}
					return ImportFieldParseResult.Fail($"{Label} must be a valid number.");

				case ImportFieldDataType.Boolean:
					if (TryParseBoolean(trimmed, out bool boolValue))
					{
						return ImportFieldParseResult.Success(boolValue);
					}
					return ImportFieldParseResult.Fail($"{Label} must be a valid yes/no or true/false value.");

				case ImportFieldDataType.Date:
					if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateValue) ||
						DateTime.TryParse(trimmed, out dateValue))
					{
						return ImportFieldParseResult.Success(dateValue);
					}
					return ImportFieldParseResult.Fail($"{Label} must be a valid date.");

				default:
					return ImportFieldParseResult.Success(trimmed);
			}
		}

		private static bool TryParseBoolean(string value, out bool result)
		{
			switch (value.Trim().ToLowerInvariant())
			{
				case "true":
				case "t":
				case "yes":
				case "y":
				case "1":
					result = true;
					return true;

				case "false":
				case "f":
				case "no":
				case "n":
				case "0":
					result = false;
					return true;

				default:
					result = false;
					return false;
			}
		}
	}
}
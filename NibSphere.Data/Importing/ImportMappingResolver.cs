using NibSphere.Core.Importing;

namespace NibSphere.Data.Importing
{
	public static class ImportMappingResolver
	{
		public static List<ImportColumnMapping> SuggestMappings<TTarget>(
			ImportDefinition<TTarget> definition,
			ImportTableSheet sheet)
		{
			List<ImportColumnMapping> mappings = new();

			for (int fieldIndex = 0; fieldIndex < definition.Fields.Count; fieldIndex++)
			{
				ImportFieldDefinition<TTarget> field = definition.Fields[fieldIndex];

				int? matchedIndex = FindMatchingHeaderIndex(field, sheet.Headers);

				mappings.Add(new ImportColumnMapping
				{
					FieldKey = field.Key,
					SourceColumnIndex = matchedIndex,
					SourceColumnHeader = matchedIndex.HasValue
						? sheet.Headers[matchedIndex.Value]
						: null
				});
			}

			return mappings;
		}

		private static int? FindMatchingHeaderIndex<TTarget>(
			ImportFieldDefinition<TTarget> field,
			IReadOnlyList<string> headers)
		{
			List<string> candidates = new();

			if (!string.IsNullOrWhiteSpace(field.Label))
			{
				candidates.Add(field.Label);
			}

			if (!string.IsNullOrWhiteSpace(field.Key))
			{
				candidates.Add(field.Key);
			}

			if (field.SourceAliases.Count > 0)
			{
				candidates.AddRange(field.SourceAliases.Where(x => !string.IsNullOrWhiteSpace(x)));
			}

			for (int headerIndex = 0; headerIndex < headers.Count; headerIndex++)
			{
				string header = headers[headerIndex];

				if (candidates.Any(candidate =>
					string.Equals(candidate, header, StringComparison.OrdinalIgnoreCase)))
				{
					return headerIndex;
				}
			}

			return null;
		}
	}
}
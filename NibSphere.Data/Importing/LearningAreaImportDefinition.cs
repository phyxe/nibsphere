using NibSphere.Core.Importing;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Models;
using NibSphere.Data.Repositories;

namespace NibSphere.Data.Importing
{
	public sealed class LearningAreaImportDefinition : ImportDefinition<LearningArea>
	{
		private const string AcademicGroupsByNameKey = "AcademicGroupsByName";
		private const string CategoriesByNameKey = "CategoriesByName";
		private const string ExistingLearningAreaCodesKey = "ExistingLearningAreaCodes";

		private readonly LearningAreaRepository _learningAreaRepository;
		private readonly AcademicGroupRepository _academicGroupRepository;
		private readonly LearningAreaCategoryRepository _learningAreaCategoryRepository;

		public LearningAreaImportDefinition(IAppPaths appPaths)
		{
			_learningAreaRepository = new LearningAreaRepository(appPaths);
			_academicGroupRepository = new AcademicGroupRepository(appPaths);
			_learningAreaCategoryRepository = new LearningAreaCategoryRepository(appPaths);

			ModuleKey = "learning-areas";
			Title = "Import Learning Areas";

			Fields = new List<ImportFieldDefinition<LearningArea>>
			{
				new()
				{
					Key = "code",
					Label = "Code",
					IsRequired = true,
					DataType = ImportFieldDataType.Text,
					SourceAliases = new[]
					{
						"Code",
						"Learning Area Code",
						"LearningAreaCode"
					},
					AssignValue = static (item, value) =>
					{
						item.Code = value as string ?? string.Empty;
					}
				},
				new()
				{
					Key = "short-name",
					Label = "Short Name",
					IsRequired = true,
					DataType = ImportFieldDataType.Text,
					SourceAliases = new[]
					{
						"Short Name",
						"ShortName",
						"Abbreviation"
					},
					AssignValue = static (item, value) =>
					{
						item.ShortName = value as string ?? string.Empty;
					}
				},
				new()
				{
					Key = "description",
					Label = "Description",
					IsRequired = true,
					DataType = ImportFieldDataType.Text,
					SourceAliases = new[]
					{
						"Description",
						"Learning Area",
						"Learning Area Description"
					},
					AssignValue = static (item, value) =>
					{
						item.Description = value as string ?? string.Empty;
					}
				},
				new()
				{
					Key = "academic-group",
					Label = "Academic Group",
					IsRequired = true,
					DataType = ImportFieldDataType.LookupText,
					SourceAliases = new[]
					{
						"Academic Group",
						"AcademicGroup",
						"Group"
					},
					ParseAsync = static (rawValue, context, _) =>
					{
						string? normalized = Normalize(rawValue);

						if (normalized == null)
						{
							return Task.FromResult(ImportFieldParseResult.Fail("Academic Group is required."));
						}

						if (!context.Items.TryGetValue(AcademicGroupsByNameKey, out object? groupsObject) ||
							groupsObject is not Dictionary<string, AcademicGroup> groupsByName)
						{
							return Task.FromResult(ImportFieldParseResult.Fail("Academic group lookup data is not available."));
						}

						if (!groupsByName.TryGetValue(normalized, out AcademicGroup? group))
						{
							return Task.FromResult(ImportFieldParseResult.Fail($"Academic Group '{normalized}' was not found."));
						}

						return Task.FromResult(ImportFieldParseResult.Success(group));
					},
					AssignValue = static (item, value) =>
					{
						if (value is AcademicGroup group)
						{
							item.AcademicGroupId = group.Id;
							item.AcademicGroupName = group.Name;
						}
					}
				},
				new()
				{
					Key = "category",
					Label = "Category",
					IsRequired = true,
					DataType = ImportFieldDataType.LookupText,
					SourceAliases = new[]
					{
						"Category",
						"Learning Area Category",
						"LearningAreaCategory"
					},
					ParseAsync = static (rawValue, context, _) =>
					{
						string? normalized = Normalize(rawValue);

						if (normalized == null)
						{
							return Task.FromResult(ImportFieldParseResult.Fail("Category is required."));
						}

						if (!context.Items.TryGetValue(CategoriesByNameKey, out object? categoriesObject) ||
							categoriesObject is not Dictionary<string, LearningAreaCategory> categoriesByName)
						{
							return Task.FromResult(ImportFieldParseResult.Fail("Category lookup data is not available."));
						}

						if (!categoriesByName.TryGetValue(normalized, out LearningAreaCategory? category))
						{
							return Task.FromResult(ImportFieldParseResult.Fail($"Category '{normalized}' was not found."));
						}

						return Task.FromResult(ImportFieldParseResult.Success(category));
					},
					AssignValue = static (item, value) =>
					{
						if (value is LearningAreaCategory category)
						{
							item.CategoryId = category.Id;
							item.CategoryName = category.Name;

							// Keep legacy text field populated.
							item.Category = category.Name;
						}
					}
				},
				new()
				{
					Key = "sort",
					Label = "Sort",
					IsRequired = false,
					DataType = ImportFieldDataType.Integer,
					SourceAliases = new[]
					{
						"Sort",
						"Display Order",
						"Order"
					},
					AssignValue = static (item, value) =>
					{
						item.Sort = value is int sort ? sort : 0;
					}
				}
			};
		}

		public override async Task PrepareAsync(
			ImportSimulationRequest request,
			CancellationToken cancellationToken = default)
		{
			List<AcademicGroup> academicGroups = await _academicGroupRepository.GetAllAsync();
			List<LearningAreaCategory> categories = await _learningAreaCategoryRepository.GetAllAsync();
			List<LearningArea> learningAreas = await _learningAreaRepository.GetAllAsync();

			request.Items[AcademicGroupsByNameKey] = academicGroups
				.GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
				.ToDictionary(
					x => x.Key,
					x => x.First(),
					StringComparer.OrdinalIgnoreCase);

			request.Items[CategoriesByNameKey] = categories
				.GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
				.ToDictionary(
					x => x.Key,
					x => x.First(),
					StringComparer.OrdinalIgnoreCase);

			request.Items[ExistingLearningAreaCodesKey] = new HashSet<string>(
				learningAreas
					.Select(x => x.Code.Trim())
					.Where(x => !string.IsNullOrWhiteSpace(x)),
				StringComparer.OrdinalIgnoreCase);
		}

		public override async Task<ImportSimulationResult<LearningArea>> SimulateAsync(
			ImportSimulationRequest request,
			CancellationToken cancellationToken = default)
		{
			await EnsurePreparedAsync(request, cancellationToken);

			List<ImportPreviewRow<LearningArea>> previewRows = new();
			HashSet<string> importCodes = new(StringComparer.OrdinalIgnoreCase);

			foreach (ImportTableRow sourceRow in request.Sheet.Rows)
			{
				cancellationToken.ThrowIfCancellationRequested();

				Dictionary<string, string?> sourceValues = BuildSourceValueDictionary(request, sourceRow);
				List<ImportSimulationIssue> issues = new();
				LearningArea item = new();

				foreach (ImportFieldDefinition<LearningArea> field in Fields)
				{
					string? rawValue = sourceValues.TryGetValue(field.Key, out string? value)
						? value
						: null;

					ImportFieldParseContext context = BuildParseContext(
						request,
						sourceRow.RowNumber,
						field,
						sourceValues);

					ImportFieldParseResult parseResult =
						await field.ParseValueAsync(rawValue, context, cancellationToken);

					if (!parseResult.IsSuccess)
					{
						issues.Add(new ImportSimulationIssue
						{
							Severity = ImportSimulationIssueSeverity.Error,
							Message = parseResult.Message ?? $"{field.Label} is invalid."
						});

						continue;
					}

					field.AssignValue(item, parseResult.Value);
				}

				ValidateLearningAreaItem(item, importCodes, request, issues);

				previewRows.Add(new ImportPreviewRow<LearningArea>
				{
					SourceRowNumber = sourceRow.RowNumber,
					Item = item,
					CanPost = issues.All(x => x.Severity != ImportSimulationIssueSeverity.Error),
					SourceValues = sourceValues,
					Issues = issues
				});
			}

			return new ImportSimulationResult<LearningArea>
			{
				PreviewRows = previewRows
			};
		}

		public override async Task<ImportFinalizeResult> FinalizeAsync(
			ImportSimulationResult<LearningArea> simulationResult,
			CancellationToken cancellationToken = default)
		{
			int createdCount = 0;
			int skippedCount = 0;

			foreach (ImportPreviewRow<LearningArea> previewRow in simulationResult.PreviewRows)
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (!previewRow.CanPost || previewRow.Item == null)
				{
					skippedCount++;
					continue;
				}

				await _learningAreaRepository.InsertAsync(previewRow.Item);
				createdCount++;
			}

			return new ImportFinalizeResult
			{
				CreatedCount = createdCount,
				UpdatedCount = 0,
				SkippedCount = skippedCount,
				Messages = new[]
				{
					$"{createdCount} learning area record(s) created.",
					$"{skippedCount} row(s) skipped."
				}
			};
		}

		private async Task EnsurePreparedAsync(
			ImportSimulationRequest request,
			CancellationToken cancellationToken)
		{
			if (!request.Items.ContainsKey(AcademicGroupsByNameKey) ||
				!request.Items.ContainsKey(CategoriesByNameKey) ||
				!request.Items.ContainsKey(ExistingLearningAreaCodesKey))
			{
				await PrepareAsync(request, cancellationToken);
			}
		}

		private static Dictionary<string, string?> BuildSourceValueDictionary(
			ImportSimulationRequest request,
			ImportTableRow sourceRow)
		{
			Dictionary<string, string?> sourceValues = new(StringComparer.OrdinalIgnoreCase);

			foreach (ImportColumnMapping mapping in request.ColumnMappings)
			{
				if (string.IsNullOrWhiteSpace(mapping.FieldKey))
				{
					continue;
				}

				int? sourceIndex = mapping.SourceColumnIndex;

				if (!sourceIndex.HasValue &&
					!string.IsNullOrWhiteSpace(mapping.SourceColumnHeader))
				{
					sourceIndex = FindHeaderIndex(request.Sheet.Headers, mapping.SourceColumnHeader);
				}

				sourceValues[mapping.FieldKey] = sourceIndex.HasValue
					? sourceRow.GetCell(sourceIndex.Value)
					: null;
			}

			return sourceValues;
		}

		private static ImportFieldParseContext BuildParseContext(
			ImportSimulationRequest request,
			int rowNumber,
			ImportFieldDefinition<LearningArea> field,
			IReadOnlyDictionary<string, string?> sourceValues)
		{
			ImportFieldParseContext context = new()
			{
				SourceRowNumber = rowNumber,
				FieldKey = field.Key,
				FieldLabel = field.Label,
				SourceValues = sourceValues
			};

			foreach (KeyValuePair<string, object> item in request.Items)
			{
				context.Items[item.Key] = item.Value;
			}

			return context;
		}

		private static void ValidateLearningAreaItem(
			LearningArea item,
			HashSet<string> importCodes,
			ImportSimulationRequest request,
			List<ImportSimulationIssue> issues)
		{
			if (string.IsNullOrWhiteSpace(item.Code))
			{
				return;
			}

			if (request.Items.TryGetValue(ExistingLearningAreaCodesKey, out object? existingCodesObject) &&
				existingCodesObject is HashSet<string> existingCodes &&
				existingCodes.Contains(item.Code))
			{
				issues.Add(new ImportSimulationIssue
				{
					Severity = ImportSimulationIssueSeverity.Error,
					Message = $"Code '{item.Code}' already exists."
				});
			}

			if (!importCodes.Add(item.Code))
			{
				issues.Add(new ImportSimulationIssue
				{
					Severity = ImportSimulationIssueSeverity.Error,
					Message = $"Code '{item.Code}' is duplicated within the import file."
				});
			}
		}

		private static int? FindHeaderIndex(
			IReadOnlyList<string> headers,
			string header)
		{
			for (int index = 0; index < headers.Count; index++)
			{
				if (string.Equals(headers[index], header, StringComparison.OrdinalIgnoreCase))
				{
					return index;
				}
			}

			return null;
		}

		private static string? Normalize(string? value)
		{
			return string.IsNullOrWhiteSpace(value)
				? null
				: value.Trim();
		}
	}
}
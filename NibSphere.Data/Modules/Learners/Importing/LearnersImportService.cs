using NibSphere.Core.Importing;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Learners.Importing;
using NibSphere.Core.Modules.Learners.Models;
using NibSphere.Core.Modules.Learners.Profile;
using NibSphere.Data.Importing;
using NibSphere.Data.Modules.Learners.Profile;
using NibSphere.Data.Modules.Learners.Repositories;
using System.Globalization;

namespace NibSphere.Data.Modules.Learners.Importing
{
	public sealed class LearnersImportService
	{
		private readonly ImportFileReaderService _fileReaderService;
		private readonly LearnerRepository _learnerRepository;
		private readonly CustodianRoleRepository _custodianRoleRepository;
		private readonly LearnerProfileService _learnerProfileService;

		public LearnersImportService(IAppPaths appPaths)
		{
			_fileReaderService = new ImportFileReaderService();
			_learnerRepository = new LearnerRepository(appPaths);
			_custodianRoleRepository = new CustodianRoleRepository(appPaths);
			_learnerProfileService = new LearnerProfileService(appPaths);
		}

		public bool CanRead(string filePath)
		{
			return _fileReaderService.CanRead(filePath);
		}

		public Task<ImportTableDocument> ReadAsync(
			string filePath,
			CancellationToken cancellationToken = default)
		{
			return _fileReaderService.ReadAsync(filePath, cancellationToken);
		}

		public ImportTableSheet ResolveSheet(
			ImportTableDocument document,
			string? sheetName = null)
		{
			ArgumentNullException.ThrowIfNull(document);
			return document.GetRequiredSheet(sheetName);
		}

		public async Task<LearnersImportPreview> BuildPreviewAsync(
			string filePath,
			string? sheetName,
			LearnersImportColumnMap learnerColumnMap,
			IReadOnlyList<LearnersImportCustodianColumnMap>? custodianColumnMaps = null,
			CancellationToken cancellationToken = default)
		{
			ImportTableDocument document = await _fileReaderService.ReadAsync(filePath, cancellationToken);
			return await BuildPreviewAsync(
				document,
				sheetName,
				learnerColumnMap,
				custodianColumnMaps,
				cancellationToken);
		}

		public async Task<LearnersImportPreview> BuildPreviewAsync(
			ImportTableDocument document,
			string? sheetName,
			LearnersImportColumnMap learnerColumnMap,
			IReadOnlyList<LearnersImportCustodianColumnMap>? custodianColumnMaps = null,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(document);
			ArgumentNullException.ThrowIfNull(learnerColumnMap);

			ImportTableSheet sheet = document.GetRequiredSheet(sheetName);

			List<LearnersImportCustodianColumnMap> normalizedCustodianMaps =
				NormalizeCustodianColumnMaps(custodianColumnMaps);

			ValidateRequiredMappings(learnerColumnMap);
			EnsureMappedHeadersExist(sheet, learnerColumnMap, normalizedCustodianMaps);

			HashSet<string> validRolePairs = await LoadValidRolePairsAsync(cancellationToken);
			Dictionary<string, int> headerIndexes = BuildHeaderIndexMap(sheet);
			HashSet<string> seenImportDuplicateKeys = new(StringComparer.OrdinalIgnoreCase);

			List<LearnersImportPreviewRow> previewRows = new();

			foreach (ImportTableRow sourceRow in sheet.Rows)
			{
				cancellationToken.ThrowIfCancellationRequested();

				List<string> errors = new();
				List<string> warnings = new();
				int skippedCustodianBlockCount = 0;

				LearnersImportRowData rowData = BuildRowData(
					sourceRow,
					headerIndexes,
					learnerColumnMap,
					normalizedCustodianMaps,
					validRolePairs,
					warnings,
					ref skippedCustodianBlockCount);

				ValidateLearner(rowData.Learner, errors);

				string? duplicateKey = BuildLearnerDuplicateKey(rowData.Learner);

				if (!string.IsNullOrWhiteSpace(duplicateKey) &&
					!seenImportDuplicateKeys.Add(duplicateKey))
				{
					errors.Add("Duplicate learner row found within the import file.");
				}

				List<Learner> potentialDuplicateLearners =
					await FindPotentialDuplicateLearnersAsync(rowData.Learner);

				bool hasPotentialDuplicate = potentialDuplicateLearners.Count > 0;

				if (hasPotentialDuplicate)
				{
					errors.Add("Potential duplicate learner found in existing records. Row will be skipped.");
				}

				previewRows.Add(new LearnersImportPreviewRow
				{
					SourceRowNumber = sourceRow.RowNumber,
					RowData = rowData,
					CanImport = errors.Count == 0,
					HasPotentialDuplicate = hasPotentialDuplicate,
					SkippedCustodianBlockCount = skippedCustodianBlockCount,
					PotentialDuplicateLearners = potentialDuplicateLearners,
					Errors = errors,
					Warnings = warnings
				});
			}

			return new LearnersImportPreview
			{
				Document = document,
				Sheet = sheet,
				Rows = previewRows
			};
		}

		public async Task<LearnersImportResult> ImportAsync(
			LearnersImportPreview preview,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(preview);

			int importedLearnerCount = 0;
			int importedCustodianCount = 0;
			int skippedRowCount = 0;
			int duplicateSkippedCount = 0;

			foreach (LearnersImportPreviewRow row in preview.Rows)
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (!row.CanImport || row.RowData == null)
				{
					skippedRowCount++;

					if (row.HasPotentialDuplicate)
					{
						duplicateSkippedCount++;
					}

					continue;
				}

				LearnerProfileRecord profile = row.RowData.ToProfileRecord();

				await _learnerProfileService.SaveAsync(profile, cancellationToken);

				importedLearnerCount++;
				importedCustodianCount += profile.Custodians.Count;
			}

			return new LearnersImportResult
			{
				ImportedLearnerCount = importedLearnerCount,
				ImportedCustodianCount = importedCustodianCount,
				SkippedRowCount = skippedRowCount,
				DuplicateSkippedCount = duplicateSkippedCount,
				SkippedCustodianBlockCount = preview.SkippedCustodianBlockCount,
				Messages = new[]
				{
					$"{importedLearnerCount} learner record(s) imported.",
					$"{importedCustodianCount} custodian record(s) linked/imported.",
					$"{skippedRowCount} row(s) skipped.",
					$"{duplicateSkippedCount} skipped row(s) were flagged as potential duplicates.",
					$"{preview.SkippedCustodianBlockCount} custodian block(s) were skipped during preview."
				}
			};
		}

		private static List<LearnersImportCustodianColumnMap> NormalizeCustodianColumnMaps(
			IReadOnlyList<LearnersImportCustodianColumnMap>? custodianColumnMaps)
		{
			if (custodianColumnMaps == null || custodianColumnMaps.Count == 0)
			{
				return new List<LearnersImportCustodianColumnMap>();
			}

			return custodianColumnMaps
				.Where(x => x != null)
				.Where(x => x.BlockNumber > 0)
				.Where(x => x.HasAnyMappedColumn())
				.OrderBy(x => x.BlockNumber)
				.Take(3)
				.ToList();
		}

		private static void ValidateRequiredMappings(LearnersImportColumnMap learnerColumnMap)
		{
			if (string.IsNullOrWhiteSpace(learnerColumnMap.FirstNameColumnHeader))
			{
				throw new InvalidOperationException("A First Name column must be mapped.");
			}

			if (string.IsNullOrWhiteSpace(learnerColumnMap.LastNameColumnHeader))
			{
				throw new InvalidOperationException("A Last Name column must be mapped.");
			}
		}

		private static void EnsureMappedHeadersExist(
			ImportTableSheet sheet,
			LearnersImportColumnMap learnerColumnMap,
			IReadOnlyList<LearnersImportCustodianColumnMap> custodianColumnMaps)
		{
			HashSet<string> headers = new(sheet.Headers, StringComparer.OrdinalIgnoreCase);

			foreach (string header in GetAllMappedHeaders(learnerColumnMap, custodianColumnMaps))
			{
				if (!headers.Contains(header))
				{
					throw new InvalidOperationException(
						$"Mapped header '{header}' was not found in sheet '{sheet.Name}'.");
				}
			}
		}

		private static IEnumerable<string> GetAllMappedHeaders(
			LearnersImportColumnMap learnerColumnMap,
			IReadOnlyList<LearnersImportCustodianColumnMap> custodianColumnMaps)
		{
			foreach (string? header in new[]
			{
				learnerColumnMap.LrnColumnHeader,
				learnerColumnMap.LastNameColumnHeader,
				learnerColumnMap.FirstNameColumnHeader,
				learnerColumnMap.MiddleNameColumnHeader,
				learnerColumnMap.ExtensionNameColumnHeader,
				learnerColumnMap.BirthdayColumnHeader,
				learnerColumnMap.SexColumnHeader,
				learnerColumnMap.PronounColumnHeader,
				learnerColumnMap.ReligiousAffiliationColumnHeader,
				learnerColumnMap.HouseStreetSitioPurokColumnHeader,
				learnerColumnMap.BarangayColumnHeader,
				learnerColumnMap.MunicipalityColumnHeader,
				learnerColumnMap.ProvinceColumnHeader,
				learnerColumnMap.MobileNumberColumnHeader,
				learnerColumnMap.EmailColumnHeader
			})
			{
				if (!string.IsNullOrWhiteSpace(header))
				{
					yield return header.Trim();
				}
			}

			foreach (LearnersImportCustodianColumnMap map in custodianColumnMaps)
			{
				foreach (string? header in new[]
				{
					map.FirstNameColumnHeader,
					map.MiddleNameColumnHeader,
					map.LastNameColumnHeader,
					map.ExtensionNameColumnHeader,
					map.MobileNumberColumnHeader,
					map.EmailColumnHeader,
					map.RelationshipTypeColumnHeader,
					map.RelationshipLabelColumnHeader,
					map.HasCustodyColumnHeader,
					map.LivesWithLearnerColumnHeader
				})
				{
					if (!string.IsNullOrWhiteSpace(header))
					{
						yield return header.Trim();
					}
				}
			}
		}

		private static Dictionary<string, int> BuildHeaderIndexMap(ImportTableSheet sheet)
		{
			Dictionary<string, int> indexes = new(StringComparer.OrdinalIgnoreCase);

			for (int index = 0; index < sheet.Headers.Count; index++)
			{
				string header = sheet.Headers[index];

				if (!indexes.ContainsKey(header))
				{
					indexes[header] = index;
				}
			}

			return indexes;
		}

		private LearnersImportRowData BuildRowData(
			ImportTableRow sourceRow,
			IReadOnlyDictionary<string, int> headerIndexes,
			LearnersImportColumnMap learnerColumnMap,
			IReadOnlyList<LearnersImportCustodianColumnMap> custodianColumnMaps,
			IReadOnlySet<string> validRolePairs,
			List<string> warnings,
			ref int skippedCustodianBlockCount)
		{
			List<string> learnerFieldErrors = new();

			Learner learner = BuildLearner(
				sourceRow,
				headerIndexes,
				learnerColumnMap,
				learnerFieldErrors);

			if (learnerFieldErrors.Count > 0)
			{
				warnings.AddRange(learnerFieldErrors.Select(x => $"Learner field warning: {x}"));
			}

			List<LearnersImportCustodianInput> custodians = new();

			foreach (LearnersImportCustodianColumnMap custodianMap in custodianColumnMaps)
			{
				if (!TryBuildCustodianInput(
					sourceRow,
					headerIndexes,
					custodianMap,
					validRolePairs,
					out LearnersImportCustodianInput? input,
					out string? warningMessage))
				{
					if (!string.IsNullOrWhiteSpace(warningMessage))
					{
						warnings.Add(warningMessage);
						skippedCustodianBlockCount++;
					}

					continue;
				}

				if (input != null)
				{
					custodians.Add(input);
				}
			}

			return new LearnersImportRowData
			{
				Learner = learner,
				Custodians = custodians
			};
		}

		private Learner BuildLearner(
			ImportTableRow sourceRow,
			IReadOnlyDictionary<string, int> headerIndexes,
			LearnersImportColumnMap learnerColumnMap,
			List<string> fieldErrors)
		{
			Learner learner = new()
			{
				Lrn = GetCellValue(sourceRow, headerIndexes, learnerColumnMap.LrnColumnHeader),
				LastName = GetCellValue(sourceRow, headerIndexes, learnerColumnMap.LastNameColumnHeader),
				FirstName = GetCellValue(sourceRow, headerIndexes, learnerColumnMap.FirstNameColumnHeader),
				MiddleName = GetCellValue(sourceRow, headerIndexes, learnerColumnMap.MiddleNameColumnHeader),
				ExtensionName = GetCellValue(sourceRow, headerIndexes, learnerColumnMap.ExtensionNameColumnHeader),
				Sex = GetCellValue(sourceRow, headerIndexes, learnerColumnMap.SexColumnHeader),
				Pronoun = GetCellValue(sourceRow, headerIndexes, learnerColumnMap.PronounColumnHeader),
				ReligiousAffiliation = GetCellValue(sourceRow, headerIndexes, learnerColumnMap.ReligiousAffiliationColumnHeader),
				HouseStreetSitioPurok = GetCellValue(sourceRow, headerIndexes, learnerColumnMap.HouseStreetSitioPurokColumnHeader),
				Barangay = GetCellValue(sourceRow, headerIndexes, learnerColumnMap.BarangayColumnHeader),
				Municipality = GetCellValue(sourceRow, headerIndexes, learnerColumnMap.MunicipalityColumnHeader),
				Province = GetCellValue(sourceRow, headerIndexes, learnerColumnMap.ProvinceColumnHeader),
				MobileNumber = GetCellValue(sourceRow, headerIndexes, learnerColumnMap.MobileNumberColumnHeader),
				Email = GetCellValue(sourceRow, headerIndexes, learnerColumnMap.EmailColumnHeader)
			};

			string? birthdayText = GetCellValue(
				sourceRow,
				headerIndexes,
				learnerColumnMap.BirthdayColumnHeader);

			if (!string.IsNullOrWhiteSpace(birthdayText))
			{
				if (TryParseDate(birthdayText, out DateTime birthday))
				{
					learner.Birthday = birthday.Date;
				}
				else
				{
					fieldErrors.Add("Birthday must be a valid date.");
				}
			}

			return learner;
		}

		private bool TryBuildCustodianInput(
			ImportTableRow sourceRow,
			IReadOnlyDictionary<string, int> headerIndexes,
			LearnersImportCustodianColumnMap custodianMap,
			IReadOnlySet<string> validRolePairs,
			out LearnersImportCustodianInput? input,
			out string? warningMessage)
		{
			input = new LearnersImportCustodianInput
			{
				BlockNumber = custodianMap.BlockNumber,
				Custodian = new Custodian
				{
					FirstName = GetCellValue(sourceRow, headerIndexes, custodianMap.FirstNameColumnHeader),
					MiddleName = GetCellValue(sourceRow, headerIndexes, custodianMap.MiddleNameColumnHeader),
					LastName = GetCellValue(sourceRow, headerIndexes, custodianMap.LastNameColumnHeader),
					ExtensionName = GetCellValue(sourceRow, headerIndexes, custodianMap.ExtensionNameColumnHeader),
					MobileNumber = GetCellValue(sourceRow, headerIndexes, custodianMap.MobileNumberColumnHeader),
					Email = GetCellValue(sourceRow, headerIndexes, custodianMap.EmailColumnHeader)
				},
				RelationshipType = GetCellValue(sourceRow, headerIndexes, custodianMap.RelationshipTypeColumnHeader),
				RelationshipLabel = GetCellValue(sourceRow, headerIndexes, custodianMap.RelationshipLabelColumnHeader)
			};

			string? hasCustodyText = GetCellValue(
				sourceRow,
				headerIndexes,
				custodianMap.HasCustodyColumnHeader);

			if (!TryParseOptionalBoolean(hasCustodyText, out bool hasCustody, out string? hasCustodyError))
			{
				warningMessage = $"Custodian block {custodianMap.BlockNumber} skipped. {hasCustodyError}";
				input = null;
				return false;
			}

			string? livesWithLearnerText = GetCellValue(
				sourceRow,
				headerIndexes,
				custodianMap.LivesWithLearnerColumnHeader);

			if (!TryParseOptionalBoolean(livesWithLearnerText, out bool livesWithLearner, out string? livesWithLearnerError))
			{
				warningMessage = $"Custodian block {custodianMap.BlockNumber} skipped. {livesWithLearnerError}";
				input = null;
				return false;
			}

			input.HasCustody = hasCustody;
			input.LivesWithLearner = livesWithLearner;

			if (!input.HasAnyData())
			{
				input = null;
				warningMessage = null;
				return false;
			}

			if (string.IsNullOrWhiteSpace(input.Custodian.FirstName))
			{
				warningMessage = $"Custodian block {custodianMap.BlockNumber} skipped. First Name is required when custodian data exists.";
				input = null;
				return false;
			}

			if (string.IsNullOrWhiteSpace(input.Custodian.LastName))
			{
				warningMessage = $"Custodian block {custodianMap.BlockNumber} skipped. Last Name is required when custodian data exists.";
				input = null;
				return false;
			}

			if (string.IsNullOrWhiteSpace(input.RelationshipType))
			{
				warningMessage = $"Custodian block {custodianMap.BlockNumber} skipped. Relationship Type is required when custodian data exists.";
				input = null;
				return false;
			}

			if (string.IsNullOrWhiteSpace(input.RelationshipLabel))
			{
				warningMessage = $"Custodian block {custodianMap.BlockNumber} skipped. Relationship Label is required when custodian data exists.";
				input = null;
				return false;
			}

			string roleKey = BuildRolePairKey(input.RelationshipType, input.RelationshipLabel);

			if (validRolePairs.Count > 0 && !validRolePairs.Contains(roleKey))
			{
				warningMessage =
					$"Custodian block {custodianMap.BlockNumber} skipped. Relationship pair '{input.RelationshipType} / {input.RelationshipLabel}' was not found in learner custodian roles.";
				input = null;
				return false;
			}

			warningMessage = null;
			return true;
		}

		private static void ValidateLearner(
			Learner learner,
			List<string> errors)
		{
			if (string.IsNullOrWhiteSpace(learner.FirstName))
			{
				errors.Add("Learner First Name is required.");
			}

			if (string.IsNullOrWhiteSpace(learner.LastName))
			{
				errors.Add("Learner Last Name is required.");
			}
		}

		private async Task<HashSet<string>> LoadValidRolePairsAsync(
			CancellationToken cancellationToken)
		{
			List<CustodianRole> roles = await _custodianRoleRepository.GetAllAsync();

			return roles
				.Select(x => BuildRolePairKey(x.RelationshipType, x.RelationshipLabel))
				.ToHashSet(StringComparer.OrdinalIgnoreCase);
		}

		private async Task<List<Learner>> FindPotentialDuplicateLearnersAsync(
			Learner learner)
		{
			if (string.IsNullOrWhiteSpace(learner.FirstName) ||
				string.IsNullOrWhiteSpace(learner.LastName))
			{
				return new List<Learner>();
			}

			return await _learnerRepository.FindPotentialMatchesAsync(
				learner.Lrn,
				learner.LastName,
				learner.FirstName,
				learner.MiddleName);
		}

		private static string? BuildLearnerDuplicateKey(Learner learner)
		{
			string? lrn = Normalize(learner.Lrn);

			if (!string.IsNullOrWhiteSpace(lrn))
			{
				return $"lrn:{lrn}";
			}

			string? lastName = Normalize(learner.LastName);
			string? firstName = Normalize(learner.FirstName);
			string? middleName = Normalize(learner.MiddleName) ?? string.Empty;

			if (string.IsNullOrWhiteSpace(lastName) ||
				string.IsNullOrWhiteSpace(firstName))
			{
				return null;
			}

			return $"name:{lastName}|{firstName}|{middleName}";
		}

		private static string BuildRolePairKey(
			string relationshipType,
			string relationshipLabel)
		{
			return $"{Normalize(relationshipType)}|{Normalize(relationshipLabel)}";
		}

		private static string GetCellValue(
			ImportTableRow row,
			IReadOnlyDictionary<string, int> headerIndexes,
			string? header)
		{
			if (string.IsNullOrWhiteSpace(header))
			{
				return string.Empty;
			}

			if (!headerIndexes.TryGetValue(header.Trim(), out int index))
			{
				return string.Empty;
			}

			return Normalize(row.GetCell(index)) ?? string.Empty;
		}

		private static bool TryParseDate(
			string value,
			out DateTime date)
		{
			return
				DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date) ||
				DateTime.TryParse(value, out date);
		}

		private static bool TryParseOptionalBoolean(
			string? rawValue,
			out bool value,
			out string? errorMessage)
		{
			value = false;
			errorMessage = null;

			if (string.IsNullOrWhiteSpace(rawValue))
			{
				return true;
			}

			switch (rawValue.Trim().ToLowerInvariant())
			{
				case "true":
				case "t":
				case "yes":
				case "y":
				case "1":
					value = true;
					return true;

				case "false":
				case "f":
				case "no":
				case "n":
				case "0":
					value = false;
					return true;

				default:
					errorMessage = "Boolean fields must use yes/no, true/false, or 1/0 values.";
					return false;
			}
		}

		private static string? Normalize(string? value)
		{
			return string.IsNullOrWhiteSpace(value)
				? null
				: value.Trim();
		}
	}
}
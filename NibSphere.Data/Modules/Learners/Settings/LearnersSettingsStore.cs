using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Learners.Settings;
using System.IO;
using System.Text.Json;

namespace NibSphere.Data.Modules.Learners.Settings
{
	public sealed class LearnersSettingsStore
	{
		private const string ModuleFolderName = "Learners";
		private const string SettingsFileName = "learners.settings.json";

		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			WriteIndented = true,
			PropertyNameCaseInsensitive = true
		};

		private readonly IAppPaths _appPaths;

		public LearnersSettingsStore(IAppPaths appPaths)
		{
			_appPaths = appPaths;
		}

		public async Task<LearnersSettings> GetAsync(CancellationToken cancellationToken = default)
		{
			string settingsFilePath = GetSettingsFilePath();

			if (!File.Exists(settingsFilePath))
			{
				LearnersSettings defaults = LearnersSettings.CreateDefault();
				await SaveAsync(defaults, cancellationToken);
				return defaults;
			}

			try
			{
				string json = await File.ReadAllTextAsync(settingsFilePath, cancellationToken);

				if (string.IsNullOrWhiteSpace(json))
				{
					LearnersSettings defaults = LearnersSettings.CreateDefault();
					await SaveAsync(defaults, cancellationToken);
					return defaults;
				}

				LearnersSettings? settings = JsonSerializer.Deserialize<LearnersSettings>(json, JsonOptions);

				if (settings == null)
				{
					LearnersSettings defaults = LearnersSettings.CreateDefault();
					await SaveAsync(defaults, cancellationToken);
					return defaults;
				}

				Normalize(settings);
				return settings;
			}
			catch (JsonException)
			{
				LearnersSettings defaults = LearnersSettings.CreateDefault();
				await SaveAsync(defaults, cancellationToken);
				return defaults;
			}
		}

		public async Task SaveAsync(
			LearnersSettings settings,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(settings);

			Normalize(settings);

			string settingsDirectory = GetSettingsDirectoryPath();
			Directory.CreateDirectory(settingsDirectory);

			string json = JsonSerializer.Serialize(settings, JsonOptions);
			await File.WriteAllTextAsync(GetSettingsFilePath(), json, cancellationToken);
		}

		private string GetSettingsDirectoryPath()
		{
			return Path.Combine(_appPaths.ConfigDirectory, "Modules", ModuleFolderName);
		}

		private string GetSettingsFilePath()
		{
			return Path.Combine(GetSettingsDirectoryPath(), SettingsFileName);
		}

		private static void Normalize(LearnersSettings settings)
		{
			settings.Pronouns = NormalizeList(settings.Pronouns);
			settings.ReligiousAffiliations = NormalizeList(settings.ReligiousAffiliations);
		}

		private static List<LearnersLookupListItem> NormalizeList(
			IEnumerable<LearnersLookupListItem>? items)
		{
			if (items == null)
			{
				return new List<LearnersLookupListItem>();
			}

			List<LearnersLookupListItem> normalized = items
				.Where(x => x != null)
				.Select(x => new LearnersLookupListItem
				{
					Value = NormalizeRequired(x.Value),
					SortOrder = x.SortOrder
				})
				.Where(x => !string.IsNullOrWhiteSpace(x.Value))
				.GroupBy(x => x.Value, StringComparer.OrdinalIgnoreCase)
				.Select(group => group
					.OrderBy(x => x.SortOrder)
					.ThenBy(x => x.Value, StringComparer.OrdinalIgnoreCase)
					.First())
				.OrderBy(x => x.SortOrder)
				.ThenBy(x => x.Value, StringComparer.OrdinalIgnoreCase)
				.ToList();

			for (int index = 0; index < normalized.Count; index++)
			{
				if (normalized[index].SortOrder <= 0)
				{
					normalized[index].SortOrder = index + 1;
				}
			}

			return normalized;
		}

		private static string NormalizeRequired(string value)
		{
			return string.IsNullOrWhiteSpace(value)
				? string.Empty
				: value.Trim();
		}
	}
}
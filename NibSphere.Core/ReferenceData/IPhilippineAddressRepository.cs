using NibSphere.Core.Interfaces;
using NibSphere.Core.ReferenceData.Models;
using System.IO;
using System.Text.Json;

namespace NibSphere.Data.ReferenceData
{
	public class PhilippineAddressRepository : IPhilippineAddressRepository
	{
		private readonly IAppPaths _appPaths;
		private PhilippineAddressDataset? _cachedData;

		public PhilippineAddressRepository(IAppPaths appPaths)
		{
			_appPaths = appPaths;
		}

		public bool ReferenceFileExists()
		{
			return File.Exists(_appPaths.PhilippineAddressDataFilePath);
		}

		public string GetReferenceFilePath()
		{
			return _appPaths.PhilippineAddressDataFilePath;
		}

		public async Task<PhilippineAddressDataset> LoadAsync()
		{
			if (_cachedData != null)
			{
				return _cachedData;
			}

			EnsureReferenceFileExists();

			string json = await File.ReadAllTextAsync(_appPaths.PhilippineAddressDataFilePath);

			JsonSerializerOptions options = new()
			{
				PropertyNameCaseInsensitive = true
			};

			PhilippineAddressDataset? data = JsonSerializer.Deserialize<PhilippineAddressDataset>(json, options);

			if (data == null)
			{
				throw new InvalidDataException("Philippine address reference file could not be parsed.");
			}

			data.TopLevels ??= new List<AddressTopLevel>();
			data.LocalitiesByParent ??= new Dictionary<string, List<AddressLocality>>();
			data.BarangaysByLocality ??= new Dictionary<string, List<AddressBarangay>>();

			if (data.TopLevels.Count == 0)
			{
				throw new InvalidDataException(
					"The Philippine address reference file was loaded, but no top-level areas were parsed. Check JSON property mappings.");
			}

			foreach (KeyValuePair<string, List<AddressLocality>> entry in data.LocalitiesByParent)
			{
				entry.Value.Sort(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
			}

			foreach (KeyValuePair<string, List<AddressBarangay>> entry in data.BarangaysByLocality)
			{
				entry.Value.Sort(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
			}

			data.TopLevels = data.TopLevels
				.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
				.ToList();

			_cachedData = data;
			return _cachedData;
		}

		public async Task<IReadOnlyList<AddressTopLevel>> GetTopLevelsAsync()
		{
			PhilippineAddressDataset data = await LoadAsync();
			return data.TopLevels;
		}

		public async Task<IReadOnlyList<AddressTopLevel>> GetProvincesOnlyAsync()
		{
			PhilippineAddressDataset data = await LoadAsync();

			return data.TopLevels
				.Where(x => string.Equals(x.Kind, "province", StringComparison.OrdinalIgnoreCase))
				.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		public async Task<IReadOnlyList<AddressTopLevel>> GetRegionsOnlyAsync()
		{
			PhilippineAddressDataset data = await LoadAsync();

			return data.TopLevels
				.Where(x => string.Equals(x.Kind, "region", StringComparison.OrdinalIgnoreCase))
				.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		public async Task<IReadOnlyList<AddressLocality>> GetLocalitiesByTopLevelCodeAsync(string topLevelCode)
		{
			if (string.IsNullOrWhiteSpace(topLevelCode))
			{
				return Array.Empty<AddressLocality>();
			}

			PhilippineAddressDataset data = await LoadAsync();

			return data.LocalitiesByParent.TryGetValue(topLevelCode, out List<AddressLocality>? localities)
				? localities
				: Array.Empty<AddressLocality>();
		}

		public async Task<IReadOnlyList<AddressBarangay>> GetBarangaysByLocalityCodeAsync(string localityCode)
		{
			if (string.IsNullOrWhiteSpace(localityCode))
			{
				return Array.Empty<AddressBarangay>();
			}

			PhilippineAddressDataset data = await LoadAsync();

			return data.BarangaysByLocality.TryGetValue(localityCode, out List<AddressBarangay>? barangays)
				? barangays
				: Array.Empty<AddressBarangay>();
		}

		public async Task<AddressTopLevel?> FindTopLevelByCodeAsync(string code)
		{
			if (string.IsNullOrWhiteSpace(code))
			{
				return null;
			}

			PhilippineAddressDataset data = await LoadAsync();

			return data.TopLevels.FirstOrDefault(x =>
				string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
		}

		public async Task<AddressLocality?> FindLocalityByCodeAsync(string code)
		{
			if (string.IsNullOrWhiteSpace(code))
			{
				return null;
			}

			PhilippineAddressDataset data = await LoadAsync();

			foreach (List<AddressLocality> localities in data.LocalitiesByParent.Values)
			{
				AddressLocality? match = localities.FirstOrDefault(x =>
					string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));

				if (match != null)
				{
					return match;
				}
			}

			return null;
		}

		private void EnsureReferenceFileExists()
		{
			if (!ReferenceFileExists())
			{
				throw new FileNotFoundException(
					$"Philippine address reference file was not found: {_appPaths.PhilippineAddressDataFilePath}");
			}
		}
	}
}
using NibSphere.Core.ReferenceData.Models;

namespace NibSphere.Core.ReferenceData
{
	public interface IPhilippineAddressRepository
	{
		bool ReferenceFileExists();
		string GetReferenceFilePath();

		Task<PhilippineAddressDataset> LoadAsync();
		Task<IReadOnlyList<AddressTopLevel>> GetTopLevelsAsync();
		Task<IReadOnlyList<AddressTopLevel>> GetProvincesOnlyAsync();
		Task<IReadOnlyList<AddressTopLevel>> GetRegionsOnlyAsync();
		Task<IReadOnlyList<AddressLocality>> GetLocalitiesByTopLevelCodeAsync(string topLevelCode);
		Task<IReadOnlyList<AddressBarangay>> GetBarangaysByLocalityCodeAsync(string localityCode);
		Task<AddressTopLevel?> FindTopLevelByCodeAsync(string code);
		Task<AddressLocality?> FindLocalityByCodeAsync(string code);
	}
}
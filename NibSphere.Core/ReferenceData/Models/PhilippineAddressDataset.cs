namespace NibSphere.Core.ReferenceData.Models
{
	public class PhilippineAddressDataset
	{
		public string? SyncedAt { get; set; }
		public List<AddressTopLevel> TopLevels { get; set; } = new();
		public Dictionary<string, List<AddressLocality>> LocalitiesByParent { get; set; } = new();
		public Dictionary<string, List<AddressBarangay>> BarangaysByLocality { get; set; } = new();
		public AddressCounts? Counts { get; set; }
	}
}
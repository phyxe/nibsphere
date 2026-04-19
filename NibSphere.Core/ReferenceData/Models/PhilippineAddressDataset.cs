using System.Text.Json.Serialization;

namespace NibSphere.Core.ReferenceData.Models
{
	public class PhilippineAddressDataset
	{
		[JsonPropertyName("synced_at")]
		public string? SyncedAt { get; set; }

		[JsonPropertyName("top_levels")]
		public List<AddressTopLevel> TopLevels { get; set; } = new();

		[JsonPropertyName("localities_by_parent")]
		public Dictionary<string, List<AddressLocality>> LocalitiesByParent { get; set; } = new();

		[JsonPropertyName("barangays_by_locality")]
		public Dictionary<string, List<AddressBarangay>> BarangaysByLocality { get; set; } = new();

		[JsonPropertyName("counts")]
		public AddressCounts? Counts { get; set; }
	}
}
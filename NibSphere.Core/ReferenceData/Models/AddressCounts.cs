using System.Text.Json.Serialization;

namespace NibSphere.Core.ReferenceData.Models
{
	public class AddressCounts
	{
		[JsonPropertyName("top_levels")]
		public int TopLevels { get; set; }

		[JsonPropertyName("provinces")]
		public int Provinces { get; set; }

		[JsonPropertyName("regions_as_top_levels")]
		public int RegionsAsTopLevels { get; set; }

		[JsonPropertyName("localities")]
		public int Localities { get; set; }

		[JsonPropertyName("barangays")]
		public int Barangays { get; set; }
	}
}
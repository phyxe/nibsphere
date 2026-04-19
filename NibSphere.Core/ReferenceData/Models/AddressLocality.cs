using System.Text.Json.Serialization;

namespace NibSphere.Core.ReferenceData.Models
{
	public class AddressLocality
	{
		[JsonPropertyName("code")]
		public string Code { get; set; } = string.Empty;

		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("type")]
		public string? Type { get; set; }

		[JsonPropertyName("kind")]
		public string Kind { get; set; } = string.Empty;

		[JsonPropertyName("parent_code")]
		public string ParentCode { get; set; } = string.Empty;

		[JsonPropertyName("parent_kind")]
		public string ParentKind { get; set; } = string.Empty;
	}
}
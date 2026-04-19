namespace NibSphere.Core.ReferenceData.Models
{
	public class AddressLocality
	{
		public string Code { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string? Type { get; set; } // City or Mun
		public string Kind { get; set; } = string.Empty; // city or municipality
		public string ParentCode { get; set; } = string.Empty;
		public string ParentKind { get; set; } = string.Empty;
	}
}
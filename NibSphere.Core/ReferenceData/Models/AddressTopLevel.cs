namespace NibSphere.Core.ReferenceData.Models
{
	public class AddressTopLevel
	{
		public string Code { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string Kind { get; set; } = string.Empty; // province or region
	}
}
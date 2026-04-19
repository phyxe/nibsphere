namespace NibSphere.Core.ReferenceData.Models
{
	public class AddressBarangay
	{
		public string Code { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;

		public override string ToString()
		{
			return Name;
		}
	}
}
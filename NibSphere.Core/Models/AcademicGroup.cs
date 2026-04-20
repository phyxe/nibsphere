namespace NibSphere.Core.Models
{
	public class AcademicGroup
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public int Sort { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}
}
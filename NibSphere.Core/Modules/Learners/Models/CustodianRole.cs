namespace NibSphere.Core.Modules.Learners.Models
{
	public class CustodianRole
	{
		public int Id { get; set; }

		public string RelationshipType { get; set; } = string.Empty;
		public string RelationshipLabel { get; set; } = string.Empty;

		public int SortOrder { get; set; }
	}
}
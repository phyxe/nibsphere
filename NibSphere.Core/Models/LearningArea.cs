namespace NibSphere.Core.Models
{
	public class LearningArea
	{
		public int Id { get; set; }
		public string Category { get; set; } = string.Empty;
		public string Code { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public int Sort { get; set; }
	}
}
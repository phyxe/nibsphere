namespace NibSphere.Core.Modules
{
	public sealed class ModuleNavItemDefinition
	{
		public required string ItemKey { get; init; }
		public required string Title { get; init; }

		public string? IconPath { get; init; }

		public int SortOrder { get; init; }

		public bool IsDefault { get; init; }

		public Func<object>? ContentFactory { get; init; }

		public IReadOnlyList<ModuleNavItemDefinition> Children { get; init; } =
			Array.Empty<ModuleNavItemDefinition>();
	}
}
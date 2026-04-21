namespace NibSphere.Core.Modules
{
	public interface IAppModuleDefinition
	{
		string ModuleKey { get; }

		string DisplayName { get; }

		int SortOrder { get; }

		IReadOnlyList<ModuleNavItemDefinition> NavigationItems { get; }
	}
}
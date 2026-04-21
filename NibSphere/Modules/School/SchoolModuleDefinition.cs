using NibSphere.Core.Modules;
using NibSphere.Views;

namespace NibSphere.Modules.School
{
	public sealed class SchoolModuleDefinition : IAppModuleDefinition
	{
		public string ModuleKey => "school";

		public string DisplayName => "School";

		public int SortOrder => 999;

		public IReadOnlyList<ModuleNavItemDefinition> NavigationItems =>
			new[]
			{
				new ModuleNavItemDefinition
				{
					ItemKey = "school",
					Title = "School Information",
					IconPath = "/Resources/Icons/school.svg",
					SortOrder = 999,
					Children = new[]
					{
						new ModuleNavItemDefinition
						{
							ItemKey = "school-profile",
							Title = "School Profile",
							SortOrder = 10,
							IsDefault = true,
							ContentFactory = static () => new SchoolProfileView()
						},
						new ModuleNavItemDefinition
						{
							ItemKey = "learning-areas",
							Title = "Learning Areas",
							SortOrder = 20,
							ContentFactory = static () => new LearningAreasView()
						}
					}
				}
			};
	}
}
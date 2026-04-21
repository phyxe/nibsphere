using NibSphere.Core.Modules;
using NibSphere.Modules.Learners.Views;

namespace NibSphere.Modules.Learners
{
	public sealed class LearnersModuleDefinition : IAppModuleDefinition
	{
		public string ModuleKey => "learners";

		public string DisplayName => "Learners";

		public int SortOrder => 200;

		public IReadOnlyList<ModuleNavItemDefinition> NavigationItems =>
			new[]
			{
				new ModuleNavItemDefinition
				{
					ItemKey = "learners",
					Title = "Learners",
					IconPath = "/Modules/Learners/Resources/Icons/student.svg",
					SortOrder = 200,
					ContentFactory = static () => new LearnersListView()
				}
			};
	}
}
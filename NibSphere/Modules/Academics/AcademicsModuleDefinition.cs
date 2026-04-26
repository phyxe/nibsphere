using NibSphere.Core.Modules;
using NibSphere.Modules.Academics.Views;

namespace NibSphere.Modules.Academics
{
	public sealed class AcademicsModuleDefinition : IAppModuleDefinition
	{
		public string ModuleKey => "academics";

		public string DisplayName => "Academics";

		public int SortOrder => 300;

		public IReadOnlyList<ModuleNavItemDefinition> NavigationItems =>
			new[]
			{
				new ModuleNavItemDefinition
				{
					ItemKey = "academics",
					Title = "Academics",
					SortOrder = 300,
					Children = new[]
					{
						new ModuleNavItemDefinition
						{
							ItemKey = "academics-school-years",
							Title = "School Years",
							SortOrder = 10,
							IsDefault = true,
							ContentFactory = static () => new AcademicsSchoolYearsView()
						},
						new ModuleNavItemDefinition
						{
							ItemKey = "academics-subjects",
							Title = "Subjects",
							SortOrder = 20,
							ContentFactory = static () => new AcademicsSubjectsView()
						},
						new ModuleNavItemDefinition
						{
							ItemKey = "academics-enrollments",
							Title = "Enrollments",
							SortOrder = 30,
							ContentFactory = static () => new AcademicsEnrollmentsView()
						},
						new ModuleNavItemDefinition
						{
							ItemKey = "academics-setup",
							Title = "Setup",
							SortOrder = 40,
							ContentFactory = static () => new AcademicsSetupView()
						}
					}
				}
			};
	}
}
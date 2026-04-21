namespace NibSphere.Core.Modules.Learners.Settings
{
	public class LearnersSettings
	{
		public List<LearnersLookupListItem> Pronouns { get; set; } = new();

		public List<LearnersLookupListItem> ReligiousAffiliations { get; set; } = new();

		public static LearnersSettings CreateDefault()
		{
			return new LearnersSettings
			{
				Pronouns = new List<LearnersLookupListItem>
				{
					new() { Value = "He/Him", SortOrder = 1 },
					new() { Value = "She/Her", SortOrder = 2 },
					new() { Value = "They/Them", SortOrder = 3 },
					new() { Value = "Prefer not to say", SortOrder = 4 }
				},
				ReligiousAffiliations = new List<LearnersLookupListItem>
				{
					new() { Value = "Roman Catholic", SortOrder = 1 },
					new() { Value = "Iglesia ni Cristo", SortOrder = 2 },
					new() { Value = "Islam", SortOrder = 3 },
					new() { Value = "Seventh-day Adventist", SortOrder = 4 },
					new() { Value = "Christian", SortOrder = 5 },
					new() { Value = "None", SortOrder = 6 },
					new() { Value = "Prefer not to say", SortOrder = 7 }
				}
			};
		}
	}
}
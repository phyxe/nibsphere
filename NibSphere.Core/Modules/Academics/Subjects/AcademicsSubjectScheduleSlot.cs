namespace NibSphere.Core.Modules.Academics.Subjects
{
	public sealed class AcademicsSubjectScheduleSlot
	{
		public int Id { get; set; }

		public int SubjectId { get; set; }

		public int DayOfWeekNumber { get; set; }
		public string DayOfWeekName { get; set; } = string.Empty;

		public TimeSpan? StartTime { get; set; }
		public TimeSpan? EndTime { get; set; }

		public string Room { get; set; } = string.Empty;

		public int SortOrder { get; set; }

		public bool IsActive { get; set; } = true;

		public string TimeDisplay
		{
			get
			{
				if (!StartTime.HasValue && !EndTime.HasValue)
				{
					return string.Empty;
				}

				string start = StartTime.HasValue
					? DateTime.Today.Add(StartTime.Value).ToString("h:mm tt")
					: string.Empty;

				string end = EndTime.HasValue
					? DateTime.Today.Add(EndTime.Value).ToString("h:mm tt")
					: string.Empty;

				if (string.IsNullOrWhiteSpace(start))
				{
					return end;
				}

				if (string.IsNullOrWhiteSpace(end))
				{
					return start;
				}

				return $"{start} - {end}";
			}
		}

		public string DisplayName
		{
			get
			{
				string day = string.IsNullOrWhiteSpace(DayOfWeekName)
					? "Schedule"
					: DayOfWeekName;

				string time = TimeDisplay;

				if (string.IsNullOrWhiteSpace(time))
				{
					return day;
				}

				return $"{day}, {time}";
			}
		}
	}
}
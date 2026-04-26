namespace NibSphere.Core.Modules.Academics.Subjects
{
	public sealed class AcademicsSubject
	{
		public int Id { get; set; }

		public int SchoolYearId { get; set; }
		public string SchoolYearName { get; set; } = string.Empty;

		public int TermId { get; set; }
		public string TermName { get; set; } = string.Empty;

		public int SectionId { get; set; }
		public string GradeLevelName { get; set; } = string.Empty;
		public string SectionName { get; set; } = string.Empty;

		public int LearningAreaId { get; set; }
		public string LearningAreaCode { get; set; } = string.Empty;
		public string LearningAreaShortName { get; set; } = string.Empty;
		public string LearningAreaDescription { get; set; } = string.Empty;

		public int? SchoolYearProgramLineId { get; set; }

		public int? TeacherId { get; set; }

		public string TeacherLastName { get; set; } = string.Empty;
		public string TeacherFirstName { get; set; } = string.Empty;
		public string TeacherMiddleName { get; set; } = string.Empty;
		public string TeacherExtensionName { get; set; } = string.Empty;

		public string TeacherPosition { get; set; } = string.Empty;
		public string TeacherDesignation { get; set; } = string.Empty;

		public string SubjectCode { get; set; } = string.Empty;
		public string SubjectName { get; set; } = string.Empty;

		public int SortOrder { get; set; }

		public bool IsActive { get; set; } = true;

		public string SectionDisplayName => $"{GradeLevelName} - {SectionName}".Trim();

		public string LearningAreaDisplayName
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(LearningAreaShortName))
				{
					return LearningAreaShortName;
				}

				if (!string.IsNullOrWhiteSpace(LearningAreaCode))
				{
					return LearningAreaCode;
				}

				return LearningAreaDescription;
			}
		}

		public string TeacherFullNameDisplay
		{
			get
			{
				if (string.IsNullOrWhiteSpace(TeacherLastName) &&
					string.IsNullOrWhiteSpace(TeacherFirstName))
				{
					return string.Empty;
				}

				string fullName = $"{TeacherLastName}, {TeacherFirstName}".Trim().Trim(',');

				if (!string.IsNullOrWhiteSpace(TeacherMiddleName))
				{
					fullName += $" {TeacherMiddleName.Trim()}";
				}

				if (!string.IsNullOrWhiteSpace(TeacherExtensionName))
				{
					fullName += $" {TeacherExtensionName.Trim()}";
				}

				return fullName.Trim();
			}
		}

		public string DisplayName
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(SubjectName))
				{
					return SubjectName;
				}

				return LearningAreaDisplayName;
			}
		}
	}
}
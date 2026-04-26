namespace NibSphere.Core.Modules.Academics.Enrollments
{
	public sealed class AcademicsEnrollment
	{
		public int Id { get; set; }

		public int LearnerId { get; set; }

		public string LearnerLastName { get; set; } = string.Empty;
		public string LearnerFirstName { get; set; } = string.Empty;
		public string LearnerMiddleName { get; set; } = string.Empty;
		public string LearnerExtensionName { get; set; } = string.Empty;
		public string LearnerLrn { get; set; } = string.Empty;

		public int SchoolYearId { get; set; }
		public string SchoolYearName { get; set; } = string.Empty;

		public int SchoolYearProgramId { get; set; }

		public string ProgramCode { get; set; } = string.Empty;
		public string ProgramName { get; set; } = string.Empty;

		public int SchoolYearSectionId { get; set; }

		public string GradeLevelName { get; set; } = string.Empty;
		public string SectionName { get; set; } = string.Empty;

		public int EnrollmentStatusId { get; set; }

		public string EnrollmentStatusCode { get; set; } = string.Empty;
		public string EnrollmentStatusName { get; set; } = string.Empty;

		public DateTime? EnrollmentDate { get; set; }
		public DateTime? EffectiveStartDate { get; set; }
		public DateTime? EffectiveEndDate { get; set; }

		public bool IsCurrent { get; set; } = true;
		public bool IsActive { get; set; } = true;

		public string Remarks { get; set; } = string.Empty;

		public string LearnerFullNameDisplay
		{
			get
			{
				string fullName = $"{LearnerLastName}, {LearnerFirstName}".Trim().Trim(',');

				if (!string.IsNullOrWhiteSpace(LearnerMiddleName))
				{
					fullName += $" {LearnerMiddleName.Trim()}";
				}

				if (!string.IsNullOrWhiteSpace(LearnerExtensionName))
				{
					fullName += $" {LearnerExtensionName.Trim()}";
				}

				return fullName.Trim();
			}
		}

		public string ProgramDisplayName
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(ProgramCode))
				{
					return $"{ProgramCode} - {ProgramName}".Trim();
				}

				return ProgramName;
			}
		}

		public string SectionDisplayName => $"{GradeLevelName} - {SectionName}".Trim();
	}
}
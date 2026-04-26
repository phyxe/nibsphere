namespace NibSphere.Core.Modules.Academics.Enrollments
{
	public sealed class AcademicsEnrollmentSubject
	{
		public int Id { get; set; }

		public int EnrollmentId { get; set; }
		public int SubjectId { get; set; }

		public int? EnrollmentStatusId { get; set; }

		public string EnrollmentStatusCode { get; set; } = string.Empty;
		public string EnrollmentStatusName { get; set; } = string.Empty;

		public string SubjectCode { get; set; } = string.Empty;
		public string SubjectName { get; set; } = string.Empty;

		public string LearningAreaCode { get; set; } = string.Empty;
		public string LearningAreaShortName { get; set; } = string.Empty;
		public string LearningAreaDescription { get; set; } = string.Empty;

		public string TermName { get; set; } = string.Empty;

		public string TeacherLastName { get; set; } = string.Empty;
		public string TeacherFirstName { get; set; } = string.Empty;
		public string TeacherMiddleName { get; set; } = string.Empty;
		public string TeacherExtensionName { get; set; } = string.Empty;

		public DateTime? EffectiveStartDate { get; set; }
		public DateTime? EffectiveEndDate { get; set; }

		public bool IsActive { get; set; } = true;

		public string SubjectDisplayName
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(SubjectName))
				{
					return SubjectName;
				}

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
	}
}
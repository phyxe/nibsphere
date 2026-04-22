using NibSphere.Core.Modules.Learners.Models;
using NibSphere.Core.Modules.Learners.Profile;

namespace NibSphere.Core.Modules.Learners.Importing
{
	public sealed class LearnersImportRowData
	{
		public Learner Learner { get; set; } = new();

		public IReadOnlyList<LearnersImportCustodianInput> Custodians { get; set; } =
			Array.Empty<LearnersImportCustodianInput>();

		public LearnerProfileRecord ToProfileRecord()
		{
			return new LearnerProfileRecord
			{
				Mode = LearnerProfileMode.Add,
				Learner = Learner,
				Custodians = Custodians
					.Where(x => x.HasAnyData())
					.Select((x, index) => x.ToCardItem(index + 1))
					.ToList()
			};
		}
	}
}
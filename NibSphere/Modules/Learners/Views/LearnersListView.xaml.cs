using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Learners.Models;
using NibSphere.Data.Modules.Learners.Repositories;
using System.Windows;
using System.Windows.Controls;

namespace NibSphere.Modules.Learners.Views
{
	public partial class LearnersListView : UserControl
	{
		private readonly LearnerRepository _learnerRepository;

		public LearnersListView()
		{
			InitializeComponent();

			IAppPaths appPaths = App.AppPaths;
			_learnerRepository = new LearnerRepository(appPaths);

			Loaded += LearnersListView_Loaded;
		}

		private async void LearnersListView_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= LearnersListView_Loaded;
			await LoadLearnersAsync();
		}

		private async Task LoadLearnersAsync()
		{
			List<Learner> learners = await _learnerRepository.GetAllAsync();

			LearnersDataGrid.ItemsSource = learners
				.Select(x => new LearnerListRow
				{
					Learner = x,
					Lrn = x.Lrn,
					FullNameDisplay = BuildFullNameDisplay(x)
				})
				.ToList();
		}

		private void AddLearnerButton_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show(
				"Add Learner is not implemented yet.",
				"Learners",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void LearnerSettingsButton_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show(
				"Learner Settings is not implemented yet.",
				"Learners",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void EditLearnerButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not LearnerListRow row)
			{
				return;
			}

			MessageBox.Show(
				$"Edit Learner is not implemented yet.{Environment.NewLine}{Environment.NewLine}{row.FullNameDisplay}",
				"Learners",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private async void DeleteLearnerButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not LearnerListRow row)
			{
				return;
			}

			MessageBoxResult result = MessageBox.Show(
				$"Delete learner '{row.FullNameDisplay}'?",
				"Confirm Delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (result != MessageBoxResult.Yes)
			{
				return;
			}

			await _learnerRepository.DeleteAsync(row.Learner.Id);
			await LoadLearnersAsync();

			MessageBox.Show(
				"Learner deleted successfully.",
				"Deleted",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private static string BuildFullNameDisplay(Learner learner)
		{
			string lastName = learner.LastName?.Trim() ?? string.Empty;
			string firstName = learner.FirstName?.Trim() ?? string.Empty;
			string middleName = learner.MiddleName?.Trim() ?? string.Empty;
			string extensionName = learner.ExtensionName?.Trim() ?? string.Empty;

			string middleInitial = string.IsNullOrWhiteSpace(middleName)
				? string.Empty
				: $"{middleName[0]}.";

			List<string> trailingParts = new();

			if (!string.IsNullOrWhiteSpace(firstName))
			{
				trailingParts.Add(firstName);
			}

			if (!string.IsNullOrWhiteSpace(middleInitial))
			{
				trailingParts.Add(middleInitial);
			}

			string trailing = string.Join(" ", trailingParts);

			string fullName = string.IsNullOrWhiteSpace(lastName)
				? trailing
				: string.IsNullOrWhiteSpace(trailing)
					? lastName
					: $"{lastName}, {trailing}";

			if (!string.IsNullOrWhiteSpace(extensionName))
			{
				fullName = string.IsNullOrWhiteSpace(fullName)
					? extensionName
					: $"{fullName} {extensionName}";
			}

			return fullName.Trim();
		}

		private sealed class LearnerListRow
		{
			public required Learner Learner { get; init; }

			public string Lrn { get; init; } = string.Empty;

			public string FullNameDisplay { get; init; } = string.Empty;
		}
	}
}
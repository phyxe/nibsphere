using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Learners.Models;
using NibSphere.Core.Modules.Learners.Profile;
using NibSphere.Data.Modules.Learners.Repositories;
using NibSphere.Modules.Learners.Windows;
using System.Windows;
using System.Windows.Controls;

namespace NibSphere.Modules.Learners.Views
{
	public partial class LearnersListView : UserControl
	{
		private readonly LearnerRepository _learnerRepository;
		private readonly LearnerCustodianRepository _learnerCustodianRepository;

		public LearnersListView()
		{
			InitializeComponent();

			IAppPaths appPaths = App.AppPaths;
			_learnerRepository = new LearnerRepository(appPaths);
			_learnerCustodianRepository = new LearnerCustodianRepository(appPaths);

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
			NavigateToProfile(new LearnerProfileView(LearnerProfileMode.Add));
		}

		private void ViewLearnerButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not LearnerListRow row)
			{
				return;
			}

			NavigateToProfile(new LearnerProfileView(
				LearnerProfileMode.View,
				row.Learner.Id));
		}

		private async void LearnerSettingsButton_Click(object sender, RoutedEventArgs e)
		{
			LearnersSettingsWindow window = new LearnersSettingsWindow
			{
				Owner = Window.GetWindow(this)
			};

			bool? result = window.ShowDialog();

			if (result == true)
			{
				await LoadLearnersAsync();
			}
		}

		private void EditLearnerButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not LearnerListRow row)
			{
				return;
			}

			NavigateToProfile(new LearnerProfileView(
				LearnerProfileMode.Edit,
				row.Learner.Id));
		}

		private async void DeleteLearnerButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not LearnerListRow row)
			{
				return;
			}

			MessageBoxResult result = MessageBox.Show(
				$"Delete learner '{row.FullNameDisplay}'?{Environment.NewLine}{Environment.NewLine}This will remove the learner profile and its learner-custodian links. Shared custodian records will be kept.",
				"Confirm Delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (result != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				await _learnerCustodianRepository.DeleteByLearnerIdAsync(row.Learner.Id);
				await _learnerRepository.DeleteAsync(row.Learner.Id);
				await LoadLearnersAsync();

				MessageBox.Show(
					"Learner deleted successfully.",
					"Deleted",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Unable to delete learner.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
					"Delete Failed",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private void NavigateToProfile(UserControl view)
		{
			if (Window.GetWindow(this) is MainWindow mainWindow)
			{
				mainWindow.ShowContent(view);
			}
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
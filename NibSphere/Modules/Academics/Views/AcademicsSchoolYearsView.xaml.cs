using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Academics.SchoolYears;
using NibSphere.Data.Modules.Academics.SchoolYears;
using System.Windows;
using System.Windows.Controls;

namespace NibSphere.Modules.Academics.Views
{
	public partial class AcademicsSchoolYearsView : UserControl
	{
		private readonly AcademicsSchoolYearRepository _schoolYearRepository;
		private readonly AcademicsSchoolYearTermRepository _termRepository;

		private AcademicsSchoolYear? _selectedSchoolYear;
		private AcademicsSchoolYearTerm? _editingTerm;

		public AcademicsSchoolYearsView()
		{
			InitializeComponent();

			IAppPaths appPaths = App.AppPaths;

			_schoolYearRepository = new AcademicsSchoolYearRepository(appPaths);
			_termRepository = new AcademicsSchoolYearTermRepository(appPaths);

			Loaded += AcademicsSchoolYearsView_Loaded;
		}

		private async void AcademicsSchoolYearsView_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= AcademicsSchoolYearsView_Loaded;

			await LoadSchoolYearsAsync();
			ShowListMode();
		}

		private async Task LoadSchoolYearsAsync()
		{
			List<AcademicsSchoolYear> schoolYears = await _schoolYearRepository.GetAllAsync(includeInactive: true);

			SchoolYearsItemsControl.ItemsSource = schoolYears
				.Select(x => new SchoolYearCardRow(x))
				.ToList();

			EmptySchoolYearsTextBlock.Visibility = schoolYears.Count == 0
				? Visibility.Visible
				: Visibility.Collapsed;
		}

		private async Task LoadSelectedSchoolYearAsync(int schoolYearId)
		{
			_selectedSchoolYear = await _schoolYearRepository.GetByIdAsync(schoolYearId);

			if (_selectedSchoolYear == null)
			{
				await LoadSchoolYearsAsync();
				ShowListMode();
				return;
			}

			await LoadTermsAsync(_selectedSchoolYear.Id);
			LoadOverview();
		}

		private async Task LoadTermsAsync(int schoolYearId)
		{
			List<AcademicsSchoolYearTerm> terms = await _termRepository.GetBySchoolYearIdAsync(
				schoolYearId,
				includeInactive: true);

			TermsItemsControl.ItemsSource = terms
				.Select(x => new TermAccordionRow(x))
				.ToList();

			EmptyTermsTextBlock.Visibility = terms.Count == 0
				? Visibility.Visible
				: Visibility.Collapsed;
		}

		private void LoadOverview()
		{
			if (_selectedSchoolYear == null)
			{
				return;
			}

			OverviewTitleTextBlock.Text = _selectedSchoolYear.Name;
			OverviewDateRangeTextBlock.Text = $"{FormatDate(_selectedSchoolYear.StartDate)} to {FormatDate(_selectedSchoolYear.EndDate)}";
			OverviewTermCountTextBlock.Text = $"{_selectedSchoolYear.TermCount} term{(_selectedSchoolYear.TermCount == 1 ? string.Empty : "s")}";
			OverviewStatusTextBlock.Text = BuildStatusDisplay(_selectedSchoolYear);

			OverviewDependencyTextBlock.Text = _selectedSchoolYear.DependentRecordCount == 0
				? "No dependent academic records yet."
				: $"{_selectedSchoolYear.DependentRecordCount} related academic record{(_selectedSchoolYear.DependentRecordCount == 1 ? string.Empty : "s")}.";

			DeleteSchoolYearButton.IsEnabled = _selectedSchoolYear.IsDeletable;
		}

		private void ShowListMode()
		{
			SchoolYearListPanel.Visibility = Visibility.Visible;
			SchoolYearOverviewPanel.Visibility = Visibility.Collapsed;
			SchoolYearEditorPanel.Visibility = Visibility.Collapsed;
			TermEditorPanel.Visibility = Visibility.Collapsed;
		}

		private void ShowOverviewMode()
		{
			SchoolYearListPanel.Visibility = Visibility.Collapsed;
			SchoolYearOverviewPanel.Visibility = Visibility.Visible;
			SchoolYearEditorPanel.Visibility = Visibility.Collapsed;
			TermEditorPanel.Visibility = Visibility.Collapsed;
		}

		private void ShowSchoolYearEditorMode()
		{
			SchoolYearListPanel.Visibility = Visibility.Collapsed;
			SchoolYearOverviewPanel.Visibility = Visibility.Collapsed;
			SchoolYearEditorPanel.Visibility = Visibility.Visible;
			TermEditorPanel.Visibility = Visibility.Collapsed;
		}

		private void ShowTermEditorMode()
		{
			SchoolYearListPanel.Visibility = Visibility.Collapsed;
			SchoolYearOverviewPanel.Visibility = Visibility.Collapsed;
			SchoolYearEditorPanel.Visibility = Visibility.Collapsed;
			TermEditorPanel.Visibility = Visibility.Visible;
		}

		private async void OpenSchoolYearButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not SchoolYearCardRow row)
			{
				return;
			}

			await LoadSelectedSchoolYearAsync(row.SchoolYear.Id);
			ShowOverviewMode();
		}

		private async void BackToListButton_Click(object sender, RoutedEventArgs e)
		{
			_selectedSchoolYear = null;
			_editingTerm = null;

			await LoadSchoolYearsAsync();
			ShowListMode();
		}

		private void NewSchoolYearButton_Click(object sender, RoutedEventArgs e)
		{
			_selectedSchoolYear = null;
			_editingTerm = null;

			SchoolYearEditorTitleTextBlock.Text = "New School Year";
			SchoolYearNameTextBox.Text = string.Empty;
			SchoolYearStartDatePicker.SelectedDate = null;
			SchoolYearEndDatePicker.SelectedDate = null;
			SchoolYearIsCurrentCheckBox.IsChecked = false;
			SchoolYearIsActiveCheckBox.IsChecked = true;

			ShowSchoolYearEditorMode();
		}

		private void EditSchoolYearButton_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedSchoolYear == null)
			{
				return;
			}

			SchoolYearEditorTitleTextBlock.Text = $"Edit School Year: {_selectedSchoolYear.Name}";
			SchoolYearNameTextBox.Text = _selectedSchoolYear.Name;
			SchoolYearStartDatePicker.SelectedDate = _selectedSchoolYear.StartDate;
			SchoolYearEndDatePicker.SelectedDate = _selectedSchoolYear.EndDate;
			SchoolYearIsCurrentCheckBox.IsChecked = _selectedSchoolYear.IsCurrent;
			SchoolYearIsActiveCheckBox.IsChecked = _selectedSchoolYear.IsActive;

			ShowSchoolYearEditorMode();
		}

		private async void SaveSchoolYearButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				AcademicsSchoolYear schoolYear = _selectedSchoolYear ?? new AcademicsSchoolYear();

				schoolYear.Name = SchoolYearNameTextBox.Text;
				schoolYear.StartDate = SchoolYearStartDatePicker.SelectedDate;
				schoolYear.EndDate = SchoolYearEndDatePicker.SelectedDate;
				schoolYear.IsCurrent = SchoolYearIsCurrentCheckBox.IsChecked == true;
				schoolYear.IsActive = SchoolYearIsActiveCheckBox.IsChecked == true;

				int selectedId;

				if (schoolYear.Id > 0)
				{
					await _schoolYearRepository.UpdateAsync(schoolYear);
					selectedId = schoolYear.Id;
				}
				else
				{
					selectedId = await _schoolYearRepository.InsertAsync(schoolYear);
				}

				await LoadSchoolYearsAsync();
				await LoadSelectedSchoolYearAsync(selectedId);
				ShowOverviewMode();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Unable to save school year.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
					"Save Failed",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private void CancelSchoolYearEditorButton_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedSchoolYear == null)
			{
				ShowListMode();
				return;
			}

			LoadOverview();
			ShowOverviewMode();
		}

		private async void DeleteSchoolYearButton_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedSchoolYear == null)
			{
				return;
			}

			if (!_selectedSchoolYear.IsDeletable)
			{
				MessageBox.Show(
					"This school year cannot be deleted because it already has terms or related academic records.",
					"Delete Not Allowed",
					MessageBoxButton.OK,
					MessageBoxImage.Information);

				return;
			}

			MessageBoxResult result = MessageBox.Show(
				$"Delete school year '{_selectedSchoolYear.Name}'?",
				"Confirm Delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (result != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				await _schoolYearRepository.DeleteAsync(_selectedSchoolYear.Id);

				_selectedSchoolYear = null;
				_editingTerm = null;

				await LoadSchoolYearsAsync();
				ShowListMode();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Unable to delete school year.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
					"Delete Failed",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private async void AddTermButton_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedSchoolYear == null)
			{
				return;
			}

			_editingTerm = null;

			await LoadParentTermOptionsAsync(null);

			TermEditorTitleTextBlock.Text = "Add Term";
			TermNameTextBox.Text = string.Empty;
			TermShortNameTextBox.Text = string.Empty;
			TermStartDatePicker.SelectedDate = _selectedSchoolYear.StartDate;
			TermEndDatePicker.SelectedDate = _selectedSchoolYear.EndDate;
			ParentTermComboBox.SelectedValue = 0;
			TermIsParentTermCheckBox.IsChecked = true;
			ParentTermPanel.Visibility = Visibility.Collapsed;
			TermSortOrderTextBox.Text = "0";
			TermIsEnrollmentCheckBox.IsChecked = false;
			TermIsGradingCheckBox.IsChecked = true;
			TermIsReportingCheckBox.IsChecked = true;
			TermIsActiveCheckBox.IsChecked = true;

			ShowTermEditorMode();
		}

		private async void EditTermButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not TermAccordionRow row)
			{
				return;
			}

			_editingTerm = row.Term;

			await LoadParentTermOptionsAsync(_editingTerm.Id);

			bool isParentTerm = !_editingTerm.ParentTermId.HasValue;

			TermEditorTitleTextBlock.Text = $"Edit Term: {_editingTerm.DisplayName}";
			TermNameTextBox.Text = _editingTerm.Name;
			TermShortNameTextBox.Text = _editingTerm.ShortName;
			TermStartDatePicker.SelectedDate = _editingTerm.StartDate;
			TermEndDatePicker.SelectedDate = _editingTerm.EndDate;
			TermIsParentTermCheckBox.IsChecked = isParentTerm;
			ParentTermPanel.Visibility = isParentTerm ? Visibility.Collapsed : Visibility.Visible;
			ParentTermComboBox.SelectedValue = _editingTerm.ParentTermId ?? 0;
			TermSortOrderTextBox.Text = _editingTerm.SortOrder.ToString();
			TermIsEnrollmentCheckBox.IsChecked = _editingTerm.IsEnrollmentTerm;
			TermIsGradingCheckBox.IsChecked = _editingTerm.IsGradingTerm;
			TermIsReportingCheckBox.IsChecked = _editingTerm.IsReportingTerm;
			TermIsActiveCheckBox.IsChecked = _editingTerm.IsActive;

			ShowTermEditorMode();
		}

		private async void SaveTermButton_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedSchoolYear == null)
			{
				return;
			}

			try
			{
				int sortOrder = 0;

				if (!string.IsNullOrWhiteSpace(TermSortOrderTextBox.Text) &&
					!int.TryParse(TermSortOrderTextBox.Text, out sortOrder))
				{
					throw new InvalidOperationException("Sort order must be a whole number.");
				}

				bool isParentTerm = TermIsParentTermCheckBox.IsChecked == true;

				int selectedParentId = ParentTermComboBox.SelectedValue is int parentId
					? parentId
					: 0;

				AcademicsSchoolYearTerm term = _editingTerm ?? new AcademicsSchoolYearTerm();

				term.SchoolYearId = _selectedSchoolYear.Id;
				term.ParentTermId = isParentTerm || selectedParentId <= 0
					? null
					: selectedParentId;
				term.Name = TermNameTextBox.Text;
				term.ShortName = TermShortNameTextBox.Text;
				term.StartDate = TermStartDatePicker.SelectedDate;
				term.EndDate = TermEndDatePicker.SelectedDate;
				term.SortOrder = sortOrder;
				term.IsEnrollmentTerm = TermIsEnrollmentCheckBox.IsChecked == true;
				term.IsGradingTerm = TermIsGradingCheckBox.IsChecked == true;
				term.IsReportingTerm = TermIsReportingCheckBox.IsChecked == true;
				term.IsActive = TermIsActiveCheckBox.IsChecked == true;

				if (term.Id > 0)
				{
					await _termRepository.UpdateAsync(term);
				}
				else
				{
					await _termRepository.InsertAsync(term);
				}

				_editingTerm = null;

				await LoadSchoolYearsAsync();
				await LoadSelectedSchoolYearAsync(_selectedSchoolYear.Id);
				ShowOverviewMode();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Unable to save term.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
					"Save Failed",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private void CancelTermButton_Click(object sender, RoutedEventArgs e)
		{
			_editingTerm = null;

			LoadOverview();
			ShowOverviewMode();
		}

		private async void DeleteTermButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not TermAccordionRow row)
			{
				return;
			}

			if (!row.Term.IsDeletable)
			{
				MessageBox.Show(
					"This term cannot be deleted because it already has child terms or related subjects.",
					"Delete Not Allowed",
					MessageBoxButton.OK,
					MessageBoxImage.Information);

				return;
			}

			MessageBoxResult result = MessageBox.Show(
				$"Delete term '{row.Name}'?",
				"Confirm Delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (result != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				await _termRepository.DeleteAsync(row.Term.Id);

				if (_selectedSchoolYear != null)
				{
					await LoadSchoolYearsAsync();
					await LoadSelectedSchoolYearAsync(_selectedSchoolYear.Id);
					ShowOverviewMode();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Unable to delete term.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
					"Delete Failed",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private void TermIsParentTermCheckBox_Changed(object sender, RoutedEventArgs e)
		{
			bool isParentTerm = TermIsParentTermCheckBox.IsChecked == true;

			ParentTermPanel.Visibility = isParentTerm
				? Visibility.Collapsed
				: Visibility.Visible;

			if (isParentTerm)
			{
				ParentTermComboBox.SelectedValue = 0;
			}
		}

		private async Task LoadParentTermOptionsAsync(int? excludeTermId)
		{
			if (_selectedSchoolYear == null)
			{
				ParentTermComboBox.ItemsSource = null;
				return;
			}

			List<AcademicsSchoolYearTerm> parentTerms = await _termRepository.GetParentOptionsBySchoolYearIdAsync(
				_selectedSchoolYear.Id,
				excludeTermId,
				includeInactive: false);

			List<ParentTermOptionRow> options = new()
			{
				new ParentTermOptionRow(0, "No parent term")
			};

			options.AddRange(parentTerms.Select(x => new ParentTermOptionRow(x.Id, x.DisplayName)));

			ParentTermComboBox.ItemsSource = options;
			ParentTermComboBox.SelectedValue = 0;
		}

		private sealed class SchoolYearCardRow
		{
			public SchoolYearCardRow(AcademicsSchoolYear schoolYear)
			{
				SchoolYear = schoolYear;
				Name = schoolYear.Name;
				DateRangeDisplay = $"{FormatDate(schoolYear.StartDate)} to {FormatDate(schoolYear.EndDate)}";
				TermSummaryDisplay = $"{schoolYear.TermCount} term{(schoolYear.TermCount == 1 ? string.Empty : "s")}";
				StatusDisplay = BuildStatusDisplay(schoolYear);
			}

			public AcademicsSchoolYear SchoolYear { get; }

			public string Name { get; }
			public string DateRangeDisplay { get; }
			public string TermSummaryDisplay { get; }
			public string StatusDisplay { get; }
		}

		private sealed class ParentTermOptionRow
		{
			public ParentTermOptionRow(int id, string displayName)
			{
				Id = id;
				DisplayName = displayName;
			}

			public int Id { get; }
			public string DisplayName { get; }

			public override string ToString()
			{
				return DisplayName;
			}
		}

		private sealed class TermAccordionRow
		{
			public TermAccordionRow(AcademicsSchoolYearTerm term)
			{
				Term = term;

				Name = term.Name;
				HeaderDisplay = term.DisplayName;
				DateRangeDisplay = $"{FormatDate(term.StartDate)} to {FormatDate(term.EndDate)}";
				DetailDisplay = BuildDetailDisplay(term);
			}

			public AcademicsSchoolYearTerm Term { get; }

			public string HeaderDisplay { get; }
			public string Name { get; }
			public string DateRangeDisplay { get; }
			public string DetailDisplay { get; }

			private static string BuildDetailDisplay(AcademicsSchoolYearTerm term)
			{
				List<string> parts = new()
				{
					term.TermScope
				};

				if (!string.IsNullOrWhiteSpace(term.ParentTermName))
				{
					parts.Add($"Parent: {term.ParentTermName}");
				}

				List<string> uses = new();

				if (term.IsEnrollmentTerm)
				{
					uses.Add("Enrollment");
				}

				if (term.IsGradingTerm)
				{
					uses.Add("Grading");
				}

				if (term.IsReportingTerm)
				{
					uses.Add("Reporting");
				}

				if (uses.Count > 0)
				{
					parts.Add(string.Join(", ", uses));
				}

				if (!term.IsActive)
				{
					parts.Add("Archived");
				}

				return string.Join(" • ", parts);
			}
		}

		private static string BuildStatusDisplay(AcademicsSchoolYear schoolYear)
		{
			List<string> parts = new();

			parts.Add(schoolYear.IsCurrent ? "Current" : "Not current");
			parts.Add(schoolYear.IsActive ? "Active" : "Archived");

			return string.Join(" • ", parts);
		}

		private static string FormatDate(DateTime? value)
		{
			return value.HasValue ? value.Value.ToString("MMM d, yyyy") : "No date";
		}
	}
}
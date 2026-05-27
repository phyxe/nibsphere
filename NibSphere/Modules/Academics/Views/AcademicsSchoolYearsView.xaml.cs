using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Academics.SchoolYears;
using NibSphere.Data.Modules.Academics.SchoolYears;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
		}

		private async Task LoadSchoolYearsAsync(int? selectedSchoolYearId = null)
		{
			List<AcademicsSchoolYear> schoolYears = await _schoolYearRepository.GetAllAsync(includeInactive: true);

			List<SchoolYearCardRow> rows = schoolYears
				.Select(x => new SchoolYearCardRow(x))
				.ToList();

			SchoolYearsItemsControl.ItemsSource = rows;

			AcademicsSchoolYear? selected = null;

			if (selectedSchoolYearId.HasValue)
			{
				selected = schoolYears.FirstOrDefault(x => x.Id == selectedSchoolYearId.Value);
			}

			selected ??= schoolYears.FirstOrDefault(x => x.IsCurrent);
			selected ??= schoolYears.FirstOrDefault();

			await SelectSchoolYearAsync(selected);
		}

		private async Task SelectSchoolYearAsync(AcademicsSchoolYear? schoolYear)
		{
			_selectedSchoolYear = schoolYear;

			if (schoolYear == null)
			{
				ClearSchoolYearForm();
				TermsItemsControl.ItemsSource = null;
				return;
			}

			SelectedSchoolYearTitleTextBlock.Text = schoolYear.Name;

			SchoolYearNameTextBox.Text = schoolYear.Name;
			SchoolYearStartDatePicker.SelectedDate = schoolYear.StartDate;
			SchoolYearEndDatePicker.SelectedDate = schoolYear.EndDate;
			SchoolYearIsCurrentCheckBox.IsChecked = schoolYear.IsCurrent;
			SchoolYearIsActiveCheckBox.IsChecked = schoolYear.IsActive;

			DeleteSchoolYearButton.IsEnabled = schoolYear.IsDeletable;

			await LoadTermsAsync(schoolYear.Id);
		}

		private async Task LoadTermsAsync(int schoolYearId)
		{
			List<AcademicsSchoolYearTerm> terms = await _termRepository.GetBySchoolYearIdAsync(
				schoolYearId,
				includeInactive: true);

			TermsItemsControl.ItemsSource = terms
				.Select(x => new TermAccordionRow(x))
				.ToList();
		}

		private void ClearSchoolYearForm()
		{
			SelectedSchoolYearTitleTextBlock.Text = "New School Year";

			SchoolYearNameTextBox.Text = string.Empty;
			SchoolYearStartDatePicker.SelectedDate = null;
			SchoolYearEndDatePicker.SelectedDate = null;
			SchoolYearIsCurrentCheckBox.IsChecked = false;
			SchoolYearIsActiveCheckBox.IsChecked = true;
			TermEditorBorder.Visibility = Visibility.Collapsed;

			DeleteSchoolYearButton.IsEnabled = false;
		}

		private async void SchoolYearCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (sender is not Border border ||
				border.Tag is not SchoolYearCardRow row)
			{
				return;
			}

			await SelectSchoolYearAsync(row.SchoolYear);
		}

		private void NewSchoolYearButton_Click(object sender, RoutedEventArgs e)
		{
			_selectedSchoolYear = null;
			ClearSchoolYearForm();
			TermsItemsControl.ItemsSource = null;
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

				await LoadSchoolYearsAsync(selectedId);
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
				await LoadSchoolYearsAsync();
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
				MessageBox.Show(
					"Select or save a school year first before adding terms.",
					"School Year Required",
					MessageBoxButton.OK,
					MessageBoxImage.Information);

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
			TermSortOrderTextBox.Text = "0";
			TermIsEnrollmentCheckBox.IsChecked = false;
			TermIsGradingCheckBox.IsChecked = true;
			TermIsReportingCheckBox.IsChecked = true;
			TermIsActiveCheckBox.IsChecked = true;

			TermEditorBorder.Visibility = Visibility.Visible;
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

			TermEditorTitleTextBlock.Text = $"Edit Term: {_editingTerm.DisplayName}";
			TermNameTextBox.Text = _editingTerm.Name;
			TermShortNameTextBox.Text = _editingTerm.ShortName;
			TermStartDatePicker.SelectedDate = _editingTerm.StartDate;
			TermEndDatePicker.SelectedDate = _editingTerm.EndDate;
			ParentTermComboBox.SelectedValue = _editingTerm.ParentTermId ?? 0;
			TermSortOrderTextBox.Text = _editingTerm.SortOrder.ToString();
			TermIsEnrollmentCheckBox.IsChecked = _editingTerm.IsEnrollmentTerm;
			TermIsGradingCheckBox.IsChecked = _editingTerm.IsGradingTerm;
			TermIsReportingCheckBox.IsChecked = _editingTerm.IsReportingTerm;
			TermIsActiveCheckBox.IsChecked = _editingTerm.IsActive;

			TermEditorBorder.Visibility = Visibility.Visible;
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
					await LoadTermsAsync(_selectedSchoolYear.Id);
					await LoadSchoolYearsAsync(_selectedSchoolYear.Id);
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

				int selectedParentId = ParentTermComboBox.SelectedValue is int parentId
					? parentId
					: 0;

				AcademicsSchoolYearTerm term = _editingTerm ?? new AcademicsSchoolYearTerm();

				term.SchoolYearId = _selectedSchoolYear.Id;
				term.ParentTermId = selectedParentId <= 0 ? null : selectedParentId;
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

				TermEditorBorder.Visibility = Visibility.Collapsed;
				_editingTerm = null;

				await LoadTermsAsync(_selectedSchoolYear.Id);
				await LoadSchoolYearsAsync(_selectedSchoolYear.Id);
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
			TermEditorBorder.Visibility = Visibility.Collapsed;
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

			private static string BuildStatusDisplay(AcademicsSchoolYear schoolYear)
			{
				List<string> parts = new();

				parts.Add(schoolYear.IsCurrent ? "Current" : "Not current");
				parts.Add(schoolYear.IsActive ? "Active" : "Archived");

				return string.Join(" • ", parts);
			}
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
		}

		private sealed class TermAccordionRow
		{
			public TermAccordionRow(AcademicsSchoolYearTerm term)
			{
				Term = term;

				Name = term.Name;
				ShortName = string.IsNullOrWhiteSpace(term.ShortName) ? "—" : term.ShortName;
				DateRangeDisplay = $"{FormatDate(term.StartDate)} to {FormatDate(term.EndDate)}";
				TermScope = term.TermScope;
				UsageDisplay = BuildUsageDisplay(term);
				HeaderDisplay = $"{term.DisplayName} • {DateRangeDisplay}";
			}

			public AcademicsSchoolYearTerm Term { get; }

			public string HeaderDisplay { get; }
			public string Name { get; }
			public string ShortName { get; }
			public string DateRangeDisplay { get; }
			public string TermScope { get; }
			public string UsageDisplay { get; }

			private static string BuildUsageDisplay(AcademicsSchoolYearTerm term)
			{
				List<string> parts = new();

				if (term.IsEnrollmentTerm)
				{
					parts.Add("Enrollment");
				}

				if (term.IsGradingTerm)
				{
					parts.Add("Grading");
				}

				if (term.IsReportingTerm)
				{
					parts.Add("Reporting");
				}

				if (!term.IsActive)
				{
					parts.Add("Archived");
				}

				return parts.Count == 0 ? "No use selected" : string.Join(" • ", parts);
			}
		}

		private static string FormatDate(DateTime? value)
		{
			return value.HasValue ? value.Value.ToString("MMM d, yyyy") : "No date";
		}
	}
}
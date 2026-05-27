using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Academics.Setup;
using NibSphere.Data.Modules.Academics.Setup;
using System.Windows;
using System.Windows.Controls;

namespace NibSphere.Modules.Academics.Views
{
	public partial class AcademicsSetupView : UserControl
	{
		private readonly AcademicsGradeLevelRepository _gradeLevelRepository;
		private readonly AcademicsProgramRepository _programRepository;
		private readonly AcademicsSectionTemplateRepository _sectionTemplateRepository;

		private AcademicsGradeLevel? _selectedGradeLevel;
		private AcademicsProgram? _selectedProgram;
		private AcademicsSectionTemplate? _selectedSectionTemplate;

		public AcademicsSetupView()
		{
			InitializeComponent();

			IAppPaths appPaths = App.AppPaths;

			_gradeLevelRepository = new AcademicsGradeLevelRepository(appPaths);
			_programRepository = new AcademicsProgramRepository(appPaths);
			_sectionTemplateRepository = new AcademicsSectionTemplateRepository(appPaths);

			Loaded += AcademicsSetupView_Loaded;
		}

		private async void AcademicsSetupView_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= AcademicsSetupView_Loaded;

			await LoadAllAsync();

			ClearGradeLevelForm();
			ClearProgramForm();
			ClearSectionTemplateForm();
		}

		private async Task LoadAllAsync()
		{
			await LoadGradeLevelsAsync();
			await LoadProgramsAsync();
			await LoadSectionTemplateGradeLevelOptionsAsync();
			await LoadSectionTemplatesAsync();
		}

		private async Task LoadGradeLevelsAsync()
		{
			List<AcademicsGradeLevel> gradeLevels = await _gradeLevelRepository.GetAllAsync(includeInactive: true);

			GradeLevelsItemsControl.ItemsSource = gradeLevels
				.Select(x => new GradeLevelRow(x))
				.ToList();
		}

		private async Task LoadProgramsAsync()
		{
			List<AcademicsProgram> programs = await _programRepository.GetAllAsync(includeInactive: true);

			ProgramsItemsControl.ItemsSource = programs
				.Select(x => new ProgramRow(x))
				.ToList();
		}

		private async Task LoadSectionTemplatesAsync()
		{
			List<AcademicsSectionTemplate> sectionTemplates = await _sectionTemplateRepository.GetAllAsync(includeInactive: true);

			SectionTemplatesItemsControl.ItemsSource = sectionTemplates
				.Select(x => new SectionTemplateRow(x))
				.ToList();
		}

		private async Task LoadSectionTemplateGradeLevelOptionsAsync()
		{
			List<AcademicsGradeLevel> gradeLevels = await _gradeLevelRepository.GetAllAsync(includeInactive: false);

			SectionTemplateGradeLevelComboBox.ItemsSource = gradeLevels
				.Select(x => new GradeLevelOptionRow(x.Name, x.DisplayName))
				.ToList();
		}

		private void ClearGradeLevelForm()
		{
			_selectedGradeLevel = null;

			GradeLevelEditorTitleTextBlock.Text = "New Grade Level";
			GradeLevelNameTextBox.Text = string.Empty;
			GradeLevelShortNameTextBox.Text = string.Empty;
			GradeLevelSortOrderTextBox.Text = "0";
			GradeLevelIsActiveCheckBox.IsChecked = true;

			DeleteGradeLevelButton.IsEnabled = false;
		}

		private void ClearProgramForm()
		{
			_selectedProgram = null;

			ProgramEditorTitleTextBlock.Text = "New Program";
			ProgramCodeTextBox.Text = string.Empty;
			ProgramNameTextBox.Text = string.Empty;
			ProgramDescriptionTextBox.Text = string.Empty;
			ProgramSortOrderTextBox.Text = "0";
			ProgramIsActiveCheckBox.IsChecked = true;

			ToggleProgramActiveButton.IsEnabled = false;
			ToggleProgramActiveButton.Content = "Archive";
		}

		private void ClearSectionTemplateForm()
		{
			_selectedSectionTemplate = null;

			SectionTemplateEditorTitleTextBlock.Text = "New Section Template";
			SectionTemplateGradeLevelComboBox.SelectedValue = null;
			SectionTemplateNameTextBox.Text = string.Empty;
			SectionTemplateSortOrderTextBox.Text = "0";
			SectionTemplateIsActiveCheckBox.IsChecked = true;

			DeleteSectionTemplateButton.IsEnabled = false;
		}

		private void EditGradeLevelButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not GradeLevelRow row)
			{
				return;
			}

			_selectedGradeLevel = row.GradeLevel;

			GradeLevelEditorTitleTextBlock.Text = $"Edit Grade Level: {_selectedGradeLevel.DisplayName}";
			GradeLevelNameTextBox.Text = _selectedGradeLevel.Name;
			GradeLevelShortNameTextBox.Text = _selectedGradeLevel.ShortName;
			GradeLevelSortOrderTextBox.Text = _selectedGradeLevel.SortOrder.ToString();
			GradeLevelIsActiveCheckBox.IsChecked = _selectedGradeLevel.IsActive;

			DeleteGradeLevelButton.IsEnabled = _selectedGradeLevel.IsDeletable;
		}

		private void NewGradeLevelButton_Click(object sender, RoutedEventArgs e)
		{
			ClearGradeLevelForm();
		}

		private async void SaveGradeLevelButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				AcademicsGradeLevel gradeLevel = _selectedGradeLevel ?? new AcademicsGradeLevel();

				gradeLevel.Name = GradeLevelNameTextBox.Text;
				gradeLevel.ShortName = GradeLevelShortNameTextBox.Text;
				gradeLevel.SortOrder = ParseSortOrder(GradeLevelSortOrderTextBox.Text);
				gradeLevel.IsActive = GradeLevelIsActiveCheckBox.IsChecked == true;

				if (gradeLevel.Id > 0)
				{
					await _gradeLevelRepository.UpdateAsync(gradeLevel);
				}
				else
				{
					await _gradeLevelRepository.InsertAsync(gradeLevel);
				}

				await LoadGradeLevelsAsync();
				await LoadSectionTemplateGradeLevelOptionsAsync();
				ClearGradeLevelForm();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Unable to save grade level.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
					"Save Failed",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private async void DeleteGradeLevelButton_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedGradeLevel == null)
			{
				return;
			}

			MessageBoxResult result = MessageBox.Show(
				$"Delete grade level '{_selectedGradeLevel.DisplayName}'?",
				"Confirm Delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (result != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				await _gradeLevelRepository.DeleteAsync(_selectedGradeLevel.Id);
				await LoadGradeLevelsAsync();
				await LoadSectionTemplateGradeLevelOptionsAsync();
				ClearGradeLevelForm();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Unable to delete grade level.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
					"Delete Failed",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private void EditProgramButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not ProgramRow row)
			{
				return;
			}

			_selectedProgram = row.Program;

			ProgramEditorTitleTextBlock.Text = $"Edit Program: {_selectedProgram.Code}";
			ProgramCodeTextBox.Text = _selectedProgram.Code;
			ProgramNameTextBox.Text = _selectedProgram.Name;
			ProgramDescriptionTextBox.Text = _selectedProgram.Description;
			ProgramSortOrderTextBox.Text = _selectedProgram.SortOrder.ToString();
			ProgramIsActiveCheckBox.IsChecked = _selectedProgram.IsActive;

			ToggleProgramActiveButton.IsEnabled = true;
			ToggleProgramActiveButton.Content = _selectedProgram.IsActive ? "Archive" : "Restore";
		}

		private void NewProgramButton_Click(object sender, RoutedEventArgs e)
		{
			ClearProgramForm();
		}

		private async void SaveProgramButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				AcademicsProgram program = _selectedProgram ?? new AcademicsProgram();

				program.Code = ProgramCodeTextBox.Text;
				program.Name = ProgramNameTextBox.Text;
				program.Description = ProgramDescriptionTextBox.Text;
				program.SortOrder = ParseSortOrder(ProgramSortOrderTextBox.Text);
				program.IsActive = ProgramIsActiveCheckBox.IsChecked == true;

				if (program.Id > 0)
				{
					await _programRepository.UpdateAsync(program);
				}
				else
				{
					await _programRepository.InsertAsync(program);
				}

				await LoadProgramsAsync();
				ClearProgramForm();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Unable to save program.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
					"Save Failed",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private async void ToggleProgramActiveButton_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedProgram == null)
			{
				return;
			}

			try
			{
				bool newActiveState = !_selectedProgram.IsActive;

				await _programRepository.SetIsActiveAsync(
					_selectedProgram.Id,
					newActiveState);

				await LoadProgramsAsync();
				ClearProgramForm();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Unable to update program status.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
					"Update Failed",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private void EditSectionTemplateButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not SectionTemplateRow row)
			{
				return;
			}

			_selectedSectionTemplate = row.SectionTemplate;

			SectionTemplateEditorTitleTextBlock.Text = $"Edit Section Template: {_selectedSectionTemplate.DisplayName}";
			SectionTemplateGradeLevelComboBox.SelectedValue = _selectedSectionTemplate.GradeLevelName;
			SectionTemplateNameTextBox.Text = _selectedSectionTemplate.SectionName;
			SectionTemplateSortOrderTextBox.Text = _selectedSectionTemplate.SortOrder.ToString();
			SectionTemplateIsActiveCheckBox.IsChecked = _selectedSectionTemplate.IsActive;

			DeleteSectionTemplateButton.IsEnabled = _selectedSectionTemplate.IsDeletable;
		}

		private void NewSectionTemplateButton_Click(object sender, RoutedEventArgs e)
		{
			ClearSectionTemplateForm();
		}

		private async void SaveSectionTemplateButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string selectedGradeLevelName = SectionTemplateGradeLevelComboBox.SelectedValue as string ?? string.Empty;

				AcademicsSectionTemplate sectionTemplate = _selectedSectionTemplate ?? new AcademicsSectionTemplate();

				sectionTemplate.GradeLevelName = selectedGradeLevelName;
				sectionTemplate.SectionName = SectionTemplateNameTextBox.Text;
				sectionTemplate.SortOrder = ParseSortOrder(SectionTemplateSortOrderTextBox.Text);
				sectionTemplate.IsActive = SectionTemplateIsActiveCheckBox.IsChecked == true;

				if (sectionTemplate.Id > 0)
				{
					await _sectionTemplateRepository.UpdateAsync(sectionTemplate);
				}
				else
				{
					await _sectionTemplateRepository.InsertAsync(sectionTemplate);
				}

				await LoadSectionTemplatesAsync();
				ClearSectionTemplateForm();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Unable to save section template.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
					"Save Failed",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private async void DeleteSectionTemplateButton_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedSectionTemplate == null)
			{
				return;
			}

			MessageBoxResult result = MessageBox.Show(
				$"Delete section template '{_selectedSectionTemplate.DisplayName}'?",
				"Confirm Delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (result != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				await _sectionTemplateRepository.DeleteAsync(_selectedSectionTemplate.Id);
				await LoadSectionTemplatesAsync();
				ClearSectionTemplateForm();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Unable to delete section template.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
					"Delete Failed",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private static int ParseSortOrder(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return 0;
			}

			if (!int.TryParse(value, out int sortOrder))
			{
				throw new InvalidOperationException("Sort order must be a whole number.");
			}

			return Math.Max(0, sortOrder);
		}

		private sealed class GradeLevelRow
		{
			public GradeLevelRow(AcademicsGradeLevel gradeLevel)
			{
				GradeLevel = gradeLevel;
				DisplayName = gradeLevel.DisplayName;
				DetailDisplay = BuildDetailDisplay(gradeLevel);
			}

			public AcademicsGradeLevel GradeLevel { get; }

			public string DisplayName { get; }
			public string DetailDisplay { get; }

			private static string BuildDetailDisplay(AcademicsGradeLevel gradeLevel)
			{
				List<string> parts = new();

				parts.Add($"Sort order: {gradeLevel.SortOrder}");
				parts.Add(gradeLevel.IsActive ? "Active" : "Archived");

				if (gradeLevel.DependentRecordCount > 0)
				{
					parts.Add($"{gradeLevel.DependentRecordCount} related record{(gradeLevel.DependentRecordCount == 1 ? string.Empty : "s")}");
				}

				return string.Join(" • ", parts);
			}
		}

		private sealed class GradeLevelOptionRow
		{
			public GradeLevelOptionRow(string name, string displayName)
			{
				Name = name;
				DisplayName = displayName;
			}

			public string Name { get; }
			public string DisplayName { get; }

			public override string ToString()
			{
				return DisplayName;
			}
		}

		private sealed class ProgramRow
		{
			public ProgramRow(AcademicsProgram program)
			{
				Program = program;
				DisplayName = $"{program.Code} - {program.Name}".Trim();
				DetailDisplay = BuildDetailDisplay(program);
			}

			public AcademicsProgram Program { get; }

			public string DisplayName { get; }
			public string DetailDisplay { get; }

			private static string BuildDetailDisplay(AcademicsProgram program)
			{
				List<string> parts = new();

				parts.Add($"Sort order: {program.SortOrder}");
				parts.Add(program.IsActive ? "Active" : "Archived");

				if (!string.IsNullOrWhiteSpace(program.Description))
				{
					parts.Add(program.Description);
				}

				return string.Join(" • ", parts);
			}
		}

		private sealed class SectionTemplateRow
		{
			public SectionTemplateRow(AcademicsSectionTemplate sectionTemplate)
			{
				SectionTemplate = sectionTemplate;
				DisplayName = sectionTemplate.DisplayName;
				DetailDisplay = BuildDetailDisplay(sectionTemplate);
			}

			public AcademicsSectionTemplate SectionTemplate { get; }

			public string DisplayName { get; }
			public string DetailDisplay { get; }

			private static string BuildDetailDisplay(AcademicsSectionTemplate sectionTemplate)
			{
				List<string> parts = new();

				parts.Add($"Sort order: {sectionTemplate.SortOrder}");
				parts.Add(sectionTemplate.IsActive ? "Active" : "Archived");

				if (sectionTemplate.DependentRecordCount > 0)
				{
					parts.Add($"{sectionTemplate.DependentRecordCount} deployed section{(sectionTemplate.DependentRecordCount == 1 ? string.Empty : "s")}");
				}

				return string.Join(" • ", parts);
			}
		}
	}
}
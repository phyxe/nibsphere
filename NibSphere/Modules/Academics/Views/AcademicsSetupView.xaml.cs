using NibSphere.Core.Interfaces;
using NibSphere.Core.Models;
using NibSphere.Core.Modules.Academics.Setup;
using NibSphere.Data.Modules.Academics.Setup;
using NibSphere.Data.Repositories;
using System.Windows;
using System.Windows.Controls;

namespace NibSphere.Modules.Academics.Views
{
	public partial class AcademicsSetupView : UserControl
	{
		private readonly AcademicsGradeLevelRepository _gradeLevelRepository;
		private readonly AcademicsProgramRepository _programRepository;
		private readonly AcademicsSectionTemplateRepository _sectionTemplateRepository;
		private readonly AcademicsProgramProspectusLineRepository _prospectusLineRepository;
		private readonly LearningAreaRepository _learningAreaRepository;


		private AcademicsGradeLevel? _selectedGradeLevel;
		private AcademicsProgram? _selectedProgram;
		private AcademicsSectionTemplate? _selectedSectionTemplate;
		private AcademicsProgramProspectusLine? _selectedProspectusLine;


		public AcademicsSetupView()
		{
			InitializeComponent();

			IAppPaths appPaths = App.AppPaths;

			_gradeLevelRepository = new AcademicsGradeLevelRepository(appPaths);
			_programRepository = new AcademicsProgramRepository(appPaths);
			_sectionTemplateRepository = new AcademicsSectionTemplateRepository(appPaths);
			_prospectusLineRepository = new AcademicsProgramProspectusLineRepository(appPaths);
			_learningAreaRepository = new LearningAreaRepository(appPaths);

			Loaded += AcademicsSetupView_Loaded;
		}

		private async void AcademicsSetupView_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= AcademicsSetupView_Loaded;

			await LoadAllAsync();

			ClearGradeLevelForm();
			ClearProgramForm();
			ClearSectionTemplateForm();
			ClearProspectusLineForm();
		}

		private async Task LoadAllAsync()
		{
			await LoadGradeLevelsAsync();
			await LoadProgramsAsync();
			await LoadSectionTemplateGradeLevelOptionsAsync();
			await LoadSectionTemplatesAsync();
			await LoadProspectusProgramOptionsAsync();
			await LoadProspectusGradeLevelOptionsAsync();
			await LoadProspectusLearningAreaOptionsAsync();
			await LoadProspectusLinesForSelectedProgramAsync();
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

		private async Task LoadProspectusProgramOptionsAsync()
		{
			List<AcademicsProgram> programs = await _programRepository.GetAllAsync(includeInactive: false);

			int currentSelectedId = GetSelectedProspectusProgramId();

			List<ProgramOptionRow> options = programs
				.Select(x => new ProgramOptionRow(x.Id, $"{x.Code} - {x.Name}".Trim()))
				.ToList();

			ProspectusProgramComboBox.ItemsSource = options;

			if (currentSelectedId > 0 && options.Any(x => x.Id == currentSelectedId))
			{
				ProspectusProgramComboBox.SelectedValue = currentSelectedId;
			}
			else if (options.Count > 0)
			{
				ProspectusProgramComboBox.SelectedValue = options[0].Id;
			}
		}

		private async Task LoadProspectusGradeLevelOptionsAsync()
		{
			List<AcademicsGradeLevel> gradeLevels = await _gradeLevelRepository.GetAllAsync(includeInactive: false);

			ProspectusGradeLevelComboBox.ItemsSource = gradeLevels
				.Select(x => new GradeLevelOptionRow(x.Name, x.DisplayName))
				.ToList();
		}

		private async Task LoadProspectusLearningAreaOptionsAsync()
		{
			List<LearningArea> learningAreas = await _learningAreaRepository.GetAllAsync();

			ProspectusLearningAreaComboBox.ItemsSource = learningAreas
				.Select(x => new LearningAreaOptionRow(x.Id, BuildLearningAreaDisplayName(x)))
				.ToList();
		}

		private async Task LoadProspectusLinesForSelectedProgramAsync()
		{
			int programId = GetSelectedProspectusProgramId();

			if (programId <= 0)
			{
				ProspectusLinesItemsControl.ItemsSource = null;
				EmptyProspectusLinesTextBlock.Visibility = Visibility.Visible;
				return;
			}

			List<AcademicsProgramProspectusLine> lines = await _prospectusLineRepository.GetByProgramIdAsync(
				programId,
				includeInactive: true);

			ProspectusLinesItemsControl.ItemsSource = lines
				.Select(x => new ProspectusLineRow(x))
				.ToList();

			EmptyProspectusLinesTextBlock.Visibility = lines.Count == 0
				? Visibility.Visible
				: Visibility.Collapsed;
		}

		private int GetSelectedProspectusProgramId()
		{
			return ProspectusProgramComboBox.SelectedValue is int id
				? id
				: 0;
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

		private void ClearProspectusLineForm()
		{
			_selectedProspectusLine = null;

			ProspectusLineEditorTitleTextBlock.Text = "New Prospectus Line";
			ProspectusGradeLevelComboBox.SelectedValue = null;
			ProspectusLearningAreaComboBox.SelectedValue = null;
			ProspectusTermSequenceTextBox.Text = "1";
			ProspectusTermLabelTextBox.Text = string.Empty;
			ProspectusSortOrderTextBox.Text = "0";
			ProspectusIsActiveCheckBox.IsChecked = true;

			ToggleProspectusLineActiveButton.IsEnabled = false;
			ToggleProspectusLineActiveButton.Content = "Archive";
			DeleteProspectusLineButton.IsEnabled = false;
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
				await LoadProspectusGradeLevelOptionsAsync();
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
				await LoadProspectusGradeLevelOptionsAsync();
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
				await LoadProspectusProgramOptionsAsync();
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
				await LoadProspectusProgramOptionsAsync();
				await LoadProspectusLinesForSelectedProgramAsync();
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

		private async void ProspectusProgramComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ClearProspectusLineForm();
			await LoadProspectusLinesForSelectedProgramAsync();
		}

		private void NewProspectusLineButton_Click(object sender, RoutedEventArgs e)
		{
			ClearProspectusLineForm();
		}

		private void EditProspectusLineButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not ProspectusLineRow row)
			{
				return;
			}

			_selectedProspectusLine = row.Line;

			ProspectusLineEditorTitleTextBlock.Text = $"Edit Prospectus Line: {_selectedProspectusLine.DisplayName}";
			ProspectusGradeLevelComboBox.SelectedValue = _selectedProspectusLine.GradeLevelName;
			ProspectusLearningAreaComboBox.SelectedValue = _selectedProspectusLine.LearningAreaId;
			ProspectusTermSequenceTextBox.Text = _selectedProspectusLine.TemplateTermSequence.ToString();
			ProspectusTermLabelTextBox.Text = _selectedProspectusLine.TemplateTermLabel;
			ProspectusSortOrderTextBox.Text = _selectedProspectusLine.SortOrder.ToString();
			ProspectusIsActiveCheckBox.IsChecked = _selectedProspectusLine.IsActive;

			ToggleProspectusLineActiveButton.IsEnabled = true;
			ToggleProspectusLineActiveButton.Content = _selectedProspectusLine.IsActive ? "Archive" : "Restore";
			DeleteProspectusLineButton.IsEnabled = _selectedProspectusLine.IsDeletable;
		}

		private async void SaveProspectusLineButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				int programId = GetSelectedProspectusProgramId();

				if (programId <= 0)
				{
					throw new InvalidOperationException("Select a program before saving prospectus lines.");
				}

				string selectedGradeLevelName = ProspectusGradeLevelComboBox.SelectedValue as string ?? string.Empty;

				int selectedLearningAreaId = ProspectusLearningAreaComboBox.SelectedValue is int learningAreaId
					? learningAreaId
					: 0;

				AcademicsProgramProspectusLine line = _selectedProspectusLine ?? new AcademicsProgramProspectusLine();

				line.ProgramId = programId;
				line.GradeLevelName = selectedGradeLevelName;
				line.LearningAreaId = selectedLearningAreaId;
				line.TemplateTermSequence = ParsePositiveInt(ProspectusTermSequenceTextBox.Text, "Template term sequence");
				line.TemplateTermLabel = ProspectusTermLabelTextBox.Text;
				line.SortOrder = ParseSortOrder(ProspectusSortOrderTextBox.Text);
				line.IsActive = ProspectusIsActiveCheckBox.IsChecked == true;

				if (line.Id > 0)
				{
					await _prospectusLineRepository.UpdateAsync(line);
				}
				else
				{
					await _prospectusLineRepository.InsertAsync(line);
				}

				await LoadProspectusLinesForSelectedProgramAsync();
				ClearProspectusLineForm();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Unable to save prospectus line.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
					"Save Failed",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private async void ToggleProspectusLineActiveButton_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedProspectusLine == null)
			{
				return;
			}

			try
			{
				bool newActiveState = !_selectedProspectusLine.IsActive;

				await _prospectusLineRepository.SetIsActiveAsync(
					_selectedProspectusLine.Id,
					newActiveState);

				await LoadProspectusLinesForSelectedProgramAsync();
				ClearProspectusLineForm();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Unable to update prospectus line status.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
					"Update Failed",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private async void DeleteProspectusLineButton_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedProspectusLine == null)
			{
				return;
			}

			MessageBoxResult result = MessageBox.Show(
				$"Delete prospectus line '{_selectedProspectusLine.DisplayName}'?",
				"Confirm Delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (result != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				await _prospectusLineRepository.DeleteAsync(_selectedProspectusLine.Id);
				await LoadProspectusLinesForSelectedProgramAsync();
				ClearProspectusLineForm();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Unable to delete prospectus line.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
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

		private static int ParsePositiveInt(string value, string label)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return 1;
			}

			if (!int.TryParse(value, out int parsedValue))
			{
				throw new InvalidOperationException($"{label} must be a whole number.");
			}

			return Math.Max(1, parsedValue);
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

		private sealed class ProgramOptionRow
		{
			public ProgramOptionRow(int id, string displayName)
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

		private sealed class LearningAreaOptionRow
		{
			public LearningAreaOptionRow(int id, string displayName)
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

		private sealed class ProspectusLineRow
		{
			public ProspectusLineRow(AcademicsProgramProspectusLine line)
			{
				Line = line;
				DisplayName = line.DisplayName;
				DetailDisplay = BuildDetailDisplay(line);
			}

			public AcademicsProgramProspectusLine Line { get; }

			public string DisplayName { get; }
			public string DetailDisplay { get; }

			private static string BuildDetailDisplay(AcademicsProgramProspectusLine line)
			{
				List<string> parts = new();

				parts.Add($"Sort order: {line.SortOrder}");
				parts.Add(line.IsActive ? "Active" : "Archived");

				if (line.DependentRecordCount > 0)
				{
					parts.Add($"{line.DependentRecordCount} deployed line{(line.DependentRecordCount == 1 ? string.Empty : "s")}");
				}

				return string.Join(" • ", parts);
			}
		}

		private static string BuildLearningAreaDisplayName(LearningArea learningArea)
		{
			if (!string.IsNullOrWhiteSpace(learningArea.ShortName))
			{
				return $"{learningArea.ShortName} - {learningArea.Description}".Trim();
			}

			if (!string.IsNullOrWhiteSpace(learningArea.Description))
			{
				return learningArea.Description;
			}

			return learningArea.Code;
		}
	}
}
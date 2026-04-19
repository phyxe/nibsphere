using NibSphere.Core.Interfaces;
using NibSphere.Core.Models;
using NibSphere.Data.Repositories;
using System.Windows;
using System.Windows.Controls;

namespace NibSphere.Views
{
	public partial class LearningAreasView : UserControl
	{
		private readonly LearningAreaRepository _learningAreaRepository;
		private readonly AcademicGroupRepository _academicGroupRepository;
		private readonly LearningAreaCategoryRepository _learningAreaCategoryRepository;

		private LearningArea? _editingLearningArea;

		public LearningAreasView()
		{
			InitializeComponent();

			IAppPaths appPaths = App.AppPaths;
			_learningAreaRepository = new LearningAreaRepository(appPaths);
			_academicGroupRepository = new AcademicGroupRepository(appPaths);
			_learningAreaCategoryRepository = new LearningAreaCategoryRepository(appPaths);

			Loaded += LearningAreasView_Loaded;
		}

		private async void LearningAreasView_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= LearningAreasView_Loaded;

			await LoadLookupListsAsync();
			await LoadLearningAreasAsync();

			ClearEntryForm();
			SetSaveMode();
		}

		private async Task LoadLookupListsAsync()
		{
			AcademicGroupComboBox.ItemsSource = await _academicGroupRepository.GetAllAsync();
			CategoryComboBox.ItemsSource = await _learningAreaCategoryRepository.GetAllAsync();
		}

		private async Task LoadLearningAreasAsync()
		{
			LearningAreasDataGrid.ItemsSource = await _learningAreaRepository.GetAllAsync();
		}

		private async void SaveLearningAreaButton_Click(object sender, RoutedEventArgs e)
		{
			string code = CodeTextBox.Text.Trim();
			string shortName = ShortNameTextBox.Text.Trim();
			string description = DescriptionTextBox.Text.Trim();

			if (string.IsNullOrWhiteSpace(code))
			{
				MessageBox.Show(
					"Code is required.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				CodeTextBox.Focus();
				return;
			}

			if (string.IsNullOrWhiteSpace(shortName))
			{
				MessageBox.Show(
					"Short Name is required.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				ShortNameTextBox.Focus();
				return;
			}

			if (string.IsNullOrWhiteSpace(description))
			{
				MessageBox.Show(
					"Description is required.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				DescriptionTextBox.Focus();
				return;
			}

			if (AcademicGroupComboBox.SelectedItem is not AcademicGroup academicGroup)
			{
				MessageBox.Show(
					"Academic Group is required.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				AcademicGroupComboBox.Focus();
				return;
			}

			if (CategoryComboBox.SelectedItem is not LearningAreaCategory category)
			{
				MessageBox.Show(
					"Category is required.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				CategoryComboBox.Focus();
				return;
			}

			int sort = 0;
			if (!string.IsNullOrWhiteSpace(SortTextBox.Text) &&
				!int.TryParse(SortTextBox.Text.Trim(), out sort))
			{
				MessageBox.Show(
					"Sort must be a valid whole number.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				SortTextBox.Focus();
				return;
			}

			LearningArea learningArea = _editingLearningArea ?? new LearningArea();

			learningArea.Code = code;
			learningArea.ShortName = shortName;
			learningArea.Description = description;
			learningArea.AcademicGroupId = academicGroup.Id;
			learningArea.AcademicGroupName = academicGroup.Name;
			learningArea.CategoryId = category.Id;
			learningArea.CategoryName = category.Name;

			// Keep the legacy text field populated for compatibility.
			learningArea.Category = category.Name;
			learningArea.Sort = sort;

			if (_editingLearningArea == null)
			{
				await _learningAreaRepository.InsertAsync(learningArea);

				MessageBox.Show(
					"Learning area saved successfully.",
					"Saved",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}
			else
			{
				await _learningAreaRepository.UpdateAsync(learningArea);

				MessageBox.Show(
					"Learning area updated successfully.",
					"Updated",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}

			await LoadLearningAreasAsync();

			ClearEntryForm();
			SetSaveMode();
		}

		private void EditLearningAreaButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.Tag is not LearningArea learningArea)
			{
				return;
			}

			_editingLearningArea = learningArea;

			CodeTextBox.Text = learningArea.Code;
			ShortNameTextBox.Text = learningArea.ShortName;
			DescriptionTextBox.Text = learningArea.Description;
			SortTextBox.Text = learningArea.Sort.ToString();

			AcademicGroupComboBox.SelectedValue = learningArea.AcademicGroupId;
			CategoryComboBox.SelectedValue = learningArea.CategoryId;

			SetEditMode();
			CodeTextBox.Focus();
		}

		private async void DeleteLearningAreaButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.Tag is not LearningArea learningArea)
			{
				return;
			}

			MessageBoxResult result = MessageBox.Show(
				$"Delete learning area '{learningArea.Description}'?",
				"Confirm Delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (result != MessageBoxResult.Yes)
			{
				return;
			}

			await _learningAreaRepository.DeleteAsync(learningArea.Id);

			if (_editingLearningArea?.Id == learningArea.Id)
			{
				ClearEntryForm();
				SetSaveMode();
			}

			await LoadLearningAreasAsync();

			MessageBox.Show(
				"Learning area deleted successfully.",
				"Deleted",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void ManageListsButton_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show(
				"The lookup setup window for Academic Groups and Categories will be added next.",
				"Coming Next",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void ClearEntryForm()
		{
			_editingLearningArea = null;

			CodeTextBox.Clear();
			ShortNameTextBox.Clear();
			DescriptionTextBox.Clear();
			SortTextBox.Clear();

			AcademicGroupComboBox.SelectedItem = null;
			CategoryComboBox.SelectedItem = null;

			CodeTextBox.Focus();
		}

		private void SetSaveMode()
		{
			SaveLearningAreaActionTextBlock.Text = "Save Learning Area";
			SaveLearningAreaActionIcon.Source = "/Resources/Icons/save.svg";
			SaveLearningAreaButton.ToolTip = "Save Learning Area";
		}

		private void SetEditMode()
		{
			SaveLearningAreaActionTextBlock.Text = "Update Learning Area";
			SaveLearningAreaActionIcon.Source = "/Resources/Icons/edit.svg";
			SaveLearningAreaButton.ToolTip = "Update Learning Area";
		}
	}
}
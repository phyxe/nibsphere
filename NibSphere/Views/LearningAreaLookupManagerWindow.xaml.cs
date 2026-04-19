using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Models;
using NibSphere.Data.Repositories;
using System.Windows;
using System.Windows.Controls;

namespace NibSphere.Views
{
	public partial class LearningAreaLookupManagerWindow : Window
	{
		private readonly AcademicGroupRepository _academicGroupRepository;
		private readonly LearningAreaCategoryRepository _learningAreaCategoryRepository;

		private AcademicGroup? _editingAcademicGroup;
		private LearningAreaCategory? _editingCategory;
		private bool _hasChanges;

		public LearningAreaLookupManagerWindow()
		{
			InitializeComponent();

			IAppPaths appPaths = App.AppPaths;
			_academicGroupRepository = new AcademicGroupRepository(appPaths);
			_learningAreaCategoryRepository = new LearningAreaCategoryRepository(appPaths);

			Loaded += LearningAreaLookupManagerWindow_Loaded;
		}

		private async void LearningAreaLookupManagerWindow_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= LearningAreaLookupManagerWindow_Loaded;

			await LoadAcademicGroupsAsync();
			await LoadCategoriesAsync();

			ClearAcademicGroupEditForm();
			ClearCategoryEditForm();
		}

		private async Task LoadAcademicGroupsAsync()
		{
			AcademicGroupsDataGrid.ItemsSource = await _academicGroupRepository.GetAllAsync();
		}

		private async Task LoadCategoriesAsync()
		{
			CategoriesDataGrid.ItemsSource = await _learningAreaCategoryRepository.GetAllAsync();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = _hasChanges;
			Close();
		}

		private async void AddAcademicGroupsButton_Click(object sender, RoutedEventArgs e)
		{
			List<string> lines = ParseNonEmptyLines(AcademicGroupBulkTextBox.Text);

			if (lines.Count == 0)
			{
				MessageBox.Show(
					"Enter one or more academic groups. One line corresponds to one row.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				AcademicGroupBulkTextBox.Focus();
				return;
			}

			List<AcademicGroup> existingItems = await _academicGroupRepository.GetAllAsync();
			HashSet<string> knownNames = new(
				existingItems.Select(x => x.Name.Trim()),
				StringComparer.OrdinalIgnoreCase);

			int nextSort = existingItems.Count == 0 ? 0 : existingItems.Max(x => x.Sort);
			int addedCount = 0;
			int skippedCount = 0;

			foreach (string line in lines)
			{
				if (!knownNames.Add(line))
				{
					skippedCount++;
					continue;
				}

				AcademicGroup item = new()
				{
					Name = line,
					Sort = ++nextSort
				};

				await _academicGroupRepository.InsertAsync(item);
				addedCount++;
			}

			if (addedCount == 0)
			{
				MessageBox.Show(
					"All entered academic groups already exist.",
					"No New Items",
					MessageBoxButton.OK,
					MessageBoxImage.Information);

				return;
			}

			_hasChanges = true;
			await LoadAcademicGroupsAsync();
			AcademicGroupBulkTextBox.Clear();

			MessageBox.Show(
				skippedCount == 0
					? $"{addedCount} academic group(s) added."
					: $"{addedCount} academic group(s) added. {skippedCount} duplicate line(s) were skipped.",
				"Academic Groups Updated",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void ClearAcademicGroupBulkButton_Click(object sender, RoutedEventArgs e)
		{
			AcademicGroupBulkTextBox.Clear();
			AcademicGroupBulkTextBox.Focus();
		}

		private void EditAcademicGroupButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.Tag is not AcademicGroup item)
			{
				return;
			}

			_editingAcademicGroup = item;
			AcademicGroupNameTextBox.Text = item.Name;
			AcademicGroupSortTextBox.Text = item.Sort.ToString();
			AcademicGroupNameTextBox.Focus();
		}

		private async void SaveAcademicGroupEditButton_Click(object sender, RoutedEventArgs e)
		{
			if (_editingAcademicGroup == null)
			{
				MessageBox.Show(
					"Select an academic group to edit first.",
					"Nothing Selected",
					MessageBoxButton.OK,
					MessageBoxImage.Information);

				return;
			}

			string name = AcademicGroupNameTextBox.Text.Trim();

			if (string.IsNullOrWhiteSpace(name))
			{
				MessageBox.Show(
					"Name is required.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				AcademicGroupNameTextBox.Focus();
				return;
			}

			int sort = 0;
			if (!string.IsNullOrWhiteSpace(AcademicGroupSortTextBox.Text) &&
				!int.TryParse(AcademicGroupSortTextBox.Text.Trim(), out sort))
			{
				MessageBox.Show(
					"Sort must be a valid whole number.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				AcademicGroupSortTextBox.Focus();
				return;
			}

			List<AcademicGroup> existingItems = await _academicGroupRepository.GetAllAsync();
			bool duplicateExists = existingItems.Any(x =>
				x.Id != _editingAcademicGroup.Id &&
				string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

			if (duplicateExists)
			{
				MessageBox.Show(
					"An academic group with the same name already exists.",
					"Duplicate Name",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				AcademicGroupNameTextBox.Focus();
				return;
			}

			_editingAcademicGroup.Name = name;
			_editingAcademicGroup.Sort = sort;

			await _academicGroupRepository.UpdateAsync(_editingAcademicGroup);

			_hasChanges = true;
			await LoadAcademicGroupsAsync();
			ClearAcademicGroupEditForm();

			MessageBox.Show(
				"Academic group updated successfully.",
				"Updated",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void CancelAcademicGroupEditButton_Click(object sender, RoutedEventArgs e)
		{
			ClearAcademicGroupEditForm();
		}

		private async void DeleteAcademicGroupButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.Tag is not AcademicGroup item)
			{
				return;
			}

			MessageBoxResult result = MessageBox.Show(
				$"Delete academic group '{item.Name}'?",
				"Confirm Delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (result != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				await _academicGroupRepository.DeleteAsync(item.Id);

				_hasChanges = true;

				if (_editingAcademicGroup?.Id == item.Id)
				{
					ClearAcademicGroupEditForm();
				}

				await LoadAcademicGroupsAsync();

				MessageBox.Show(
					"Academic group deleted successfully.",
					"Deleted",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}
			catch (SqlException)
			{
				MessageBox.Show(
					"This academic group cannot be deleted because it is already used by one or more learning areas.",
					"Delete Blocked",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private async void AddCategoriesButton_Click(object sender, RoutedEventArgs e)
		{
			List<string> lines = ParseNonEmptyLines(CategoryBulkTextBox.Text);

			if (lines.Count == 0)
			{
				MessageBox.Show(
					"Enter one or more categories. One line corresponds to one row.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				CategoryBulkTextBox.Focus();
				return;
			}

			List<LearningAreaCategory> existingItems = await _learningAreaCategoryRepository.GetAllAsync();
			HashSet<string> knownNames = new(
				existingItems.Select(x => x.Name.Trim()),
				StringComparer.OrdinalIgnoreCase);

			int nextSort = existingItems.Count == 0 ? 0 : existingItems.Max(x => x.Sort);
			int addedCount = 0;
			int skippedCount = 0;

			foreach (string line in lines)
			{
				if (!knownNames.Add(line))
				{
					skippedCount++;
					continue;
				}

				LearningAreaCategory item = new()
				{
					Name = line,
					Sort = ++nextSort
				};

				await _learningAreaCategoryRepository.InsertAsync(item);
				addedCount++;
			}

			if (addedCount == 0)
			{
				MessageBox.Show(
					"All entered categories already exist.",
					"No New Items",
					MessageBoxButton.OK,
					MessageBoxImage.Information);

				return;
			}

			_hasChanges = true;
			await LoadCategoriesAsync();
			CategoryBulkTextBox.Clear();

			MessageBox.Show(
				skippedCount == 0
					? $"{addedCount} category item(s) added."
					: $"{addedCount} category item(s) added. {skippedCount} duplicate line(s) were skipped.",
				"Categories Updated",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void ClearCategoryBulkButton_Click(object sender, RoutedEventArgs e)
		{
			CategoryBulkTextBox.Clear();
			CategoryBulkTextBox.Focus();
		}

		private void EditCategoryButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.Tag is not LearningAreaCategory item)
			{
				return;
			}

			_editingCategory = item;
			CategoryNameTextBox.Text = item.Name;
			CategorySortTextBox.Text = item.Sort.ToString();
			CategoryNameTextBox.Focus();
		}

		private async void SaveCategoryEditButton_Click(object sender, RoutedEventArgs e)
		{
			if (_editingCategory == null)
			{
				MessageBox.Show(
					"Select a category to edit first.",
					"Nothing Selected",
					MessageBoxButton.OK,
					MessageBoxImage.Information);

				return;
			}

			string name = CategoryNameTextBox.Text.Trim();

			if (string.IsNullOrWhiteSpace(name))
			{
				MessageBox.Show(
					"Name is required.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				CategoryNameTextBox.Focus();
				return;
			}

			int sort = 0;
			if (!string.IsNullOrWhiteSpace(CategorySortTextBox.Text) &&
				!int.TryParse(CategorySortTextBox.Text.Trim(), out sort))
			{
				MessageBox.Show(
					"Sort must be a valid whole number.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				CategorySortTextBox.Focus();
				return;
			}

			List<LearningAreaCategory> existingItems = await _learningAreaCategoryRepository.GetAllAsync();
			bool duplicateExists = existingItems.Any(x =>
				x.Id != _editingCategory.Id &&
				string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

			if (duplicateExists)
			{
				MessageBox.Show(
					"A category with the same name already exists.",
					"Duplicate Name",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				CategoryNameTextBox.Focus();
				return;
			}

			_editingCategory.Name = name;
			_editingCategory.Sort = sort;

			await _learningAreaCategoryRepository.UpdateAsync(_editingCategory);

			_hasChanges = true;
			await LoadCategoriesAsync();
			ClearCategoryEditForm();

			MessageBox.Show(
				"Category updated successfully.",
				"Updated",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void CancelCategoryEditButton_Click(object sender, RoutedEventArgs e)
		{
			ClearCategoryEditForm();
		}

		private async void DeleteCategoryButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.Tag is not LearningAreaCategory item)
			{
				return;
			}

			MessageBoxResult result = MessageBox.Show(
				$"Delete category '{item.Name}'?",
				"Confirm Delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (result != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				await _learningAreaCategoryRepository.DeleteAsync(item.Id);

				_hasChanges = true;

				if (_editingCategory?.Id == item.Id)
				{
					ClearCategoryEditForm();
				}

				await LoadCategoriesAsync();

				MessageBox.Show(
					"Category deleted successfully.",
					"Deleted",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}
			catch (SqlException)
			{
				MessageBox.Show(
					"This category cannot be deleted because it is already used by one or more learning areas.",
					"Delete Blocked",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private void ClearAcademicGroupEditForm()
		{
			_editingAcademicGroup = null;
			AcademicGroupNameTextBox.Clear();
			AcademicGroupSortTextBox.Clear();
		}

		private void ClearCategoryEditForm()
		{
			_editingCategory = null;
			CategoryNameTextBox.Clear();
			CategorySortTextBox.Clear();
		}

		private static List<string> ParseNonEmptyLines(string input)
		{
			return input
				.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
				.Select(x => x.Trim())
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.ToList();
		}
	}
}
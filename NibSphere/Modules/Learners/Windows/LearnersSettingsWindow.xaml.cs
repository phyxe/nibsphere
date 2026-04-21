using Microsoft.Data.SqlClient;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Learners.Models;
using NibSphere.Core.Modules.Learners.Settings;
using NibSphere.Data.Modules.Learners.Repositories;
using NibSphere.Data.Modules.Learners.Settings;
using System.Windows;
using System.Windows.Controls;

namespace NibSphere.Modules.Learners.Windows
{
	public partial class LearnersSettingsWindow : Window
	{
		private readonly CustodianRoleRepository _custodianRoleRepository;
		private readonly LearnersSettingsStore _learnersSettingsStore;

		private CustodianRole? _editingCustodianRole;
		private LearnersLookupListItem? _editingPronoun;
		private LearnersLookupListItem? _editingReligiousAffiliation;

		private LearnersSettings _settings = LearnersSettings.CreateDefault();
		private bool _hasChanges;

		public LearnersSettingsWindow()
		{
			InitializeComponent();

			IAppPaths appPaths = App.AppPaths;
			_custodianRoleRepository = new CustodianRoleRepository(appPaths);
			_learnersSettingsStore = new LearnersSettingsStore(appPaths);

			Loaded += LearnersSettingsWindow_Loaded;
		}

		private async void LearnersSettingsWindow_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= LearnersSettingsWindow_Loaded;

			await LoadCustodianRolesAsync();
			await LoadSettingsAsync();

			ClearCustodianRoleEditForm();
			ClearPronounEditForm();
			ClearReligiousAffiliationEditForm();
		}

		private async Task LoadCustodianRolesAsync()
		{
			CustodianRolesDataGrid.ItemsSource = await _custodianRoleRepository.GetAllAsync();
		}

		private async Task LoadSettingsAsync()
		{
			_settings = await _learnersSettingsStore.GetAsync();

			PronounsDataGrid.ItemsSource = _settings.Pronouns
				.OrderBy(x => x.SortOrder)
				.ThenBy(x => x.Value, StringComparer.OrdinalIgnoreCase)
				.ToList();

			ReligiousAffiliationsDataGrid.ItemsSource = _settings.ReligiousAffiliations
				.OrderBy(x => x.SortOrder)
				.ThenBy(x => x.Value, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = _hasChanges;
			Close();
		}

		private async void AddCustodianRolesButton_Click(object sender, RoutedEventArgs e)
		{
			List<CustodianRole> existingItems = await _custodianRoleRepository.GetAllAsync();
			HashSet<string> knownKeys = new(
				existingItems.Select(BuildCustodianRoleKey),
				StringComparer.OrdinalIgnoreCase);

			int nextSort = existingItems.Count == 0 ? 0 : existingItems.Max(x => x.SortOrder);
			int addedCount = 0;
			int skippedCount = 0;

			foreach (CustodianRole role in ParseCustodianRoleLines(CustodianRoleBulkTextBox.Text))
			{
				string key = BuildCustodianRoleKey(role);

				if (!knownKeys.Add(key))
				{
					skippedCount++;
					continue;
				}

				if (role.SortOrder <= 0)
				{
					role.SortOrder = ++nextSort;
				}

				await _custodianRoleRepository.InsertAsync(role);
				addedCount++;
			}

			await LoadCustodianRolesAsync();
			CustodianRoleBulkTextBox.Clear();

			if (addedCount > 0)
			{
				_hasChanges = true;
			}

			MessageBox.Show(
				addedCount == 0
					? "No new custodian role entries were added."
					: skippedCount == 0
						? $"{addedCount} custodian role item(s) added."
						: $"{addedCount} custodian role item(s) added. {skippedCount} duplicate line(s) were skipped.",
				"Custodian Roles",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void ClearCustodianRoleBulkButton_Click(object sender, RoutedEventArgs e)
		{
			CustodianRoleBulkTextBox.Clear();
			CustodianRoleBulkTextBox.Focus();
		}

		private void EditCustodianRoleButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not CustodianRole role)
			{
				return;
			}

			_editingCustodianRole = role;
			CustodianRoleTypeTextBox.Text = role.RelationshipType;
			CustodianRoleLabelTextBox.Text = role.RelationshipLabel;
			CustodianRoleSortTextBox.Text = role.SortOrder.ToString();
			CustodianRoleTypeTextBox.Focus();
		}

		private async void SaveCustodianRoleEditButton_Click(object sender, RoutedEventArgs e)
		{
			if (_editingCustodianRole == null)
			{
				MessageBox.Show(
					"Select a custodian role item to edit first.",
					"Nothing Selected",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
				return;
			}

			string relationshipType = CustodianRoleTypeTextBox.Text.Trim();
			string relationshipLabel = CustodianRoleLabelTextBox.Text.Trim();

			if (string.IsNullOrWhiteSpace(relationshipType))
			{
				MessageBox.Show(
					"Relationship Type is required.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				CustodianRoleTypeTextBox.Focus();
				return;
			}

			if (string.IsNullOrWhiteSpace(relationshipLabel))
			{
				MessageBox.Show(
					"Relationship Label is required.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				CustodianRoleLabelTextBox.Focus();
				return;
			}

			int sortOrder = 0;
			if (!string.IsNullOrWhiteSpace(CustodianRoleSortTextBox.Text) &&
				!int.TryParse(CustodianRoleSortTextBox.Text.Trim(), out sortOrder))
			{
				MessageBox.Show(
					"Sort must be a valid whole number.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				CustodianRoleSortTextBox.Focus();
				return;
			}

			List<CustodianRole> existingItems = await _custodianRoleRepository.GetAllAsync();
			bool duplicateExists = existingItems.Any(x =>
				x.Id != _editingCustodianRole.Id &&
				string.Equals(x.RelationshipType, relationshipType, StringComparison.OrdinalIgnoreCase) &&
				string.Equals(x.RelationshipLabel, relationshipLabel, StringComparison.OrdinalIgnoreCase));

			if (duplicateExists)
			{
				MessageBox.Show(
					"An item with the same Relationship Type and Relationship Label already exists.",
					"Duplicate Entry",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return;
			}

			_editingCustodianRole.RelationshipType = relationshipType;
			_editingCustodianRole.RelationshipLabel = relationshipLabel;
			_editingCustodianRole.SortOrder = sortOrder;

			try
			{
				await _custodianRoleRepository.UpdateAsync(_editingCustodianRole);

				_hasChanges = true;
				await LoadCustodianRolesAsync();
				ClearCustodianRoleEditForm();

				MessageBox.Show(
					"Custodian role updated successfully.",
					"Updated",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}
			catch (SqlException ex)
			{
				MessageBox.Show(
					$"Unable to save this item.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
					"Save Failed",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private void CancelCustodianRoleEditButton_Click(object sender, RoutedEventArgs e)
		{
			ClearCustodianRoleEditForm();
		}

		private async void DeleteCustodianRoleButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not CustodianRole role)
			{
				return;
			}

			MessageBoxResult result = MessageBox.Show(
				$"Delete custodian role '{role.RelationshipType} | {role.RelationshipLabel}'?",
				"Confirm Delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (result != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				await _custodianRoleRepository.DeleteAsync(role.Id);

				_hasChanges = true;

				if (_editingCustodianRole?.Id == role.Id)
				{
					ClearCustodianRoleEditForm();
				}

				await LoadCustodianRolesAsync();

				MessageBox.Show(
					"Custodian role deleted successfully.",
					"Deleted",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}
			catch (SqlException ex)
			{
				MessageBox.Show(
					$"This custodian role cannot be deleted right now.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
					"Delete Blocked",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private async void AddPronounsButton_Click(object sender, RoutedEventArgs e)
		{
			int addedCount = AddLookupLines(_settings.Pronouns, PronounBulkTextBox.Text);
			await SaveAndReloadSettingsAsync();

			PronounBulkTextBox.Clear();

			MessageBox.Show(
				addedCount == 0
					? "No new pronoun entries were added."
					: $"{addedCount} pronoun item(s) added.",
				"Pronouns",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void ClearPronounBulkButton_Click(object sender, RoutedEventArgs e)
		{
			PronounBulkTextBox.Clear();
			PronounBulkTextBox.Focus();
		}

		private void EditPronounButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not LearnersLookupListItem item)
			{
				return;
			}

			_editingPronoun = item;
			PronounValueTextBox.Text = item.Value;
			PronounSortTextBox.Text = item.SortOrder.ToString();
			PronounValueTextBox.Focus();
		}

		private async void SavePronounEditButton_Click(object sender, RoutedEventArgs e)
		{
			if (_editingPronoun == null)
			{
				MessageBox.Show(
					"Select a pronoun item to edit first.",
					"Nothing Selected",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
				return;
			}

			string value = PronounValueTextBox.Text.Trim();

			if (string.IsNullOrWhiteSpace(value))
			{
				MessageBox.Show(
					"Value is required.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				PronounValueTextBox.Focus();
				return;
			}

			int sortOrder = 0;
			if (!string.IsNullOrWhiteSpace(PronounSortTextBox.Text) &&
				!int.TryParse(PronounSortTextBox.Text.Trim(), out sortOrder))
			{
				MessageBox.Show(
					"Sort must be a valid whole number.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				PronounSortTextBox.Focus();
				return;
			}

			bool duplicateExists = _settings.Pronouns.Any(x =>
				!ReferenceEquals(x, _editingPronoun) &&
				string.Equals(x.Value, value, StringComparison.OrdinalIgnoreCase));

			if (duplicateExists)
			{
				MessageBox.Show(
					"A pronoun item with the same value already exists.",
					"Duplicate Entry",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return;
			}

			_editingPronoun.Value = value;
			_editingPronoun.SortOrder = sortOrder;

			await SaveAndReloadSettingsAsync();
			ClearPronounEditForm();

			MessageBox.Show(
				"Pronoun item updated successfully.",
				"Updated",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void CancelPronounEditButton_Click(object sender, RoutedEventArgs e)
		{
			ClearPronounEditForm();
		}

		private async void DeletePronounButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not LearnersLookupListItem item)
			{
				return;
			}

			MessageBoxResult result = MessageBox.Show(
				$"Delete pronoun item '{item.Value}'?",
				"Confirm Delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (result != MessageBoxResult.Yes)
			{
				return;
			}

			_settings.Pronouns.Remove(item);

			_hasChanges = true;
			await SaveAndReloadSettingsAsync();

			if (ReferenceEquals(_editingPronoun, item))
			{
				ClearPronounEditForm();
			}

			MessageBox.Show(
				"Pronoun item deleted successfully.",
				"Deleted",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private async void AddReligiousAffiliationsButton_Click(object sender, RoutedEventArgs e)
		{
			int addedCount = AddLookupLines(_settings.ReligiousAffiliations, ReligiousAffiliationBulkTextBox.Text);
			await SaveAndReloadSettingsAsync();

			ReligiousAffiliationBulkTextBox.Clear();

			MessageBox.Show(
				addedCount == 0
					? "No new religious affiliation entries were added."
					: $"{addedCount} religious affiliation item(s) added.",
				"Religious Affiliations",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void ClearReligiousAffiliationBulkButton_Click(object sender, RoutedEventArgs e)
		{
			ReligiousAffiliationBulkTextBox.Clear();
			ReligiousAffiliationBulkTextBox.Focus();
		}

		private void EditReligiousAffiliationButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not LearnersLookupListItem item)
			{
				return;
			}

			_editingReligiousAffiliation = item;
			ReligiousAffiliationValueTextBox.Text = item.Value;
			ReligiousAffiliationSortTextBox.Text = item.SortOrder.ToString();
			ReligiousAffiliationValueTextBox.Focus();
		}

		private async void SaveReligiousAffiliationEditButton_Click(object sender, RoutedEventArgs e)
		{
			if (_editingReligiousAffiliation == null)
			{
				MessageBox.Show(
					"Select a religious affiliation item to edit first.",
					"Nothing Selected",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
				return;
			}

			string value = ReligiousAffiliationValueTextBox.Text.Trim();

			if (string.IsNullOrWhiteSpace(value))
			{
				MessageBox.Show(
					"Value is required.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				ReligiousAffiliationValueTextBox.Focus();
				return;
			}

			int sortOrder = 0;
			if (!string.IsNullOrWhiteSpace(ReligiousAffiliationSortTextBox.Text) &&
				!int.TryParse(ReligiousAffiliationSortTextBox.Text.Trim(), out sortOrder))
			{
				MessageBox.Show(
					"Sort must be a valid whole number.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				ReligiousAffiliationSortTextBox.Focus();
				return;
			}

			bool duplicateExists = _settings.ReligiousAffiliations.Any(x =>
				!ReferenceEquals(x, _editingReligiousAffiliation) &&
				string.Equals(x.Value, value, StringComparison.OrdinalIgnoreCase));

			if (duplicateExists)
			{
				MessageBox.Show(
					"A religious affiliation item with the same value already exists.",
					"Duplicate Entry",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return;
			}

			_editingReligiousAffiliation.Value = value;
			_editingReligiousAffiliation.SortOrder = sortOrder;

			await SaveAndReloadSettingsAsync();
			ClearReligiousAffiliationEditForm();

			MessageBox.Show(
				"Religious affiliation item updated successfully.",
				"Updated",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void CancelReligiousAffiliationEditButton_Click(object sender, RoutedEventArgs e)
		{
			ClearReligiousAffiliationEditForm();
		}

		private async void DeleteReligiousAffiliationButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not LearnersLookupListItem item)
			{
				return;
			}

			MessageBoxResult result = MessageBox.Show(
				$"Delete religious affiliation item '{item.Value}'?",
				"Confirm Delete",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (result != MessageBoxResult.Yes)
			{
				return;
			}

			_settings.ReligiousAffiliations.Remove(item);

			_hasChanges = true;
			await SaveAndReloadSettingsAsync();

			if (ReferenceEquals(_editingReligiousAffiliation, item))
			{
				ClearReligiousAffiliationEditForm();
			}

			MessageBox.Show(
				"Religious affiliation item deleted successfully.",
				"Deleted",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private async Task SaveAndReloadSettingsAsync()
		{
			_hasChanges = true;
			await _learnersSettingsStore.SaveAsync(_settings);
			await LoadSettingsAsync();
		}

		private static int AddLookupLines(List<LearnersLookupListItem> target, string input)
		{
			HashSet<string> knownValues = new(
				target.Select(x => x.Value.Trim()),
				StringComparer.OrdinalIgnoreCase);

			int nextSort = target.Count == 0 ? 0 : target.Max(x => x.SortOrder);
			int addedCount = 0;

			foreach (string line in ParseNonEmptyLines(input))
			{
				if (!knownValues.Add(line))
				{
					continue;
				}

				target.Add(new LearnersLookupListItem
				{
					Value = line,
					SortOrder = ++nextSort
				});

				addedCount++;
			}

			return addedCount;
		}

		private static List<CustodianRole> ParseCustodianRoleLines(string input)
		{
			List<CustodianRole> items = new();

			foreach (string line in ParseNonEmptyLines(input))
			{
				string[] parts = line
					.Split('|', 2, StringSplitOptions.TrimEntries);

				if (parts.Length < 2 ||
					string.IsNullOrWhiteSpace(parts[0]) ||
					string.IsNullOrWhiteSpace(parts[1]))
				{
					continue;
				}

				items.Add(new CustodianRole
				{
					RelationshipType = parts[0],
					RelationshipLabel = parts[1]
				});
			}

			return items;
		}

		private static string BuildCustodianRoleKey(CustodianRole role)
		{
			return $"{role.RelationshipType.Trim()}|{role.RelationshipLabel.Trim()}";
		}

		private void ClearCustodianRoleEditForm()
		{
			_editingCustodianRole = null;
			CustodianRoleTypeTextBox.Clear();
			CustodianRoleLabelTextBox.Clear();
			CustodianRoleSortTextBox.Clear();
		}

		private void ClearPronounEditForm()
		{
			_editingPronoun = null;
			PronounValueTextBox.Clear();
			PronounSortTextBox.Clear();
		}

		private void ClearReligiousAffiliationEditForm()
		{
			_editingReligiousAffiliation = null;
			ReligiousAffiliationValueTextBox.Clear();
			ReligiousAffiliationSortTextBox.Clear();
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
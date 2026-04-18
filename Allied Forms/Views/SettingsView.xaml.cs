using AFCore.Interfaces;
using AFCore.Models;
using AFData.Repositories;
using System.Windows;
using System.Windows.Controls;

namespace Allied_Forms.Views
{
	public partial class SettingsView : UserControl
	{
		private readonly SchoolProfileRepository _schoolProfileRepository;
		private SchoolProfile? _schoolProfile;
		private bool _isSchoolEditMode;
		private readonly AppUserProfileRepository _appUserProfileRepository;
		private AppUserProfile? _appUserProfile;
		private bool _isUserEditMode;
		private readonly LearningAreaRepository _learningAreaRepository;
		private LearningArea? _editingLearningArea;

		public SettingsView()
		{
			InitializeComponent();

			IAppPaths appPaths = App.AppPaths;
			_schoolProfileRepository = new SchoolProfileRepository(appPaths);
			_appUserProfileRepository = new AppUserProfileRepository(appPaths);
			_learningAreaRepository = new LearningAreaRepository(appPaths);

			Loaded += SettingsView_Loaded;
		}

		private async void SettingsView_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= SettingsView_Loaded;
			await LoadSchoolProfileAsync();
			await LoadUserProfileAsync();
			await LoadLearningAreasAsync();
		}

		private async Task LoadSchoolProfileAsync()
		{
			_schoolProfile = await _schoolProfileRepository.GetSchoolProfileAsync();

			if (_schoolProfile == null)
			{
				SetSchoolFieldsEditable(true);
				SetSchoolButtonToSaveMode();
				_isSchoolEditMode = true;
				return;
			}

			SchoolNameTextBox.Text = _schoolProfile.SchoolName;
			SchoolIdTextBox.Text = _schoolProfile.SchoolId ?? string.Empty;
			RegionTextBox.Text = _schoolProfile.Region ?? string.Empty;
			DivisionTextBox.Text = _schoolProfile.Division ?? string.Empty;
			DistrictTextBox.Text = _schoolProfile.District ?? string.Empty;
			SchoolHeadNameTextBox.Text = _schoolProfile.SchoolHeadName ?? string.Empty;
			SchoolHeadPositionTextBox.Text = _schoolProfile.SchoolHeadPosition ?? string.Empty;

			SetSchoolFieldsEditable(false);
			SetSchoolButtonToEditMode();
			_isSchoolEditMode = false;
		}

		private async void SaveSchoolSettingsButton_Click(object sender, RoutedEventArgs e)
		{
			if (!_isSchoolEditMode && _schoolProfile != null)
			{
				SetSchoolFieldsEditable(true);
				SetSchoolButtonToSaveMode();
				_isSchoolEditMode = true;
				return;
			}

			string schoolName = SchoolNameTextBox.Text.Trim();

			if (string.IsNullOrWhiteSpace(schoolName))
			{
				MessageBox.Show(
					"School Name is required.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				SchoolNameTextBox.Focus();
				return;
			}

			SchoolProfile schoolProfile = _schoolProfile ?? new SchoolProfile();

			schoolProfile.SchoolName = schoolName;
			schoolProfile.SchoolId = NullIfWhiteSpace(SchoolIdTextBox.Text);
			schoolProfile.Region = NullIfWhiteSpace(RegionTextBox.Text);
			schoolProfile.Division = NullIfWhiteSpace(DivisionTextBox.Text);
			schoolProfile.District = NullIfWhiteSpace(DistrictTextBox.Text);
			schoolProfile.SchoolHeadName = NullIfWhiteSpace(SchoolHeadNameTextBox.Text);
			schoolProfile.SchoolHeadPosition = NullIfWhiteSpace(SchoolHeadPositionTextBox.Text);

			if (_schoolProfile == null)
			{
				int newId = await _schoolProfileRepository.InsertSchoolProfileAsync(schoolProfile);
				schoolProfile.Id = newId;
				_schoolProfile = schoolProfile;
			}
			else
			{
				await _schoolProfileRepository.UpdateSchoolProfileAsync(schoolProfile);
				_schoolProfile = schoolProfile;
			}

			SetSchoolFieldsEditable(false);
			SetSchoolButtonToEditMode();
			_isSchoolEditMode = false;

			MessageBox.Show(
				"School settings saved successfully.",
				"Settings Saved",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void SetSchoolFieldsEditable(bool isEditable)
		{
			SchoolNameTextBox.IsReadOnly = !isEditable;
			SchoolIdTextBox.IsReadOnly = !isEditable;
			RegionTextBox.IsReadOnly = !isEditable;
			DivisionTextBox.IsReadOnly = !isEditable;
			DistrictTextBox.IsReadOnly = !isEditable;
			SchoolHeadNameTextBox.IsReadOnly = !isEditable;
			SchoolHeadPositionTextBox.IsReadOnly = !isEditable;
		}

		private void SetSchoolButtonToSaveMode()
		{
			SaveSchoolSettingsButton.ToolTip = "Save School Settings";
			SaveSchoolSettingsIcon.UriSource = new System.Uri("/Resources/Icons/save.svg", System.UriKind.Relative);
		}

		private void SetSchoolButtonToEditMode()
		{
			SaveSchoolSettingsButton.ToolTip = "Edit School Settings";
			SaveSchoolSettingsIcon.UriSource = new System.Uri("/Resources/Icons/edit.svg", System.UriKind.Relative);
		}

		private static string? NullIfWhiteSpace(string? value)
		{
			return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
		}

		private async Task LoadUserProfileAsync()
		{
			_appUserProfile = await _appUserProfileRepository.GetPrimaryUserProfileAsync();

			if (_appUserProfile == null)
			{
				SetUserFieldsEditable(true);
				SetUserButtonToSaveMode();
				_isUserEditMode = true;
				SetThemePreferenceSelection(null);
				return;
			}

			UserDefaultFullNameTextBox.Text = _appUserProfile.FullName;
			UserDefaultPositionTextBox.Text = _appUserProfile.PositionTitle ?? string.Empty;
			SetThemePreferenceSelection(_appUserProfile.ThemePreference);

			SetUserFieldsEditable(false);
			SetUserButtonToEditMode();
			_isUserEditMode = false;


		}

		private async void SaveUserSettingsButton_Click(object sender, RoutedEventArgs e)
		{
			if (!_isUserEditMode && _appUserProfile != null)
			{
				SetUserFieldsEditable(true);
				SetUserButtonToSaveMode();
				_isUserEditMode = true;
				return;
			}

			string fullName = UserDefaultFullNameTextBox.Text.Trim();

			if (string.IsNullOrWhiteSpace(fullName))
			{
				MessageBox.Show(
					"Complete Name is required.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				UserDefaultFullNameTextBox.Focus();
				return;
			}

			AppUserProfile userProfile = _appUserProfile ?? new AppUserProfile();

			userProfile.FullName = fullName;
			userProfile.PositionTitle = NullIfWhiteSpace(UserDefaultPositionTextBox.Text);
			userProfile.EmailAddress = null;
			userProfile.ContactNumber = null;
			userProfile.SignaturePath = null;
			userProfile.ThemePreference = GetSelectedThemePreference();
			userProfile.IsPrimary = true;

			if (_appUserProfile == null)
			{
				int newId = await _appUserProfileRepository.InsertPrimaryUserProfileAsync(userProfile);
				userProfile.Id = newId;
				_appUserProfile = userProfile;
			}
			else
			{
				await _appUserProfileRepository.UpdateUserProfileAsync(userProfile);
				_appUserProfile = userProfile;
			}

			SetUserFieldsEditable(false);
			SetUserButtonToEditMode();
			_isUserEditMode = false;

			MessageBox.Show(
				"User settings saved successfully.",
				"Settings Saved",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void SetUserFieldsEditable(bool isEditable)
		{
			UserDefaultFullNameTextBox.IsReadOnly = !isEditable;
			UserDefaultPositionTextBox.IsReadOnly = !isEditable;
			ThemePreferenceComboBox.IsEnabled = isEditable;
		}

		private void SetUserButtonToSaveMode()
		{
			SaveUserSettingsButton.ToolTip = "Save User Settings";
			SaveUserSettingsIcon.UriSource = new Uri("/Resources/Icons/save.svg", UriKind.Relative);
		}

		private void SetUserButtonToEditMode()
		{
			SaveUserSettingsButton.ToolTip = "Edit User Settings";
			SaveUserSettingsIcon.UriSource = new Uri("/Resources/Icons/edit.svg", UriKind.Relative);
		}

		private string GetSelectedThemePreference()
		{
			if (ThemePreferenceComboBox.SelectedItem is ComboBoxItem item &&
				item.Content is string value)
			{
				return value;
			}

			return "System";
		}

		private void SetThemePreferenceSelection(string? themePreference)
		{
			string target = string.IsNullOrWhiteSpace(themePreference)
				? "System"
				: themePreference;

			foreach (var item in ThemePreferenceComboBox.Items)
			{
				if (item is ComboBoxItem comboBoxItem &&
					string.Equals(comboBoxItem.Content as string, target, StringComparison.OrdinalIgnoreCase))
				{
					ThemePreferenceComboBox.SelectedItem = comboBoxItem;
					return;
				}
			}

			ThemePreferenceComboBox.SelectedIndex = 0;
		}

		private async Task LoadLearningAreasAsync()
		{
			LearningAreasDataGrid.ItemsSource = await _learningAreaRepository.GetAllAsync();
		}

		private async void SaveLearningAreaButton_Click(object sender, RoutedEventArgs e)
		{
			string category = LearningAreaCategoryTextBox.Text.Trim();
			string code = LearningAreaCodeTextBox.Text.Trim();
			string description = LearningAreaDescriptionTextBox.Text.Trim();

			if (string.IsNullOrWhiteSpace(category))
			{
				MessageBox.Show(
					"Category is required.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				LearningAreaCategoryTextBox.Focus();
				return;
			}

			if (string.IsNullOrWhiteSpace(code))
			{
				MessageBox.Show(
					"Code is required.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				LearningAreaCodeTextBox.Focus();
				return;
			}

			if (string.IsNullOrWhiteSpace(description))
			{
				MessageBox.Show(
					"Description is required.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				LearningAreaDescriptionTextBox.Focus();
				return;
			}

			int sort = 0;
			if (!string.IsNullOrWhiteSpace(LearningAreaSortTextBox.Text) &&
				!int.TryParse(LearningAreaSortTextBox.Text.Trim(), out sort))
			{
				MessageBox.Show(
					"Sort must be a valid whole number.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				LearningAreaSortTextBox.Focus();
				return;
			}

			if (_editingLearningArea == null)
			{
				LearningArea learningArea = new LearningArea
				{
					Category = category,
					Code = code,
					Description = description,
					Sort = sort
				};

				await _learningAreaRepository.InsertAsync(learningArea);

				MessageBox.Show(
					"Learning area saved successfully.",
					"Settings Saved",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}
			else
			{
				_editingLearningArea.Category = category;
				_editingLearningArea.Code = code;
				_editingLearningArea.Description = description;
				_editingLearningArea.Sort = sort;

				await _learningAreaRepository.UpdateAsync(_editingLearningArea);

				MessageBox.Show(
					"Learning area updated successfully.",
					"Settings Updated",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}

			await LoadLearningAreasAsync();
			ClearLearningAreaEntryFields();
			SetLearningAreaButtonToSaveMode();
		}

		private void EditLearningAreaButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.Tag is not LearningArea learningArea)
			{
				return;
			}

			_editingLearningArea = learningArea;

			LearningAreaCategoryTextBox.Text = learningArea.Category;
			LearningAreaCodeTextBox.Text = learningArea.Code;
			LearningAreaDescriptionTextBox.Text = learningArea.Description;
			LearningAreaSortTextBox.Text = learningArea.Sort.ToString();

			SetLearningAreaButtonToEditMode();
			LearningAreaCategoryTextBox.Focus();
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
				ClearLearningAreaEntryFields();
				SetLearningAreaButtonToSaveMode();
			}

			await LoadLearningAreasAsync();

			MessageBox.Show(
				"Learning area deleted successfully.",
				"Deleted",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void ClearLearningAreaEntryFields()
		{
			_editingLearningArea = null;
			LearningAreaCategoryTextBox.Clear();
			LearningAreaCodeTextBox.Clear();
			LearningAreaDescriptionTextBox.Clear();
			LearningAreaSortTextBox.Clear();
			LearningAreaCategoryTextBox.Focus();
		}

		private void SetLearningAreaButtonToSaveMode()
		{
			SaveLearningAreaButton.ToolTip = "Save Learning Area";
			SaveLearningAreaIcon.UriSource = new Uri("/Resources/Icons/save.svg", UriKind.Relative);
		}

		private void SetLearningAreaButtonToEditMode()
		{
			SaveLearningAreaButton.ToolTip = "Update Learning Area";
			SaveLearningAreaIcon.UriSource = new Uri("/Resources/Icons/edit.svg", UriKind.Relative);
		}
	}
}
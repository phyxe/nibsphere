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

		public SettingsView()
		{
			InitializeComponent();

			IAppPaths appPaths = App.AppPaths;
			_schoolProfileRepository = new SchoolProfileRepository(appPaths);
			_appUserProfileRepository = new AppUserProfileRepository(appPaths);

			Loaded += SettingsView_Loaded;
		}

		private async void SettingsView_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= SettingsView_Loaded;
			await LoadSchoolProfileAsync();
			await LoadUserProfileAsync();
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
				return;
			}

			UserDefaultFullNameTextBox.Text = _appUserProfile.FullName;
			UserDefaultPositionTextBox.Text = _appUserProfile.PositionTitle ?? string.Empty;

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
	}
}
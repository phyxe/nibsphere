using Microsoft.Win32;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Models;
using NibSphere.Data.Repositories;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NibSphere.Views
{
	public partial class UserProfileView : UserControl
	{
		private readonly IAppPaths _appPaths;
		private readonly AppUserProfileRepository _appUserProfileRepository;

		private AppUserProfile? _appUserProfile;
		private string? _pendingProfileImageSourcePath;
		private bool _removeProfileImage;
		private bool _isEditMode;

		public UserProfileView()
		{
			InitializeComponent();

			_appPaths = App.AppPaths;
			_appUserProfileRepository = new AppUserProfileRepository(_appPaths);

			Loaded += UserProfileView_Loaded;
		}

		private async void UserProfileView_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= UserProfileView_Loaded;
			await LoadUserProfileAsync();
		}

		private async Task LoadUserProfileAsync()
		{
			_appUserProfile = await _appUserProfileRepository.GetPrimaryUserProfileAsync();

			if (_appUserProfile == null)
			{
				_appUserProfile = new AppUserProfile
				{
					IsPrimary = true,
					ThemePreference = "System"
				};

				_isEditMode = true;
			}
			else
			{
				_isEditMode = false;
			}

			PopulateFields();
			UpdateHeaderPreview();
			UpdateProfileImagePreview();
			UpdateProfileImageStatusText();
			ApplyEditModeUi();
		}

		private void PopulateFields()
		{
			if (_appUserProfile == null)
			{
				return;
			}

			FirstNameTextBox.Text = _appUserProfile.FirstName ?? string.Empty;
			MiddleNameTextBox.Text = _appUserProfile.MiddleName ?? string.Empty;
			LastNameTextBox.Text = _appUserProfile.LastName ?? string.Empty;
			ExtensionNameTextBox.Text = _appUserProfile.ExtensionName ?? string.Empty;
			PositionTitleTextBox.Text = _appUserProfile.PositionTitle ?? string.Empty;
			ContactNumberTextBox.Text = _appUserProfile.ContactNumber ?? string.Empty;
			EmailAddressTextBox.Text = _appUserProfile.EmailAddress ?? string.Empty;

			SetThemePreferenceSelection(_appUserProfile.ThemePreference);
		}

		private async void SaveProfileButton_Click(object sender, RoutedEventArgs e)
		{
			if (!_isEditMode)
			{
				_isEditMode = true;
				ApplyEditModeUi();
				return;
			}

			if (_appUserProfile == null)
			{
				_appUserProfile = new AppUserProfile();
			}

			string firstName = FirstNameTextBox.Text.Trim();
			string lastName = LastNameTextBox.Text.Trim();

			if (string.IsNullOrWhiteSpace(firstName))
			{
				MessageBox.Show(
					"First Name is required.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				FirstNameTextBox.Focus();
				return;
			}

			if (string.IsNullOrWhiteSpace(lastName))
			{
				MessageBox.Show(
					"Last Name is required.",
					"Validation",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				LastNameTextBox.Focus();
				return;
			}

			_appUserProfile.IsPrimary = true;
			_appUserProfile.FirstName = firstName;
			_appUserProfile.MiddleName = NullIfWhiteSpace(MiddleNameTextBox.Text);
			_appUserProfile.LastName = lastName;
			_appUserProfile.ExtensionName = NullIfWhiteSpace(ExtensionNameTextBox.Text);
			_appUserProfile.PositionTitle = NullIfWhiteSpace(PositionTitleTextBox.Text);
			_appUserProfile.ContactNumber = NullIfWhiteSpace(ContactNumberTextBox.Text);
			_appUserProfile.EmailAddress = NullIfWhiteSpace(EmailAddressTextBox.Text);
			_appUserProfile.ThemePreference = GetSelectedThemePreference();

			ApplyProfileImageChanges();

			if (_appUserProfile.Id == 0)
			{
				int newId = await _appUserProfileRepository.InsertPrimaryUserProfileAsync(_appUserProfile);
				_appUserProfile.Id = newId;
			}
			else
			{
				await _appUserProfileRepository.UpdateUserProfileAsync(_appUserProfile);
			}

			_pendingProfileImageSourcePath = null;
			_removeProfileImage = false;
			_isEditMode = false;

			UpdateHeaderPreview();
			UpdateProfileImagePreview();
			UpdateProfileImageStatusText();
			ApplyEditModeUi();

			MessageBox.Show(
				"User profile saved successfully.",
				"Profile Saved",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void BrowseProfileImageButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog
			{
				Title = "Choose Profile Image",
				Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.webp",
				CheckFileExists = true,
				CheckPathExists = true,
				Multiselect = false
			};

			if (dialog.ShowDialog() != true)
			{
				return;
			}

			_pendingProfileImageSourcePath = dialog.FileName;
			_removeProfileImage = false;

			ProfileImagePathStatusTextBlock.Text = $"Selected image: {Path.GetFileName(dialog.FileName)}";
			UpdateProfileImagePreview();
		}

		private void RemoveProfileImageButton_Click(object sender, RoutedEventArgs e)
		{
			_pendingProfileImageSourcePath = null;
			_removeProfileImage = true;

			ProfileImagePathStatusTextBlock.Text = "Profile image will be removed on save.";
			UpdateProfileImagePreview();
		}

		private void ProfileFields_TextChanged(object sender, TextChangedEventArgs e)
		{
			UpdateHeaderPreview();
			UpdateProfileImagePreview();
		}

		private void UpdateHeaderPreview()
		{
			string displayName = BuildDisplayNameFromFields();

			ProfileHeaderNameTextBlock.Text = string.IsNullOrWhiteSpace(displayName)
				? "USER PROFILE"
				: displayName.ToUpperInvariant();

			ProfileHeaderPositionTextBlock.Text = PositionTitleTextBox.Text.Trim();
		}

		private void UpdateProfileImagePreview()
		{
			string? previewPath = null;

			if (!string.IsNullOrWhiteSpace(_pendingProfileImageSourcePath))
			{
				previewPath = _pendingProfileImageSourcePath;
			}
			else if (!_removeProfileImage && !string.IsNullOrWhiteSpace(_appUserProfile?.ProfileImagePath))
			{
				previewPath = GetAbsolutePath(_appUserProfile.ProfileImagePath);
			}

			if (!string.IsNullOrWhiteSpace(previewPath) && File.Exists(previewPath))
			{
				try
				{
					BitmapImage bitmap = new BitmapImage();
					bitmap.BeginInit();
					bitmap.CacheOption = BitmapCacheOption.OnLoad;
					bitmap.UriSource = new Uri(previewPath, UriKind.Absolute);
					bitmap.EndInit();
					bitmap.Freeze();

					ProfileImageEllipse.Fill = new ImageBrush(bitmap)
					{
						Stretch = Stretch.UniformToFill
					};

					ProfileInitialsTextBlock.Visibility = Visibility.Collapsed;
					return;
				}
				catch
				{
				}
			}

			ProfileImageEllipse.Fill = (Brush)FindResource("Brush.SurfaceAlt");
			ProfileInitialsTextBlock.Text = BuildInitials();
			ProfileInitialsTextBlock.Visibility = Visibility.Visible;
		}

		private void ApplyProfileImageChanges()
		{
			if (_appUserProfile == null)
			{
				return;
			}

			if (_removeProfileImage)
			{
				DeleteStoredProfileImageIfExists(_appUserProfile.ProfileImagePath);
				_appUserProfile.ProfileImagePath = null;
			}

			if (string.IsNullOrWhiteSpace(_pendingProfileImageSourcePath) || !File.Exists(_pendingProfileImageSourcePath))
			{
				return;
			}

			if (_appUserProfile.UserUid == null || _appUserProfile.UserUid == Guid.Empty)
			{
				_appUserProfile.UserUid = Guid.NewGuid();
			}

			string extension = Path.GetExtension(_pendingProfileImageSourcePath);
			string fileName = $"{_appUserProfile.UserUid}{extension}";
			string targetPath = Path.Combine(_appPaths.UserProfileImagesDirectory, fileName);

			Directory.CreateDirectory(_appPaths.UserProfileImagesDirectory);
			File.Copy(_pendingProfileImageSourcePath, targetPath, true);

			string relativePath = Path.GetRelativePath(_appPaths.RootDirectory, targetPath);
			relativePath = relativePath.Replace('/', '\\');

			string? oldAbsolutePath = GetAbsolutePath(_appUserProfile.ProfileImagePath);
			if (!string.IsNullOrWhiteSpace(oldAbsolutePath) &&
				!string.Equals(
					Path.GetFullPath(oldAbsolutePath),
					Path.GetFullPath(targetPath),
					StringComparison.OrdinalIgnoreCase) &&
				File.Exists(oldAbsolutePath))
			{
				File.Delete(oldAbsolutePath);
			}

			_appUserProfile.ProfileImagePath = relativePath;
		}

		private void DeleteStoredProfileImageIfExists(string? relativePath)
		{
			string? absolutePath = GetAbsolutePath(relativePath);

			if (!string.IsNullOrWhiteSpace(absolutePath) && File.Exists(absolutePath))
			{
				File.Delete(absolutePath);
			}
		}

		private string? GetAbsolutePath(string? relativePath)
		{
			if (string.IsNullOrWhiteSpace(relativePath))
			{
				return null;
			}

			return Path.Combine(_appPaths.RootDirectory, relativePath);
		}

		private string BuildDisplayNameFromFields()
		{
			AppUserProfile preview = new AppUserProfile
			{
				FirstName = NullIfWhiteSpace(FirstNameTextBox.Text),
				MiddleName = NullIfWhiteSpace(MiddleNameTextBox.Text),
				LastName = NullIfWhiteSpace(LastNameTextBox.Text),
				ExtensionName = NullIfWhiteSpace(ExtensionNameTextBox.Text)
			};

			return preview.BuildFullName();
		}

		private string BuildInitials()
		{
			string firstName = FirstNameTextBox.Text.Trim();
			string lastName = LastNameTextBox.Text.Trim();

			string initials = string.Concat(
				string.IsNullOrWhiteSpace(firstName) ? string.Empty : firstName[0].ToString(),
				string.IsNullOrWhiteSpace(lastName) ? string.Empty : lastName[0].ToString());

			if (string.IsNullOrWhiteSpace(initials))
			{
				return "U";
			}

			return initials.ToUpperInvariant();
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

			foreach (object item in ThemePreferenceComboBox.Items)
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

		private void ApplyEditModeUi()
		{
			SetTextBoxMode(FirstNameTextBox, _isEditMode);
			SetTextBoxMode(MiddleNameTextBox, _isEditMode);
			SetTextBoxMode(LastNameTextBox, _isEditMode);
			SetTextBoxMode(ExtensionNameTextBox, _isEditMode);
			SetTextBoxMode(PositionTitleTextBox, _isEditMode);
			SetTextBoxMode(ContactNumberTextBox, _isEditMode);
			SetTextBoxMode(EmailAddressTextBox, _isEditMode);
			SetComboBoxMode(ThemePreferenceComboBox, _isEditMode);

			ProfileImageButtonsPanel.Visibility = _isEditMode ? Visibility.Visible : Visibility.Collapsed;

			SaveProfileActionTextBlock.Text = _isEditMode ? "Save Profile" : "Edit Profile";
			SaveProfileActionIcon.Source = _isEditMode
				? "/Resources/Icons/save.svg"
				: "/Resources/Icons/edit.svg";
			SaveProfileButton.ToolTip = _isEditMode ? "Save Profile" : "Edit Profile";
		}

		private void SetTextBoxMode(TextBox textBox, bool isEditable)
		{
			textBox.IsReadOnly = !isEditable;
			textBox.IsHitTestVisible = isEditable;
			textBox.Focusable = isEditable;
			textBox.CaretBrush = isEditable ? (Brush)FindResource("Brush.PrimaryText") : Brushes.Transparent;
			textBox.Background = isEditable ? (Brush)FindResource("Brush.Surface") : Brushes.Transparent;
			textBox.BorderBrush = isEditable ? (Brush)FindResource("Brush.Border") : Brushes.Transparent;
			textBox.BorderThickness = isEditable ? new Thickness(1) : new Thickness(0);
			textBox.Padding = isEditable ? new Thickness(10, 6, 10, 6) : new Thickness(0);
		}

		private void SetComboBoxMode(ComboBox comboBox, bool isEditable)
		{
			comboBox.IsHitTestVisible = isEditable;
			comboBox.Focusable = isEditable;
			comboBox.Background = isEditable ? (Brush)FindResource("Brush.Surface") : Brushes.Transparent;
			comboBox.BorderBrush = isEditable ? (Brush)FindResource("Brush.Border") : Brushes.Transparent;
			comboBox.BorderThickness = isEditable ? new Thickness(1) : new Thickness(0);
			comboBox.Padding = isEditable ? new Thickness(10, 6, 10, 6) : new Thickness(0);
		}

		private void UpdateProfileImageStatusText()
		{
			if (_removeProfileImage)
			{
				ProfileImagePathStatusTextBlock.Text = "Profile image will be removed on save.";
				return;
			}

			if (!string.IsNullOrWhiteSpace(_pendingProfileImageSourcePath))
			{
				ProfileImagePathStatusTextBlock.Text = $"Selected image: {Path.GetFileName(_pendingProfileImageSourcePath)}";
				return;
			}

			ProfileImagePathStatusTextBlock.Text = string.IsNullOrWhiteSpace(_appUserProfile?.ProfileImagePath)
				? "No profile image selected."
				: "Profile image selected.";
		}

		private static string? NullIfWhiteSpace(string? value)
		{
			return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
		}
	}
}
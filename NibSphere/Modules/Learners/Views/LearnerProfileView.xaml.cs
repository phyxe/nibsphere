using Microsoft.Win32;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Learners.Models;
using NibSphere.Core.Modules.Learners.Profile;
using NibSphere.Core.Modules.Learners.Settings;
using NibSphere.Data.Modules.Learners.Profile;
using NibSphere.Data.Modules.Learners.Repositories;
using NibSphere.Data.Modules.Learners.Settings;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NibSphere.Modules.Learners.Views
{
	public partial class LearnerProfileView : UserControl, INotifyPropertyChanged
	{
		private readonly IAppPaths _appPaths;
		private readonly LearnerProfileService _learnerProfileService;
		private readonly CustodianRoleRepository _custodianRoleRepository;
		private readonly LearnersSettingsStore _learnersSettingsStore;

		private LearnerProfileRecord? _profile;
		private readonly LearnerProfileMode _requestedMode;
		private int? _learnerId;

		private string? _pendingProfileImageSourcePath;
		private bool _removeProfileImage;

		private IReadOnlyList<string> _relationshipTypeOptions = Array.Empty<string>();
		private IReadOnlyList<string> _relationshipLabelOptions = Array.Empty<string>();
		private IReadOnlyList<string> _pronounOptions = Array.Empty<string>();
		private IReadOnlyList<string> _religiousAffiliationOptions = Array.Empty<string>();

		public ObservableCollection<LearnerCustodianCardItem> CustodianCards { get; } = new();

		public IReadOnlyList<string> SexOptions { get; } = new[] { "Male", "Female" };

		public IReadOnlyList<string> RelationshipTypeOptions
		{
			get => _relationshipTypeOptions;
			private set
			{
				_relationshipTypeOptions = value;
				OnPropertyChanged();
			}
		}

		public IReadOnlyList<string> RelationshipLabelOptions
		{
			get => _relationshipLabelOptions;
			private set
			{
				_relationshipLabelOptions = value;
				OnPropertyChanged();
			}
		}

		public IReadOnlyList<string> PronounOptions
		{
			get => _pronounOptions;
			private set
			{
				_pronounOptions = value;
				OnPropertyChanged();
			}
		}

		public IReadOnlyList<string> ReligiousAffiliationOptions
		{
			get => _religiousAffiliationOptions;
			private set
			{
				_religiousAffiliationOptions = value;
				OnPropertyChanged();
			}
		}

		public bool IsEditMode => _profile != null && _profile.Mode != LearnerProfileMode.View;

		public bool IsViewMode => !IsEditMode;

		public Visibility EditOnlyVisibility => IsEditMode ? Visibility.Visible : Visibility.Collapsed;

		public event PropertyChangedEventHandler? PropertyChanged;

		public LearnerProfileView(LearnerProfileMode mode, int? learnerId = null)
		{
			InitializeComponent();
			DataContext = this;

			_requestedMode = mode;
			_learnerId = learnerId;

			_appPaths = App.AppPaths;
			_learnerProfileService = new LearnerProfileService(_appPaths);
			_custodianRoleRepository = new CustodianRoleRepository(_appPaths);
			_learnersSettingsStore = new LearnersSettingsStore(_appPaths);

			Loaded += LearnerProfileView_Loaded;
		}

		private async void LearnerProfileView_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= LearnerProfileView_Loaded;

			await LoadReferenceDataAsync();
			await LoadProfileAsync();
		}

		private async Task LoadReferenceDataAsync()
		{
			List<CustodianRole> roles = await _custodianRoleRepository.GetAllAsync();
			LearnersSettings settings = await _learnersSettingsStore.GetAsync();

			RelationshipTypeOptions = roles
				.Select(x => x.RelationshipType?.Trim() ?? string.Empty)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
				.ToList();

			RelationshipLabelOptions = roles
				.Select(x => x.RelationshipLabel?.Trim() ?? string.Empty)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
				.ToList();

			PronounOptions = settings.Pronouns
				.OrderBy(x => x.SortOrder)
				.ThenBy(x => x.Value, StringComparer.OrdinalIgnoreCase)
				.Select(x => x.Value)
				.ToList();

			ReligiousAffiliationOptions = settings.ReligiousAffiliations
				.OrderBy(x => x.SortOrder)
				.ThenBy(x => x.Value, StringComparer.OrdinalIgnoreCase)
				.Select(x => x.Value)
				.ToList();
		}

		private async Task LoadProfileAsync()
		{
			if (_learnerId.HasValue && _learnerId.Value > 0)
			{
				_profile = await _learnerProfileService.GetByLearnerIdAsync(
					_learnerId.Value,
					_requestedMode);

				if (_profile == null)
				{
					MessageBox.Show(
						"Learner profile not found.",
						"Learners",
						MessageBoxButton.OK,
						MessageBoxImage.Warning);

					NavigateBackToLearnersList();
					return;
				}
			}
			else
			{
				_profile = _learnerProfileService.CreateNew();
			}

			_pendingProfileImageSourcePath = null;
			_removeProfileImage = false;

			PopulateFields();
			SyncCustodianCards();
			UpdateHeaderPreview();
			UpdateProfileImagePreview();
			UpdateProfileImageStatusText();
			ApplyModeUi();
		}

		private void PopulateFields()
		{
			if (_profile == null)
			{
				return;
			}

			Learner learner = _profile.Learner;

			FirstNameTextBox.Text = learner.FirstName;
			MiddleNameTextBox.Text = learner.MiddleName;
			LastNameTextBox.Text = learner.LastName;
			ExtensionNameTextBox.Text = learner.ExtensionName;
			LrnTextBox.Text = learner.Lrn;
			BirthdayDatePicker.SelectedDate = learner.Birthday;
			SexComboBox.SelectedItem = string.IsNullOrWhiteSpace(learner.Sex) ? null : learner.Sex;
			PronounComboBox.SelectedItem = string.IsNullOrWhiteSpace(learner.Pronoun) ? null : learner.Pronoun;
			PronounComboBox.Text = learner.Pronoun;
			ReligiousAffiliationComboBox.SelectedItem = string.IsNullOrWhiteSpace(learner.ReligiousAffiliation) ? null : learner.ReligiousAffiliation;
			ReligiousAffiliationComboBox.Text = learner.ReligiousAffiliation;
			MobileNumberTextBox.Text = learner.MobileNumber;
			EmailAddressTextBox.Text = learner.Email;

			HouseStreetTextBox.Text = learner.HouseStreetSitioPurok;
			BarangayTextBox.Text = learner.Barangay;
			MunicipalityTextBox.Text = learner.Municipality;
			ProvinceTextBox.Text = learner.Province;
		}

		private void SyncCustodianCards()
		{
			CustodianCards.Clear();

			if (_profile == null)
			{
				return;
			}

			foreach (LearnerCustodianCardItem card in _profile.Custodians
				.OrderBy(x => x.SortOrder)
				.ThenBy(x => x.RelationshipType, StringComparer.OrdinalIgnoreCase)
				.ThenBy(x => x.RelationshipLabel, StringComparer.OrdinalIgnoreCase))
			{
				CustodianCards.Add(card);
			}
		}

		private async void SaveProfileButton_Click(object sender, RoutedEventArgs e)
		{
			if (_profile == null)
			{
				return;
			}

			if (!IsEditMode)
			{
				_profile.Mode = LearnerProfileMode.Edit;
				ApplyModeUi();
				return;
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

			ReadFieldsToProfile();
			ApplyProfileImageChanges();

			try
			{
				int learnerId = await _learnerProfileService.SaveAsync(_profile);
				_learnerId = learnerId;

				_profile = await _learnerProfileService.GetByLearnerIdAsync(
					learnerId,
					LearnerProfileMode.View);

				_pendingProfileImageSourcePath = null;
				_removeProfileImage = false;

				PopulateFields();
				SyncCustodianCards();
				UpdateHeaderPreview();
				UpdateProfileImagePreview();
				UpdateProfileImageStatusText();
				ApplyModeUi();

				MessageBox.Show(
					"Learner profile saved successfully.",
					"Learners",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Unable to save learner profile.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
					"Save Failed",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		private async void CancelEditButton_Click(object sender, RoutedEventArgs e)
		{
			if (_profile == null)
			{
				return;
			}

			if (_profile.Mode == LearnerProfileMode.Add)
			{
				NavigateBackToLearnersList();
				return;
			}

			if (_learnerId.HasValue && _learnerId.Value > 0)
			{
				_profile = await _learnerProfileService.GetByLearnerIdAsync(
					_learnerId.Value,
					LearnerProfileMode.View);

				_pendingProfileImageSourcePath = null;
				_removeProfileImage = false;

				PopulateFields();
				SyncCustodianCards();
				UpdateHeaderPreview();
				UpdateProfileImagePreview();
				UpdateProfileImageStatusText();
				ApplyModeUi();
			}
		}

		private void BackProfileButton_Click(object sender, RoutedEventArgs e)
		{
			NavigateBackToLearnersList();
		}

		private void AddCustodianCardButton_Click(object sender, RoutedEventArgs e)
		{
			if (!IsEditMode)
			{
				return;
			}

			CustodianCards.Add(new LearnerCustodianCardItem
			{
				SortOrder = CustodianCards.Count + 1
			});
		}

		private void RemoveCustodianCardButton_Click(object sender, RoutedEventArgs e)
		{
			if (!IsEditMode)
			{
				return;
			}

			if (sender is not Button button ||
				button.Tag is not LearnerCustodianCardItem card)
			{
				return;
			}

			CustodianCards.Remove(card);
			ResetCustodianCardSortOrders();
		}

		private void BrowseProfileImageButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new()
			{
				Title = "Choose Learner Profile Image",
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

			UpdateProfileImagePreview();
			UpdateProfileImageStatusText();
		}

		private void RemoveProfileImageButton_Click(object sender, RoutedEventArgs e)
		{
			_pendingProfileImageSourcePath = null;
			_removeProfileImage = true;

			UpdateProfileImagePreview();
			UpdateProfileImageStatusText();
		}

		private void LearnerFields_TextChanged(object sender, TextChangedEventArgs e)
		{
			UpdateHeaderPreview();
			UpdateProfileImagePreview();
		}

		private void ReadFieldsToProfile()
		{
			if (_profile == null)
			{
				return;
			}

			Learner learner = _profile.Learner;

			learner.FirstName = FirstNameTextBox.Text.Trim();
			learner.MiddleName = MiddleNameTextBox.Text.Trim();
			learner.LastName = LastNameTextBox.Text.Trim();
			learner.ExtensionName = ExtensionNameTextBox.Text.Trim();
			learner.Lrn = LrnTextBox.Text.Trim();
			learner.Birthday = BirthdayDatePicker.SelectedDate;
			learner.Sex = SexComboBox.SelectedItem as string ?? SexComboBox.Text.Trim();
			learner.Pronoun = PronounComboBox.Text.Trim();
			learner.ReligiousAffiliation = ReligiousAffiliationComboBox.Text.Trim();
			learner.MobileNumber = MobileNumberTextBox.Text.Trim();
			learner.Email = EmailAddressTextBox.Text.Trim();
			learner.HouseStreetSitioPurok = HouseStreetTextBox.Text.Trim();
			learner.Barangay = BarangayTextBox.Text.Trim();
			learner.Municipality = MunicipalityTextBox.Text.Trim();
			learner.Province = ProvinceTextBox.Text.Trim();

			ResetCustodianCardSortOrders();
			_profile.Custodians = CustodianCards.ToList();
		}

		private void ResetCustodianCardSortOrders()
		{
			for (int index = 0; index < CustodianCards.Count; index++)
			{
				CustodianCards[index].SortOrder = index + 1;
			}
		}

		private void UpdateHeaderPreview()
		{
			if (_profile == null)
			{
				return;
			}

			Learner preview = new()
			{
				FirstName = FirstNameTextBox.Text.Trim(),
				MiddleName = MiddleNameTextBox.Text.Trim(),
				LastName = LastNameTextBox.Text.Trim(),
				ExtensionName = ExtensionNameTextBox.Text.Trim()
			};

			LearnerProfileRecord previewRecord = new()
			{
				Learner = preview
			};

			ProfileHeaderNameTextBlock.Text = previewRecord.BuildHeaderDisplayName();

			string lrn = LrnTextBox.Text.Trim();
			ProfileHeaderMetaTextBlock.Text = string.IsNullOrWhiteSpace(lrn)
				? "LRN: -"
				: $"LRN: {lrn}";
		}

		private void UpdateProfileImagePreview()
		{
			string? previewPath = null;

			if (!string.IsNullOrWhiteSpace(_pendingProfileImageSourcePath))
			{
				previewPath = _pendingProfileImageSourcePath;
			}
			else if (!_removeProfileImage &&
					 _profile != null &&
					 !string.IsNullOrWhiteSpace(_profile.Learner.ProfilePicturePath))
			{
				previewPath = GetAbsolutePath(_profile.Learner.ProfilePicturePath);
			}

			if (!string.IsNullOrWhiteSpace(previewPath) && File.Exists(previewPath))
			{
				try
				{
					BitmapImage bitmap = new();
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
			if (_profile == null)
			{
				return;
			}

			if (_removeProfileImage)
			{
				DeleteStoredProfileImageIfExists(_profile.Learner.ProfilePicturePath);
				_profile.Learner.ProfilePicturePath = string.Empty;
			}

			if (string.IsNullOrWhiteSpace(_pendingProfileImageSourcePath) ||
				!File.Exists(_pendingProfileImageSourcePath))
			{
				return;
			}

			string targetDirectory = Path.Combine(
				_appPaths.ModuleImagesDirectory,
				"Learners",
				"ProfileImages");

			Directory.CreateDirectory(targetDirectory);

			string extension = Path.GetExtension(_pendingProfileImageSourcePath);
			string fileName = $"{Guid.NewGuid()}{extension}";
			string targetPath = Path.Combine(targetDirectory, fileName);

			File.Copy(_pendingProfileImageSourcePath, targetPath, true);

			string relativePath = Path.GetRelativePath(_appPaths.RootDirectory, targetPath)
				.Replace('/', '\\');

			string? oldAbsolutePath = GetAbsolutePath(_profile.Learner.ProfilePicturePath);
			if (!string.IsNullOrWhiteSpace(oldAbsolutePath) &&
				!string.Equals(
					Path.GetFullPath(oldAbsolutePath),
					Path.GetFullPath(targetPath),
					StringComparison.OrdinalIgnoreCase) &&
				File.Exists(oldAbsolutePath))
			{
				File.Delete(oldAbsolutePath);
			}

			_profile.Learner.ProfilePicturePath = relativePath;
		}

		private void DeleteStoredProfileImageIfExists(string? relativePath)
		{
			string? absolutePath = GetAbsolutePath(relativePath);

			if (!string.IsNullOrWhiteSpace(absolutePath) &&
				File.Exists(absolutePath))
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

		private string BuildInitials()
		{
			string firstName = FirstNameTextBox.Text.Trim();
			string lastName = LastNameTextBox.Text.Trim();

			string initials = string.Concat(
				string.IsNullOrWhiteSpace(firstName) ? string.Empty : firstName[0].ToString(),
				string.IsNullOrWhiteSpace(lastName) ? string.Empty : lastName[0].ToString());

			if (string.IsNullOrWhiteSpace(initials))
			{
				return "L";
			}

			return initials.ToUpperInvariant();
		}

		private void ApplyModeUi()
		{
			OnPropertyChanged(nameof(IsEditMode));
			OnPropertyChanged(nameof(IsViewMode));
			OnPropertyChanged(nameof(EditOnlyVisibility));

			bool isEditable = IsEditMode;

			SetTextBoxMode(FirstNameTextBox, isEditable);
			SetTextBoxMode(MiddleNameTextBox, isEditable);
			SetTextBoxMode(LastNameTextBox, isEditable);
			SetTextBoxMode(ExtensionNameTextBox, isEditable);
			SetTextBoxMode(LrnTextBox, isEditable);
			SetComboBoxMode(SexComboBox, isEditable);
			SetComboBoxMode(PronounComboBox, isEditable);
			SetComboBoxMode(ReligiousAffiliationComboBox, isEditable);
			SetTextBoxMode(MobileNumberTextBox, isEditable);
			SetTextBoxMode(EmailAddressTextBox, isEditable);
			SetTextBoxMode(HouseStreetTextBox, isEditable);
			SetTextBoxMode(BarangayTextBox, isEditable);
			SetTextBoxMode(MunicipalityTextBox, isEditable);
			SetTextBoxMode(ProvinceTextBox, isEditable);
			SetDatePickerMode(BirthdayDatePicker, isEditable);

			ProfileImageButtonsPanel.Visibility = isEditable
				? Visibility.Visible
				: Visibility.Collapsed;

			CancelEditButton.Visibility = isEditable
				? Visibility.Visible
				: Visibility.Collapsed;

			if (_profile == null)
			{
				ProfilePageTitleTextBlock.Text = "Learner Profile";
				SaveProfileActionTextBlock.Text = "Save Learner";
				return;
			}

			ProfilePageTitleTextBlock.Text = _profile.Mode switch
			{
				LearnerProfileMode.Add => "Add Learner",
				LearnerProfileMode.Edit => "Edit Learner",
				_ => "Learner Profile"
			};

			SaveProfileActionTextBlock.Text = _profile.Mode switch
			{
				LearnerProfileMode.View => "Edit Learner",
				_ => "Save Learner"
			};
		}

		private void SetTextBoxMode(TextBox textBox, bool isEditable)
		{
			textBox.IsReadOnly = !isEditable;
			textBox.IsHitTestVisible = isEditable;
			textBox.Focusable = isEditable;

			if (isEditable)
			{
				textBox.SetResourceReference(TextBox.BackgroundProperty, "Brush.Surface");
				textBox.SetResourceReference(TextBox.BorderBrushProperty, "Brush.Border");
				textBox.SetResourceReference(TextBox.ForegroundProperty, "Brush.PrimaryText");
				textBox.SetResourceReference(TextBoxBase.CaretBrushProperty, "Brush.PrimaryText");

				textBox.BorderThickness = new Thickness(1);
				textBox.Padding = new Thickness(10, 6, 10, 6);
			}
			else
			{
				textBox.Background = Brushes.Transparent;
				textBox.BorderBrush = Brushes.Transparent;
				textBox.CaretBrush = Brushes.Transparent;

				textBox.ClearValue(TextBox.ForegroundProperty);

				textBox.BorderThickness = new Thickness(0);
				textBox.Padding = new Thickness(0);
			}
		}

		private void SetComboBoxMode(ComboBox comboBox, bool isEditable)
		{
			comboBox.IsHitTestVisible = isEditable;
			comboBox.Focusable = isEditable;

			if (isEditable)
			{
				comboBox.SetResourceReference(ComboBox.BackgroundProperty, "Brush.Surface");
				comboBox.SetResourceReference(ComboBox.BorderBrushProperty, "Brush.Border");
				comboBox.SetResourceReference(ComboBox.ForegroundProperty, "Brush.PrimaryText");

				comboBox.BorderThickness = new Thickness(1);
				comboBox.Padding = new Thickness(10, 6, 10, 6);
			}
			else
			{
				comboBox.Background = Brushes.Transparent;
				comboBox.BorderBrush = Brushes.Transparent;

				comboBox.ClearValue(ComboBox.ForegroundProperty);

				comboBox.BorderThickness = new Thickness(0);
				comboBox.Padding = new Thickness(0);
			}
		}

		private void SetDatePickerMode(DatePicker datePicker, bool isEditable)
		{
			datePicker.IsHitTestVisible = isEditable;
			datePicker.Focusable = isEditable;
			datePicker.IsEnabled = isEditable;

			if (isEditable)
			{
				datePicker.SetResourceReference(DatePicker.BackgroundProperty, "Brush.Surface");
				datePicker.SetResourceReference(DatePicker.BorderBrushProperty, "Brush.Border");
				datePicker.SetResourceReference(DatePicker.ForegroundProperty, "Brush.PrimaryText");
			}
			else
			{
				datePicker.Background = Brushes.Transparent;
				datePicker.BorderBrush = Brushes.Transparent;
				datePicker.ClearValue(DatePicker.ForegroundProperty);
			}
		}

		private void UpdateProfileImageStatusText()
		{
			if (_removeProfileImage)
			{
				ProfileImageStatusTextBlock.Text = "Profile image will be removed on save.";
				ProfileImageStatusTextBlock.Visibility = Visibility.Visible;
				return;
			}

			if (!string.IsNullOrWhiteSpace(_pendingProfileImageSourcePath))
			{
				ProfileImageStatusTextBlock.Text = $"Selected image: {Path.GetFileName(_pendingProfileImageSourcePath)}";
				ProfileImageStatusTextBlock.Visibility = Visibility.Visible;
				return;
			}

			if (_profile == null ||
				string.IsNullOrWhiteSpace(_profile.Learner.ProfilePicturePath))
			{
				ProfileImageStatusTextBlock.Text = "No profile image selected.";
				ProfileImageStatusTextBlock.Visibility = Visibility.Visible;
				return;
			}

			ProfileImageStatusTextBlock.Text = string.Empty;
			ProfileImageStatusTextBlock.Visibility = Visibility.Collapsed;
		}

		private void NavigateBackToLearnersList()
		{
			if (Window.GetWindow(this) is MainWindow mainWindow)
			{
				mainWindow.ShowContent(new LearnersListView());
			}
		}

		private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
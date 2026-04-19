using Microsoft.Win32;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Models;
using NibSphere.Core.ReferenceData.Models;
using NibSphere.Data.ReferenceData;
using NibSphere.Data.Repositories;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NibSphere.Views
{
	public partial class SchoolProfileView : UserControl
	{
		private readonly IAppPaths _appPaths;
		private readonly SchoolProfileRepository _schoolProfileRepository;
		private readonly PhilippineAddressRepository _philippineAddressRepository;

		private SchoolProfile? _schoolProfile;
		private string? _pendingSchoolLogoSourcePath;
		private bool _removeSchoolLogo;
		private bool _isEditMode;
		private bool _isAddressSelectionLoading;

		public SchoolProfileView()
		{
			InitializeComponent();

			_appPaths = App.AppPaths;
			_schoolProfileRepository = new SchoolProfileRepository(_appPaths);
			_philippineAddressRepository = new PhilippineAddressRepository(_appPaths);

			Loaded += SchoolProfileView_Loaded;
		}

		private async void SchoolProfileView_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= SchoolProfileView_Loaded;
			await LoadSchoolProfileAsync();
		}

		private async Task LoadSchoolProfileAsync()
		{
			_schoolProfile = await _schoolProfileRepository.GetSchoolProfileAsync();

			if (_schoolProfile == null)
			{
				_schoolProfile = new SchoolProfile();
				_isEditMode = true;
			}
			else
			{
				_isEditMode = false;
			}

			await LoadTopLevelsAsync();
			PopulateFields();
			await RestoreAddressSelectionsAsync();

			UpdateHeaderPreview();
			UpdateSchoolLogoPreview();
			UpdateSchoolLogoStatusText();
			ApplyEditModeUi();
		}

		private async Task LoadTopLevelsAsync()
		{
			TopLevelComboBox.ItemsSource = await _philippineAddressRepository.GetTopLevelsAsync();
		}

		private void PopulateFields()
		{
			if (_schoolProfile == null)
			{
				return;
			}

			SchoolNameTextBox.Text = _schoolProfile.SchoolName ?? string.Empty;
			SchoolIdTextBox.Text = _schoolProfile.SchoolId ?? string.Empty;
			SchoolAcronymTextBox.Text = _schoolProfile.SchoolAcronym ?? string.Empty;

			RegionTextBox.Text = _schoolProfile.Region ?? string.Empty;
			DivisionTextBox.Text = _schoolProfile.Division ?? string.Empty;
			DistrictTextBox.Text = _schoolProfile.District ?? string.Empty;

			AddressLineTextBox.Text = _schoolProfile.AddressLine ?? string.Empty;

			SchoolHeadNameTextBox.Text = _schoolProfile.SchoolHeadName ?? string.Empty;
			SchoolHeadPositionTextBox.Text = _schoolProfile.SchoolHeadPosition ?? string.Empty;
		}

		private async Task RestoreAddressSelectionsAsync()
		{
			if (_schoolProfile == null)
			{
				return;
			}

			_isAddressSelectionLoading = true;

			try
			{
				TopLevelComboBox.SelectedItem = null;
				MunicipalityCityComboBox.ItemsSource = null;
				MunicipalityCityComboBox.SelectedItem = null;
				BarangayComboBox.ItemsSource = null;
				BarangayComboBox.SelectedItem = null;

				AddressTopLevel? selectedTopLevel = null;
				AddressLocality? selectedLocality = null;

				IReadOnlyList<AddressTopLevel> topLevels = await _philippineAddressRepository.GetTopLevelsAsync();

				if (!string.IsNullOrWhiteSpace(_schoolProfile.ProvinceCode))
				{
					selectedTopLevel = topLevels.FirstOrDefault(x =>
						string.Equals(x.Code, _schoolProfile.ProvinceCode, StringComparison.OrdinalIgnoreCase));

					TopLevelComboBox.SelectedItem = selectedTopLevel;
				}

				if (selectedTopLevel != null)
				{
					IReadOnlyList<AddressLocality> localities =
						await _philippineAddressRepository.GetLocalitiesByTopLevelCodeAsync(selectedTopLevel.Code);

					MunicipalityCityComboBox.ItemsSource = localities;

					if (!string.IsNullOrWhiteSpace(_schoolProfile.MunicipalityCityCode))
					{
						selectedLocality = localities.FirstOrDefault(x =>
							string.Equals(x.Code, _schoolProfile.MunicipalityCityCode, StringComparison.OrdinalIgnoreCase));

						MunicipalityCityComboBox.SelectedItem = selectedLocality;
					}
				}

				if (selectedLocality != null)
				{
					IReadOnlyList<AddressBarangay> barangays =
						await _philippineAddressRepository.GetBarangaysByLocalityCodeAsync(selectedLocality.Code);

					BarangayComboBox.ItemsSource = barangays;

					if (!string.IsNullOrWhiteSpace(_schoolProfile.BarangayCode))
					{
						BarangayComboBox.SelectedItem = barangays.FirstOrDefault(x =>
							string.Equals(x.Code, _schoolProfile.BarangayCode, StringComparison.OrdinalIgnoreCase));
					}
				}
			}
			finally
			{
				_isAddressSelectionLoading = false;
				UpdateAddressComboState();
			}
		}

		private async void TopLevelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_isAddressSelectionLoading)
			{
				return;
			}

			_isAddressSelectionLoading = true;

			try
			{
				await LoadLocalitiesForSelectedTopLevelAsync(true);
			}
			finally
			{
				_isAddressSelectionLoading = false;
				UpdateAddressComboState();
			}
		}

		private async void MunicipalityCityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_isAddressSelectionLoading)
			{
				return;
			}

			_isAddressSelectionLoading = true;

			try
			{
				await LoadBarangaysForSelectedLocalityAsync(true);
			}
			finally
			{
				_isAddressSelectionLoading = false;
				UpdateAddressComboState();
			}
		}

		private async Task LoadLocalitiesForSelectedTopLevelAsync(bool clearChildSelections)
		{
			if (TopLevelComboBox.SelectedItem is not AddressTopLevel topLevel)
			{
				MunicipalityCityComboBox.ItemsSource = null;
				BarangayComboBox.ItemsSource = null;

				if (clearChildSelections)
				{
					MunicipalityCityComboBox.SelectedItem = null;
					BarangayComboBox.SelectedItem = null;
				}

				UpdateAddressComboState();
				return;
			}

			IReadOnlyList<AddressLocality> localities =
				await _philippineAddressRepository.GetLocalitiesByTopLevelCodeAsync(topLevel.Code);

			MunicipalityCityComboBox.ItemsSource = localities;

			if (clearChildSelections)
			{
				MunicipalityCityComboBox.SelectedItem = null;
				BarangayComboBox.ItemsSource = null;
				BarangayComboBox.SelectedItem = null;
			}

			UpdateAddressComboState();
		}

		private async Task LoadBarangaysForSelectedLocalityAsync(bool clearBarangaySelection)
		{
			if (MunicipalityCityComboBox.SelectedItem is not AddressLocality locality)
			{
				BarangayComboBox.ItemsSource = null;

				if (clearBarangaySelection)
				{
					BarangayComboBox.SelectedItem = null;
				}

				UpdateAddressComboState();
				return;
			}

			IReadOnlyList<AddressBarangay> barangays =
				await _philippineAddressRepository.GetBarangaysByLocalityCodeAsync(locality.Code);

			BarangayComboBox.ItemsSource = barangays;

			if (clearBarangaySelection)
			{
				BarangayComboBox.SelectedItem = null;
			}

			UpdateAddressComboState();
		}

		private async void SaveSchoolProfileButton_Click(object sender, RoutedEventArgs e)
		{
			if (!_isEditMode)
			{
				_isEditMode = true;
				ApplyEditModeUi();
				return;
			}

			if (_schoolProfile == null)
			{
				_schoolProfile = new SchoolProfile();
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

			_schoolProfile.SchoolName = schoolName;
			_schoolProfile.SchoolId = NullIfWhiteSpace(SchoolIdTextBox.Text);
			_schoolProfile.SchoolAcronym = NullIfWhiteSpace(SchoolAcronymTextBox.Text);

			_schoolProfile.Region = NullIfWhiteSpace(RegionTextBox.Text);
			_schoolProfile.Division = NullIfWhiteSpace(DivisionTextBox.Text);
			_schoolProfile.District = NullIfWhiteSpace(DistrictTextBox.Text);

			if (TopLevelComboBox.SelectedItem is AddressTopLevel topLevel)
			{
				_schoolProfile.ProvinceCode = topLevel.Code;
				_schoolProfile.ProvinceName = topLevel.Name;
			}
			else
			{
				_schoolProfile.ProvinceCode = null;
				_schoolProfile.ProvinceName = null;
			}

			if (MunicipalityCityComboBox.SelectedItem is AddressLocality locality)
			{
				_schoolProfile.MunicipalityCityCode = locality.Code;
				_schoolProfile.MunicipalityCityName = locality.Name;
			}
			else
			{
				_schoolProfile.MunicipalityCityCode = null;
				_schoolProfile.MunicipalityCityName = null;
			}

			if (BarangayComboBox.SelectedItem is AddressBarangay barangay)
			{
				_schoolProfile.BarangayCode = barangay.Code;
				_schoolProfile.BarangayName = barangay.Name;
			}
			else
			{
				_schoolProfile.BarangayCode = null;
				_schoolProfile.BarangayName = null;
			}

			_schoolProfile.AddressLine = NullIfWhiteSpace(AddressLineTextBox.Text);
			_schoolProfile.SchoolHeadName = NullIfWhiteSpace(SchoolHeadNameTextBox.Text);
			_schoolProfile.SchoolHeadPosition = NullIfWhiteSpace(SchoolHeadPositionTextBox.Text);

			ApplySchoolLogoChanges();

			if (_schoolProfile.Id == 0)
			{
				int newId = await _schoolProfileRepository.InsertSchoolProfileAsync(_schoolProfile);
				_schoolProfile.Id = newId;
			}
			else
			{
				await _schoolProfileRepository.UpdateSchoolProfileAsync(_schoolProfile);
			}

			_pendingSchoolLogoSourcePath = null;
			_removeSchoolLogo = false;
			_isEditMode = false;

			UpdateHeaderPreview();
			UpdateSchoolLogoPreview();
			UpdateSchoolLogoStatusText();
			ApplyEditModeUi();

			MessageBox.Show(
				"School profile saved successfully.",
				"School Profile Saved",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		private void BrowseSchoolLogoButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog
			{
				Title = "Choose School Logo",
				Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.webp",
				CheckFileExists = true,
				CheckPathExists = true,
				Multiselect = false
			};

			if (dialog.ShowDialog() != true)
			{
				return;
			}

			_pendingSchoolLogoSourcePath = dialog.FileName;
			_removeSchoolLogo = false;

			UpdateSchoolLogoPreview();
			UpdateSchoolLogoStatusText();
		}

		private void RemoveSchoolLogoButton_Click(object sender, RoutedEventArgs e)
		{
			_pendingSchoolLogoSourcePath = null;
			_removeSchoolLogo = true;

			UpdateSchoolLogoPreview();
			UpdateSchoolLogoStatusText();
		}

		private void SchoolFields_TextChanged(object sender, TextChangedEventArgs e)
		{
			UpdateHeaderPreview();
			UpdateSchoolLogoPreview();
		}

		private void UpdateHeaderPreview()
		{
			string schoolName = SchoolNameTextBox.Text.Trim();
			string schoolAcronym = SchoolAcronymTextBox.Text.Trim();
			string division = DivisionTextBox.Text.Trim();

			SchoolHeaderNameTextBlock.Text = string.IsNullOrWhiteSpace(schoolName)
				? "SCHOOL PROFILE"
				: schoolName.ToUpperInvariant();

			List<string> metaParts = new();

			if (!string.IsNullOrWhiteSpace(schoolAcronym))
			{
				metaParts.Add(schoolAcronym.ToUpperInvariant());
			}

			if (!string.IsNullOrWhiteSpace(division))
			{
				metaParts.Add(division);
			}

			SchoolHeaderMetaTextBlock.Text = string.Join(" • ", metaParts);
		}

		private void UpdateSchoolLogoPreview()
		{
			string? previewPath = null;

			if (!string.IsNullOrWhiteSpace(_pendingSchoolLogoSourcePath))
			{
				previewPath = _pendingSchoolLogoSourcePath;
			}
			else if (!_removeSchoolLogo && !string.IsNullOrWhiteSpace(_schoolProfile?.SchoolLogoPath))
			{
				previewPath = GetAbsolutePath(_schoolProfile.SchoolLogoPath);
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

					SchoolLogoEllipse.Fill = new ImageBrush(bitmap)
					{
						Stretch = Stretch.UniformToFill
					};

					SchoolLogoInitialsTextBlock.Visibility = Visibility.Collapsed;
					return;
				}
				catch
				{
				}
			}

			SchoolLogoEllipse.Fill = (Brush)FindResource("Brush.SurfaceAlt");
			SchoolLogoInitialsTextBlock.Text = BuildInitials();
			SchoolLogoInitialsTextBlock.Visibility = Visibility.Visible;
		}

		private void ApplySchoolLogoChanges()
		{
			if (_schoolProfile == null)
			{
				return;
			}

			if (_removeSchoolLogo)
			{
				DeleteStoredSchoolLogoIfExists(_schoolProfile.SchoolLogoPath);
				_schoolProfile.SchoolLogoPath = null;
			}

			if (string.IsNullOrWhiteSpace(_pendingSchoolLogoSourcePath) || !File.Exists(_pendingSchoolLogoSourcePath))
			{
				return;
			}

			if (_schoolProfile.SchoolUid == null || _schoolProfile.SchoolUid == Guid.Empty)
			{
				_schoolProfile.SchoolUid = Guid.NewGuid();
			}

			string logoDirectory = GetSchoolLogoDirectory();
			string extension = Path.GetExtension(_pendingSchoolLogoSourcePath);
			string fileName = $"{_schoolProfile.SchoolUid}{extension}";
			string targetPath = Path.Combine(logoDirectory, fileName);

			Directory.CreateDirectory(logoDirectory);
			File.Copy(_pendingSchoolLogoSourcePath, targetPath, true);

			string relativePath = Path.GetRelativePath(_appPaths.RootDirectory, targetPath);
			relativePath = relativePath.Replace('/', '\\');

			string? oldAbsolutePath = GetAbsolutePath(_schoolProfile.SchoolLogoPath);
			if (!string.IsNullOrWhiteSpace(oldAbsolutePath) &&
				!string.Equals(
					Path.GetFullPath(oldAbsolutePath),
					Path.GetFullPath(targetPath),
					StringComparison.OrdinalIgnoreCase) &&
				File.Exists(oldAbsolutePath))
			{
				File.Delete(oldAbsolutePath);
			}

			_schoolProfile.SchoolLogoPath = relativePath;
		}

		private void DeleteStoredSchoolLogoIfExists(string? relativePath)
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

		private string GetSchoolLogoDirectory()
		{
			return Path.Combine(_appPaths.ImagesDirectory, "SchoolLogos");
		}

		private string BuildInitials()
		{
			string schoolName = SchoolNameTextBox.Text.Trim();

			if (string.IsNullOrWhiteSpace(schoolName))
			{
				return "SCH";
			}

			string[] parts = schoolName
				.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.Take(3)
				.ToArray();

			string initials = string.Concat(parts.Select(x => x[0])).ToUpperInvariant();

			return string.IsNullOrWhiteSpace(initials) ? "SCH" : initials;
		}

		private void ApplyEditModeUi()
		{
			SetTextBoxMode(SchoolNameTextBox, _isEditMode);
			SetTextBoxMode(SchoolIdTextBox, _isEditMode);
			SetTextBoxMode(SchoolAcronymTextBox, _isEditMode);
			SetTextBoxMode(RegionTextBox, _isEditMode);
			SetTextBoxMode(DivisionTextBox, _isEditMode);
			SetTextBoxMode(DistrictTextBox, _isEditMode);
			SetTextBoxMode(AddressLineTextBox, _isEditMode);
			SetTextBoxMode(SchoolHeadNameTextBox, _isEditMode);
			SetTextBoxMode(SchoolHeadPositionTextBox, _isEditMode);

			SetComboBoxMode(TopLevelComboBox, _isEditMode);
			SetComboBoxMode(MunicipalityCityComboBox, _isEditMode);
			SetComboBoxMode(BarangayComboBox, _isEditMode);

			SchoolLogoButtonsPanel.Visibility = _isEditMode ? Visibility.Visible : Visibility.Collapsed;

			SaveSchoolProfileActionTextBlock.Text = _isEditMode ? "Save School Profile" : "Edit School Profile";
			SaveSchoolProfileActionIcon.Source = _isEditMode
				? "/Resources/Icons/save.svg"
				: "/Resources/Icons/edit.svg";
			SaveSchoolProfileButton.ToolTip = _isEditMode ? "Save School Profile" : "Edit School Profile";
			UpdateAddressComboState();
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

		private void UpdateSchoolLogoStatusText()
		{
			if (_removeSchoolLogo)
			{
				SchoolLogoStatusTextBlock.Text = "School logo will be removed on save.";
				SchoolLogoStatusTextBlock.Visibility = Visibility.Visible;
				return;
			}

			if (!string.IsNullOrWhiteSpace(_pendingSchoolLogoSourcePath))
			{
				SchoolLogoStatusTextBlock.Text = $"Selected logo: {Path.GetFileName(_pendingSchoolLogoSourcePath)}";
				SchoolLogoStatusTextBlock.Visibility = Visibility.Visible;
				return;
			}

			if (string.IsNullOrWhiteSpace(_schoolProfile?.SchoolLogoPath))
			{
				SchoolLogoStatusTextBlock.Text = "No school logo selected.";
				SchoolLogoStatusTextBlock.Visibility = Visibility.Visible;
				return;
			}

			SchoolLogoStatusTextBlock.Text = string.Empty;
			SchoolLogoStatusTextBlock.Visibility = Visibility.Collapsed;
		}

		private static string? NullIfWhiteSpace(string? value)
		{
			return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
		}

		private void UpdateAddressComboState()
		{
			bool canEdit = _isEditMode;

			TopLevelComboBox.IsEnabled = canEdit;
			MunicipalityCityComboBox.IsEnabled = canEdit && TopLevelComboBox.SelectedItem is AddressTopLevel;
			BarangayComboBox.IsEnabled = canEdit && MunicipalityCityComboBox.SelectedItem is AddressLocality;
		}
	}
}
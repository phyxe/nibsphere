using Microsoft.Win32;
using NibSphere.Core.Importing;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Modules.Learners.Importing;
using NibSphere.Data.Modules.Learners.Importing;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace NibSphere.Modules.Learners.Windows
{
	public partial class LearnersImportWindow : Window
	{
		private readonly LearnersImportService _learnersImportService;

		private readonly ObservableCollection<ImportMappingRow> _learnerMappingRows = new();

		private ImportTableDocument? _document;
		private ImportTableSheet? _selectedSheet;
		private LearnersImportColumnMap? _learnerColumnMap;

		private int _currentStepIndex;

		public LearnersImportWindow()
		{
			InitializeComponent();

			IAppPaths appPaths = App.AppPaths;
			_learnersImportService = new LearnersImportService(appPaths);

			LearnerMappingsDataGrid.ItemsSource = _learnerMappingRows;

			UpdateWindowStateUi();
			UpdateStepUi();

			SourceInitialized += LearnersImportWindow_SourceInitialized;
		}

		private async void BrowseFileButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new()
			{
				Title = "Select Learners Import File",
				Filter = "Supported Files|*.csv;*.xlsx|CSV Files|*.csv|Excel Files|*.xlsx",
				CheckFileExists = true,
				CheckPathExists = true,
				Multiselect = false
			};

			if (dialog.ShowDialog() != true)
			{
				return;
			}

			try
			{
				_document = await _learnersImportService.ReadAsync(dialog.FileName);
				_selectedSheet = null;
				_learnerColumnMap = null;

				SelectedFilePathTextBox.Text = dialog.FileName;
				DetectedFileTypeTextBox.Text = _document.FileKind.ToString();

				SheetComboBox.ItemsSource = _document.Sheets;
				SheetComboBox.SelectedIndex = _document.Sheets.Count > 0 ? 0 : -1;

				UpdateSourceInfo();
				BuildLearnerSuggestedMappings();

				_currentStepIndex = 0;
				UpdateStepUi();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Failed to read import file.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
					"Import Error",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
		}

		private void SheetComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			_selectedSheet = SheetComboBox.SelectedItem as ImportTableSheet;
			_learnerColumnMap = null;

			UpdateSourceInfo();
			BuildLearnerSuggestedMappings();
		}

		private void PreviousButton_Click(object sender, RoutedEventArgs e)
		{
			if (_currentStepIndex <= 0)
			{
				return;
			}

			_currentStepIndex--;
			UpdateStepUi();
		}

		private void NextButton_Click(object sender, RoutedEventArgs e)
		{
			if (_currentStepIndex == 0)
			{
				if (!ValidateSourceStep())
				{
					return;
				}

				_currentStepIndex = 1;
				UpdateStepUi();
				return;
			}

			if (_currentStepIndex == 1)
			{
				if (!ValidateMappingStep())
				{
					return;
				}

				_learnerColumnMap = BuildLearnerColumnMapFromUi();

				MessageBox.Show(
					"Learner field mapping is ready. The next slice will use this mapping to build the import preview.",
					"Import Learners",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}
		}

		private bool ValidateSourceStep()
		{
			if (_document == null)
			{
				MessageBox.Show(
					"Select an import file first.",
					"Import Learners",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return false;
			}

			if (_selectedSheet == null)
			{
				MessageBox.Show(
					"Select a sheet first.",
					"Import Learners",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return false;
			}

			if (_selectedSheet.Headers.Count == 0)
			{
				MessageBox.Show(
					"The selected sheet does not contain any headers.",
					"Import Learners",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return false;
			}

			return true;
		}

		private bool ValidateMappingStep()
		{
			foreach (ImportMappingRow row in _learnerMappingRows)
			{
				if (row.IsRequired && string.IsNullOrWhiteSpace(row.SelectedSourceColumnHeader))
				{
					MessageBox.Show(
						$"Map the required field '{row.FieldLabel}' first.",
						"Field Mapping",
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					return false;
				}
			}

			return true;
		}

		private void UpdateSourceInfo()
		{
			if (_document == null)
			{
				SourceInfoTextBox.Clear();
				return;
			}

			if (_selectedSheet == null)
			{
				SourceInfoTextBox.Text =
					$"File: {_document.FilePath}{Environment.NewLine}" +
					$"Type: {_document.FileKind}{Environment.NewLine}" +
					$"Sheets: {_document.Sheets.Count}";
				return;
			}

			string headersPreview = _selectedSheet.Headers.Count == 0
				? "(none)"
				: string.Join(", ", _selectedSheet.Headers.Take(12));

			SourceInfoTextBox.Text =
				$"File: {_document.FilePath}{Environment.NewLine}" +
				$"Type: {_document.FileKind}{Environment.NewLine}" +
				$"Selected Sheet: {_selectedSheet.Name}{Environment.NewLine}" +
				$"Header Count: {_selectedSheet.Headers.Count}{Environment.NewLine}" +
				$"Row Count: {_selectedSheet.Rows.Count}{Environment.NewLine}" +
				$"Headers: {headersPreview}";
		}

		private void BuildLearnerSuggestedMappings()
		{
			_learnerMappingRows.Clear();

			if (_selectedSheet == null)
			{
				return;
			}

			List<string> availableHeaders = new() { string.Empty };
			availableHeaders.AddRange(_selectedSheet.Headers);

			foreach (LearnerFieldMappingDefinition field in GetLearnerFieldDefinitions())
			{
				_learnerMappingRows.Add(new ImportMappingRow
				{
					FieldKey = field.FieldKey,
					FieldLabel = field.FieldLabel,
					IsRequired = field.IsRequired,
					AvailableHeaders = availableHeaders,
					SelectedSourceColumnHeader = SuggestHeader(_selectedSheet.Headers, field.CandidateHeaders)
				});
			}
		}

		private static string? SuggestHeader(
			IReadOnlyList<string> headers,
			IReadOnlyList<string> candidateHeaders)
		{
			foreach (string candidate in candidateHeaders)
			{
				string? match = headers.FirstOrDefault(x =>
					string.Equals(x, candidate, StringComparison.OrdinalIgnoreCase));

				if (!string.IsNullOrWhiteSpace(match))
				{
					return match;
				}
			}

			return null;
		}

		private LearnersImportColumnMap BuildLearnerColumnMapFromUi()
		{
			return new LearnersImportColumnMap
			{
				LrnColumnHeader = GetSelectedHeader("lrn"),
				LastNameColumnHeader = GetSelectedHeader("last-name"),
				FirstNameColumnHeader = GetSelectedHeader("first-name"),
				MiddleNameColumnHeader = GetSelectedHeader("middle-name"),
				ExtensionNameColumnHeader = GetSelectedHeader("extension-name"),
				BirthdayColumnHeader = GetSelectedHeader("birthday"),
				SexColumnHeader = GetSelectedHeader("sex"),
				PronounColumnHeader = GetSelectedHeader("pronoun"),
				ReligiousAffiliationColumnHeader = GetSelectedHeader("religious-affiliation"),
				HouseStreetSitioPurokColumnHeader = GetSelectedHeader("house-street-sitio-purok"),
				BarangayColumnHeader = GetSelectedHeader("barangay"),
				MunicipalityColumnHeader = GetSelectedHeader("municipality"),
				ProvinceColumnHeader = GetSelectedHeader("province"),
				MobileNumberColumnHeader = GetSelectedHeader("mobile-number"),
				EmailColumnHeader = GetSelectedHeader("email")
			};
		}

		private string? GetSelectedHeader(string fieldKey)
		{
			return _learnerMappingRows
				.FirstOrDefault(x => string.Equals(x.FieldKey, fieldKey, StringComparison.OrdinalIgnoreCase))
				?.SelectedSourceColumnHeader;
		}

		private static IReadOnlyList<LearnerFieldMappingDefinition> GetLearnerFieldDefinitions()
		{
			return new List<LearnerFieldMappingDefinition>
			{
				new("lrn", "LRN", false, new[] { "LRN", "Lrn", "Learner LRN" }),
				new("last-name", "Last Name", true, new[] { "Last Name", "Lastname", "Surname", "Family Name" }),
				new("first-name", "First Name", true, new[] { "First Name", "Firstname", "Given Name" }),
				new("middle-name", "Middle Name", false, new[] { "Middle Name", "Middlename", "Middle" }),
				new("extension-name", "Extension Name", false, new[] { "Extension Name", "Suffix", "Name Extension" }),
				new("birthday", "Birthday", false, new[] { "Birthday", "Birth Date", "Date of Birth", "DOB" }),
				new("sex", "Sex", false, new[] { "Sex", "Gender" }),
				new("pronoun", "Pronoun", false, new[] { "Pronoun", "Pronouns" }),
				new("religious-affiliation", "Religious Affiliation", false, new[] { "Religious Affiliation", "Religion", "ReligiousAffiliation" }),
				new("house-street-sitio-purok", "House/Street/Sitio/Purok", false, new[] { "HouseStreetSitioPurok", "House/Street/Sitio/Purok", "Street Address", "Address Line" }),
				new("barangay", "Barangay", false, new[] { "Barangay", "Brgy" }),
				new("municipality", "Municipality", false, new[] { "Municipality", "City", "Municipality/City" }),
				new("province", "Province", false, new[] { "Province" }),
				new("mobile-number", "Mobile Number", false, new[] { "Mobile Number", "Mobile", "Contact Number", "Phone Number" }),
				new("email", "Email", false, new[] { "Email", "Email Address" })
			};
		}

		private void UpdateStepUi()
		{
			if (_currentStepIndex == 0)
			{
				StepTitleTextBlock.Text = "Import Learners";
				StepDescriptionTextBlock.Text = "Step 1: select the source file and choose the sheet to import from.";

				SourceStepPanel.Visibility = Visibility.Visible;
				MappingStepPanel.Visibility = Visibility.Collapsed;

				PreviousButton.Visibility = Visibility.Collapsed;
				NextButton.Content = "Next";
				return;
			}

			StepTitleTextBlock.Text = "Import Learners";
			StepDescriptionTextBlock.Text = "Step 2: map learner fields to the source columns.";

			SourceStepPanel.Visibility = Visibility.Collapsed;
			MappingStepPanel.Visibility = Visibility.Visible;

			PreviousButton.Visibility = Visibility.Visible;
			NextButton.Content = "Continue";
		}

		private void MinimizeButton_Click(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState == WindowState.Maximized
				? WindowState.Normal
				: WindowState.Maximized;
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void LearnersImportWindow_StateChanged(object? sender, EventArgs e)
		{
			UpdateWindowStateUi();
		}

		private void UpdateWindowStateUi()
		{
			if (MaximizeRestoreGlyph == null || WindowBorder == null)
			{
				return;
			}

			bool isMaximized = WindowState == WindowState.Maximized;

			MaximizeRestoreGlyph.Text = isMaximized ? "\uE923" : "\uE922";
			WindowBorder.BorderThickness = isMaximized ? new Thickness(0) : new Thickness(1);
		}

		private void LearnersImportWindow_SourceInitialized(object? sender, EventArgs e)
		{
			IntPtr handle = new WindowInteropHelper(this).Handle;
			HwndSource source = HwndSource.FromHwnd(handle);
			source.AddHook(WndProc);
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			const int WM_GETMINMAXINFO = 0x0024;

			if (msg == WM_GETMINMAXINFO)
			{
				WmGetMinMaxInfo(hwnd, lParam);
				handled = true;
			}

			return IntPtr.Zero;
		}

		private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
		{
			MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

			IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

			if (monitor != IntPtr.Zero)
			{
				MONITORINFO monitorInfo = new();
				GetMonitorInfo(monitor, monitorInfo);

				RECT workArea = monitorInfo.rcWork;
				RECT monitorArea = monitorInfo.rcMonitor;

				mmi.ptMaxPosition.x = Math.Abs(workArea.left - monitorArea.left);
				mmi.ptMaxPosition.y = Math.Abs(workArea.top - monitorArea.top);
				mmi.ptMaxSize.x = Math.Abs(workArea.right - workArea.left);
				mmi.ptMaxSize.y = Math.Abs(workArea.bottom - workArea.top);
			}

			Marshal.StructureToPtr(mmi, lParam, true);
		}

		private const int MONITOR_DEFAULTTONEAREST = 2;

		[DllImport("user32.dll")]
		private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

		[StructLayout(LayoutKind.Sequential)]
		private struct POINT
		{
			public int x;
			public int y;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct MINMAXINFO
		{
			public POINT ptReserved;
			public POINT ptMaxSize;
			public POINT ptMaxPosition;
			public POINT ptMinTrackSize;
			public POINT ptMaxTrackSize;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private class MONITORINFO
		{
			public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
			public RECT rcMonitor = new();
			public RECT rcWork = new();
			public int dwFlags;
		}

		private sealed record LearnerFieldMappingDefinition(
			string FieldKey,
			string FieldLabel,
			bool IsRequired,
			IReadOnlyList<string> CandidateHeaders);

		private sealed class ImportMappingRow : INotifyPropertyChanged
		{
			private string? _selectedSourceColumnHeader;

			public string FieldKey { get; set; } = string.Empty;

			public string FieldLabel { get; set; } = string.Empty;

			public bool IsRequired { get; set; }

			public string RequiredText => IsRequired ? "Yes" : "No";

			public IReadOnlyList<string> AvailableHeaders { get; set; } = Array.Empty<string>();

			public string? SelectedSourceColumnHeader
			{
				get => _selectedSourceColumnHeader;
				set
				{
					if (_selectedSourceColumnHeader != value)
					{
						_selectedSourceColumnHeader = value;
						OnPropertyChanged();
					}
				}
			}

			public event PropertyChangedEventHandler? PropertyChanged;

			private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
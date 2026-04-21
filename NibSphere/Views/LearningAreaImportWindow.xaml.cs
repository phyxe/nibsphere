using Microsoft.Win32;
using NibSphere.Core.Importing;
using NibSphere.Core.Interfaces;
using NibSphere.Core.Models;
using NibSphere.Data.Importing;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace NibSphere.Views
{
	public partial class LearningAreaImportWindow : Window
	{
		private readonly ImportFileReaderService _fileReaderService;
		private readonly LearningAreaImportDefinition _definition;

		private ImportTableDocument? _document;
		private ImportTableSheet? _selectedSheet;
		private ImportSimulationResult<LearningArea>? _simulationResult;
		private ImportFinalizeResult? _finalizeResult;

		private readonly ObservableCollection<ImportMappingRow> _mappingRows = new();

		public LearningAreaImportWindow()
		{
			InitializeComponent();

			IAppPaths appPaths = App.AppPaths;
			_fileReaderService = new ImportFileReaderService();
			_definition = new LearningAreaImportDefinition(appPaths);

			MappingsDataGrid.ItemsSource = _mappingRows;

			WizardTabControl.SelectedIndex = 0;
			UpdateStepVisibility();
			UpdateNavigationUi();
		}

		private async void BrowseFileButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog
			{
				Title = "Select Import File",
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
				_document = await _fileReaderService.ReadAsync(dialog.FileName);
				SelectedFilePathTextBox.Text = dialog.FileName;
				DetectedFileTypeTextBox.Text = _document.FileKind.ToString();

				SheetComboBox.ItemsSource = _document.Sheets.Select(x => x.Name).ToList();
				SheetComboBox.SelectedIndex = _document.Sheets.Count > 0 ? 0 : -1;

				UpdateSourceInfo();
				BuildSuggestedMappings();

				_simulationResult = null;
				_finalizeResult = null;
				ClearSimulationUi();
				ClearFinalizeUi();
				UpdateStepVisibility();
				UpdateNavigationUi();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Failed to read import file.\n\n{ex.Message}",
					"Import Error",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
		}

		private void SheetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_document == null || SheetComboBox.SelectedItem is not string sheetName)
			{
				return;
			}

			_selectedSheet = _document.Sheets.FirstOrDefault(x =>
				string.Equals(x.Name, sheetName, StringComparison.OrdinalIgnoreCase));

			UpdateSourceInfo();
			BuildSuggestedMappings();

			_simulationResult = null;
			_finalizeResult = null;
			ClearSimulationUi();
			ClearFinalizeUi();
			UpdateStepVisibility();
			UpdateNavigationUi();
		}

		private async void NextButton_Click(object sender, RoutedEventArgs e)
		{
			switch (WizardTabControl.SelectedIndex)
			{
				case 0:
					if (!CanProceedFromSourceStep())
					{
						return;
					}

					WizardTabControl.SelectedIndex = 1;
					break;

				case 1:
					if (!CanProceedFromMappingStep())
					{
						return;
					}

					await RunSimulationAsync();
					WizardTabControl.SelectedIndex = 2;
					break;

				case 2:
					if (_simulationResult == null)
					{
						return;
					}

					PopulateFinalizeSummary();
					WizardTabControl.SelectedIndex = 3;
					break;

				case 3:
					await FinalizeImportAsync();
					break;
			}

			UpdateStepVisibility();
			UpdateNavigationUi();
		}

		private void PreviousButton_Click(object sender, RoutedEventArgs e)
		{
			if (WizardTabControl.SelectedIndex > 0)
			{
				WizardTabControl.SelectedIndex--;
				UpdateStepVisibility();
				UpdateNavigationUi();
			}
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private bool CanProceedFromSourceStep()
		{
			if (_document == null)
			{
				MessageBox.Show(
					"Select a source file first.",
					"Import",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				return false;
			}

			if (_selectedSheet == null)
			{
				MessageBox.Show(
					"Select a sheet first.",
					"Import",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				return false;
			}

			return true;
		}

		private bool CanProceedFromMappingStep()
		{
			foreach (ImportMappingRow row in _mappingRows)
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
			if (_selectedSheet == null)
			{
				SourceInfoTextBox.Text = string.Empty;
				return;
			}

			SourceInfoTextBox.Text =
				$"Sheet: {_selectedSheet.Name}{Environment.NewLine}" +
				$"Headers: {_selectedSheet.Headers.Count}{Environment.NewLine}" +
				$"Rows: {_selectedSheet.Rows.Count}";
		}

		private void BuildSuggestedMappings()
		{
			_mappingRows.Clear();

			if (_selectedSheet == null)
			{
				return;
			}

			List<ImportColumnMapping> suggestedMappings =
				ImportMappingResolver.SuggestMappings(_definition, _selectedSheet);

			List<string> availableColumns = _selectedSheet.Headers.ToList();

			foreach (ImportFieldDefinition<LearningArea> field in _definition.Fields)
			{
				ImportColumnMapping? mapping = suggestedMappings.FirstOrDefault(x =>
					string.Equals(x.FieldKey, field.Key, StringComparison.OrdinalIgnoreCase));

				_mappingRows.Add(new ImportMappingRow
				{
					FieldKey = field.Key,
					FieldLabel = field.Label,
					IsRequired = field.IsRequired,
					AvailableSourceColumns = availableColumns,
					SelectedSourceColumnHeader = mapping?.SourceColumnHeader
				});
			}
		}

		private async Task RunSimulationAsync()
		{
			if (_document == null || _selectedSheet == null)
			{
				return;
			}

			try
			{
				ImportSimulationRequest request = new()
				{
					Document = _document,
					Sheet = _selectedSheet,
					ColumnMappings = _mappingRows
						.Select(x => new ImportColumnMapping
						{
							FieldKey = x.FieldKey,
							SourceColumnHeader = x.SelectedSourceColumnHeader,
							SourceColumnIndex = string.IsNullOrWhiteSpace(x.SelectedSourceColumnHeader)
								? null
								: _selectedSheet.Headers
									.Select((header, index) => new { header, index })
									.Where(xh => string.Equals(xh.header, x.SelectedSourceColumnHeader, StringComparison.OrdinalIgnoreCase))
									.Select(xh => (int?)xh.index)
									.FirstOrDefault()
						})
						.ToList()
				};

				await _definition.PrepareAsync(request);
				_simulationResult = await _definition.SimulateAsync(request);

				SimulationSummaryTextBox.Text =
					$"Total Rows: {_simulationResult.TotalRows}{Environment.NewLine}" +
					$"Valid Rows: {_simulationResult.ValidRowCount}{Environment.NewLine}" +
					$"Rows With Errors: {_simulationResult.ErrorRowCount}";

				SimulationPreviewDataGrid.ItemsSource = _simulationResult.PreviewRows
					.Select(x => new LearningAreaImportPreviewRow
					{
						SourceRowNumber = x.SourceRowNumber,
						Code = x.Item?.Code ?? string.Empty,
						ShortName = x.Item?.ShortName ?? string.Empty,
						Description = x.Item?.Description ?? string.Empty,
						CanPostText = x.CanPost ? "Yes" : "No",
						Issues = x.Issues.Count == 0
							? string.Empty
							: string.Join(" | ", x.Issues.Select(issue => issue.Message))
					})
					.ToList();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Simulation failed.\n\n{ex.Message}",
					"Import Simulation",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
		}

		private async Task FinalizeImportAsync()
		{
			if (_simulationResult == null)
			{
				return;
			}

			try
			{
				_finalizeResult = await _definition.FinalizeAsync(_simulationResult);

				FinalizeSummaryTextBox.Text =
					$"Created: {_finalizeResult.CreatedCount}{Environment.NewLine}" +
					$"Updated: {_finalizeResult.UpdatedCount}{Environment.NewLine}" +
					$"Skipped: {_finalizeResult.SkippedCount}";

				FinalizeMessagesTextBox.Text = string.Join(Environment.NewLine, _finalizeResult.Messages);

				MessageBox.Show(
					"Import completed successfully.",
					"Import Complete",
					MessageBoxButton.OK,
					MessageBoxImage.Information);

				DialogResult = true;
				Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Finalize import failed.\n\n{ex.Message}",
					"Import Finalize",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
		}

		private void PopulateFinalizeSummary()
		{
			if (_simulationResult == null)
			{
				return;
			}

			FinalizeSummaryTextBox.Text =
				$"Ready to finalize import.{Environment.NewLine}" +
				$"Total Rows: {_simulationResult.TotalRows}{Environment.NewLine}" +
				$"Rows That Can Post: {_simulationResult.ValidRowCount}{Environment.NewLine}" +
				$"Rows That Will Be Skipped: {_simulationResult.TotalRows - _simulationResult.ValidRowCount}";

			FinalizeMessagesTextBox.Text = "Click Next to finalize and post valid rows.";
		}

		private void ClearSimulationUi()
		{
			SimulationSummaryTextBox.Clear();
			SimulationPreviewDataGrid.ItemsSource = null;
		}

		private void ClearFinalizeUi()
		{
			FinalizeSummaryTextBox.Clear();
			FinalizeMessagesTextBox.Clear();
		}

		private void UpdateNavigationUi()
		{
			PreviousButton.IsEnabled = WizardTabControl.SelectedIndex > 0;

			switch (WizardTabControl.SelectedIndex)
			{
				case 0:
					NextButton.Content = "Next";
					break;
				case 1:
					NextButton.Content = "Simulate";
					break;
				case 2:
					NextButton.Content = "Next";
					break;
				case 3:
					NextButton.Content = "Finalize";
					break;
				default:
					NextButton.Content = "Next";
					break;
			}
		}

		private void UpdateStepVisibility()
		{
			if (SourceFileTab == null ||
				FieldMappingTab == null ||
				SimulateTab == null ||
				FinalizeTab == null)
			{
				return;
			}

			SourceFileTab.Visibility = Visibility.Collapsed;
			FieldMappingTab.Visibility = Visibility.Collapsed;
			SimulateTab.Visibility = Visibility.Collapsed;
			FinalizeTab.Visibility = Visibility.Collapsed;

			switch (WizardTabControl.SelectedIndex)
			{
				case 0:
					SourceFileTab.Visibility = Visibility.Visible;
					break;

				case 1:
					FieldMappingTab.Visibility = Visibility.Visible;
					break;

				case 2:
					SimulateTab.Visibility = Visibility.Visible;
					break;

				case 3:
					FinalizeTab.Visibility = Visibility.Visible;
					break;

				default:
					SourceFileTab.Visibility = Visibility.Visible;
					WizardTabControl.SelectedIndex = 0;
					break;
			}
		}

		private sealed class ImportMappingRow : INotifyPropertyChanged
		{
			private string? _selectedSourceColumnHeader;

			public string FieldKey { get; set; } = string.Empty;
			public string FieldLabel { get; set; } = string.Empty;
			public bool IsRequired { get; set; }
			public IReadOnlyList<string> AvailableSourceColumns { get; set; } = Array.Empty<string>();

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

		private sealed class LearningAreaImportPreviewRow
		{
			public int SourceRowNumber { get; set; }
			public string Code { get; set; } = string.Empty;
			public string ShortName { get; set; } = string.Empty;
			public string Description { get; set; } = string.Empty;
			public string CanPostText { get; set; } = string.Empty;
			public string Issues { get; set; } = string.Empty;
		}
	}
}
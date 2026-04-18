using Allied_Forms.Views;
using System.Windows;

namespace Allied_Forms
{
	public partial class MainWindow : Window
	{
		private bool _isNavCollapsed = false;

		public MainWindow()
		{
			InitializeComponent();
			UpdateThemeUi();
		}

		private void NavToggleButton_Click(object sender, RoutedEventArgs e)
		{
			_isNavCollapsed = !_isNavCollapsed;

			NavColumn.Width = _isNavCollapsed
				? new GridLength(72)
				: new GridLength(220);

			NavHeaderText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
			DashboardNavText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
			StudentsNavText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
			ReportsNavText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
		}

		private void BottomSettingsNavButton_Click(object sender, RoutedEventArgs e)
		{
			MainContentHost.Content = new SettingsView();
		}

		private void ThemeToggleNavButton_Click(object sender, RoutedEventArgs e)
		{
			App.ToggleTheme();
			UpdateThemeUi();
		}

		private void UpdateThemeUi()
		{
			if (App.IsDarkTheme)
			{
				ThemeStatusTextBlock.Text = "Theme: Dark";
				ThemeToggleNavButton.ToolTip = "Switch to Light Theme";
				ThemeToggleNavIcon.Source = "/Resources/Icons/modelight.svg";
			}
			else
			{
				ThemeStatusTextBlock.Text = "Theme: Light";
				ThemeToggleNavButton.ToolTip = "Switch to Dark Theme";
				ThemeToggleNavIcon.Source = "/Resources/Icons/modedark.svg";
			}
		}
	}
}
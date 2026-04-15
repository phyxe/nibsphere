using System.Windows;

namespace Allied_Forms
{
    public partial class MainWindow : Window
    {
        private bool _isNavCollapsed = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void NavToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isNavCollapsed = !_isNavCollapsed;

            NavColumn.Width = _isNavCollapsed
                ? new GridLength(72)
                : new GridLength(220);

            NavHeaderText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
            ThemeToggleNavText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SchoolNameLabel.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SchoolNameText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
            BottomSettingsNavText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;

            DashboardNavText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SchoolProfileNavText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SchoolYearNavText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
            StudentsNavText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
            LearningAreasNavText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
            ReportsNavText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SettingsNavText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
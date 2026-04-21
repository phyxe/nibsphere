using NibSphere.Views;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace NibSphere
{
	public partial class MainWindow : Window
	{
		private bool _isNavCollapsed = false;
		private bool _isSchoolNavPopupOpen = false;

		public MainWindow()
		{
			InitializeComponent();
			UpdateThemeUi();
			UpdateWindowStateUi();
			UpdateNavBrandUi();
			UpdateNavToggleButtonUi();
			UpdateSchoolNavUi();

			SourceInitialized += MainWindow_SourceInitialized;
		}

		private void NavToggleButton_Click(object sender, RoutedEventArgs e)
		{
			_isNavCollapsed = !_isNavCollapsed;

			NavColumn.Width = _isNavCollapsed
				? new GridLength(72)
				: new GridLength(220);

			DashboardNavText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
			LearnerNavText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
			ReportsNavText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
			SchoolYearNavText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;
			SchoolNavText.Visibility = _isNavCollapsed ? Visibility.Collapsed : Visibility.Visible;

			UpdateNavBrandUi();
			UpdateNavToggleButtonUi();
			UpdateSchoolNavUi();
		}

		private void UserProfileViewNavButton_Click(object sender, RoutedEventArgs e)
		{
			MainContentHost.Content = new UserProfileView();
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

		private void MainWindow_StateChanged(object? sender, EventArgs e)
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

		private void MainWindow_SourceInitialized(object? sender, EventArgs e)
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
				MONITORINFO monitorInfo = new MONITORINFO();
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
			public RECT rcMonitor = new RECT();
			public RECT rcWork = new RECT();
			public int dwFlags;
		}

		private void UpdateNavToggleButtonUi()
		{
			if (NavToggleButtonIcon == null || NavToggleButton == null)
			{
				return;
			}

			NavToggleButtonIcon.Source = _isNavCollapsed
				? "/Resources/Icons/arrowright.svg"
				: "/Resources/Icons/arrowleft.svg";

			NavToggleButton.ToolTip = _isNavCollapsed
				? "Expand navigation"
				: "Collapse navigation";
		}

		private void UpdateNavBrandUi()
		{
			if (NavBrandImage == null)
			{
				return;
			}

			NavBrandImage.Source = new System.Windows.Media.Imaging.BitmapImage(
				new Uri(
					_isNavCollapsed
						? "/Resources/Brand/nibsphere-mark.png"
						: "/Resources/Brand/nibsphere-full.png",
					UriKind.Relative));
		}

		private void SchoolNavButton_Click(object sender, RoutedEventArgs e)
		{
			if (_isNavCollapsed)
			{
				return;
			}

			_isSchoolNavPopupOpen = !_isSchoolNavPopupOpen;
			UpdateSchoolNavUi();
		}

		private void SchoolNavPopup_Closed(object sender, EventArgs e)
		{
			_isSchoolNavPopupOpen = false;
			UpdateSchoolNavUi();
		}

		private void UpdateSchoolNavUi()
		{
			if (SchoolNavPopup == null || SchoolNavIndicatorText == null)
			{
				return;
			}

			if (_isNavCollapsed)
			{
				_isSchoolNavPopupOpen = false;
				SchoolNavPopup.IsOpen = false;
				SchoolNavIndicatorText.Visibility = Visibility.Collapsed;
				return;
			}

			SchoolNavIndicatorText.Visibility = Visibility.Visible;
			SchoolNavIndicatorText.Text = _isSchoolNavPopupOpen ? "▾" : "▸";
			SchoolNavPopup.IsOpen = _isSchoolNavPopupOpen;
		}

		private void SchoolProfileSubNavButton_Click(object sender, RoutedEventArgs e)
		{
			_isSchoolNavPopupOpen = false;
			UpdateSchoolNavUi();

			MainContentHost.Content = new SchoolProfileView();
		}

		private void LearningAreasSubNavButton_Click(object sender, RoutedEventArgs e)
		{
			_isSchoolNavPopupOpen = false;
			UpdateSchoolNavUi();

			MainContentHost.Content = new LearningAreasView();
		}
	}
}
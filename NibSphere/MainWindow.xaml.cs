using NibSphere.Views;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace NibSphere
{
	public partial class MainWindow : Window
	{
		private bool _isNavCollapsed = false;
		private bool _isUserProfileNavSelected = false;

		public MainWindow()
		{
			InitializeComponent();
			UpdateThemeUi();
			UpdateWindowStateUi();
			UpdateBottomNavUi();

			SourceInitialized += MainWindow_SourceInitialized;
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

		private void UserProfileViewNavButton_Click(object sender, RoutedEventArgs e)
		{
			MainContentHost.Content = new UserProfileView();
			_isUserProfileNavSelected = true;
			UpdateBottomNavUi();
		}

		private void ThemeToggleNavButton_Click(object sender, RoutedEventArgs e)
		{
			App.ToggleTheme();
			UpdateThemeUi();
			UpdateBottomNavUi();
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

		private void UpdateBottomNavUi()
		{
			Brush activeBrush = (Brush)FindResource("Brush.NavItemActive");

			UserProfileViewNavButton.Background = _isUserProfileNavSelected ? activeBrush : Brushes.Transparent;
			UserProfileViewNavButton.BorderBrush = _isUserProfileNavSelected ? activeBrush : Brushes.Transparent;

			if (UserProfileViewNavIcon != null)
			{
				UserProfileViewNavIcon.Tint = (Brush)FindResource("Brush.NavIcon");
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
	}
}
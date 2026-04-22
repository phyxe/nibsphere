using NibSphere.Controls;
using NibSphere.Shell.Navigation;
using NibSphere.Views;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NibSphere
{
	public partial class MainWindow : Window
	{
		private readonly ShellNavigationService _navigationService;
		private readonly Dictionary<string, NavigationVisual> _navigationVisuals =
			new(StringComparer.OrdinalIgnoreCase);

		private bool _isNavCollapsed = false;

		public MainWindow()
		{
			InitializeComponent();

			_navigationService = new ShellNavigationService(App.ModulesCatalog);

			BuildModuleNavigationUi();

			UpdateThemeUi();
			UpdateWindowStateUi();
			UpdateNavBrandUi();
			UpdateNavToggleButtonUi();
			RefreshModuleNavigationLayout();
			RefreshModuleNavigationVisuals();

			SourceInitialized += MainWindow_SourceInitialized;
		}

		private void ActivateDefaultNavigationItem()
		{
			object? content = _navigationService.ActivateDefault();

			if (content != null)
			{
				MainContentHost.Content = content;
			}

			RefreshModuleNavigationVisuals();
		}

		private void BuildModuleNavigationUi()
		{
			ModuleNavigationHost.Children.Clear();
			_navigationVisuals.Clear();

			foreach (ShellNavigationItem item in _navigationService.RootItems)
			{
				ModuleNavigationHost.Children.Add(CreateNavigationNode(item, 0));
			}
		}

		private FrameworkElement CreateNavigationNode(ShellNavigationItem item, int depth)
		{
			StackPanel container = new()
			{
				Margin = new Thickness(0, 0, 0, item.Parent == null ? 8 : 4)
			};

			Button button = CreateNavigationButton(
				item,
				depth,
				out TextBlock titleText,
				out TextBlock? indicatorText);

			container.Children.Add(button);

			StackPanel? childHost = null;

			if (item.HasChildren)
			{
				childHost = new StackPanel
				{
					Margin = new Thickness(0, 4, 0, 0)
				};

				foreach (ShellNavigationItem child in item.Children)
				{
					childHost.Children.Add(CreateNavigationNode(child, depth + 1));
				}

				container.Children.Add(childHost);
			}

			_navigationVisuals[item.ItemKey] = new NavigationVisual
			{
				Item = item,
				Button = button,
				LayoutRoot = (Grid)button.Content,
				TitleText = titleText,
				IndicatorText = indicatorText,
				ChildrenHost = childHost,
				Depth = depth
			};

			return container;
		}

		private Button CreateNavigationButton(
			ShellNavigationItem item,
			int depth,
			out TextBlock titleText,
			out TextBlock? indicatorText)
		{
			Button button = new()
			{
				Height = 42,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				Background = Brushes.Transparent,
				BorderBrush = Brushes.Transparent,
				BorderThickness = new Thickness(1),
				Padding = GetNavigationButtonPadding(depth),
				FocusVisualStyle = null,
				ToolTip = item.Title,
				Tag = item
			};

			button.Click += NavigationItemButton_Click;

			Grid layout = new()
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Center
			};

			layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			ControlSvgIcon? icon = null;

			if (!string.IsNullOrWhiteSpace(item.IconPath))
			{
				icon = new ControlSvgIcon
				{
					Width = 18,
					Height = 18,
					Source = item.IconPath,
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center
				};

				icon.SetResourceReference(ControlSvgIcon.TintProperty, "Brush.NavIcon");

				Grid.SetColumn(icon, 0);
				layout.Children.Add(icon);
			}

			titleText = new TextBlock
			{
				Text = item.Title,
				VerticalAlignment = VerticalAlignment.Center,
				TextTrimming = TextTrimming.CharacterEllipsis,
				Margin = string.IsNullOrWhiteSpace(item.IconPath)
					? new Thickness(0)
					: new Thickness(12, 0, 0, 0),
				FontSize = depth == 0 ? 13 : 12
			};

			titleText.SetResourceReference(TextBlock.ForegroundProperty, "Brush.NavIcon");

			if (depth > 0)
			{
				titleText.SetResourceReference(TextBlock.FontFamilyProperty, "Font.Palanquin");
			}

			Grid.SetColumn(titleText, 1);
			layout.Children.Add(titleText);

			if (item.HasChildren)
			{
				indicatorText = new TextBlock
				{
					Text = item.IsExpanded ? "▾" : "▸",
					FontSize = 12,
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(10, 0, 0, 0)
				};

				indicatorText.SetResourceReference(TextBlock.ForegroundProperty, "Brush.NavIcon");

				Grid.SetColumn(indicatorText, 2);
				layout.Children.Add(indicatorText);
			}
			else
			{
				indicatorText = null;
			}

			button.Content = layout;

			return button;
		}

		private void NavigationItemButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag is not ShellNavigationItem item)
			{
				return;
			}

			if (_isNavCollapsed && item.HasChildren && !item.CanActivate)
			{
				return;
			}

			object? content = _navigationService.Activate(item);

			if (content != null)
			{
				MainContentHost.Content = content;
			}

			RefreshModuleNavigationLayout();
			RefreshModuleNavigationVisuals();
		}

		private void RefreshModuleNavigationLayout()
		{
			foreach (NavigationVisual visual in _navigationVisuals.Values)
			{
				bool isCollapsed = _isNavCollapsed;

				visual.Button.HorizontalContentAlignment = isCollapsed
					? HorizontalAlignment.Center
					: HorizontalAlignment.Stretch;

				visual.Button.Padding = isCollapsed
					? new Thickness(0)
					: GetNavigationButtonPadding(visual.Depth);

				visual.LayoutRoot.HorizontalAlignment = isCollapsed
					? HorizontalAlignment.Center
					: HorizontalAlignment.Stretch;

				visual.TitleText.Visibility = isCollapsed
					? Visibility.Collapsed
					: Visibility.Visible;

				if (visual.IndicatorText != null)
				{
					visual.IndicatorText.Visibility =
						(!isCollapsed && visual.Item.HasChildren)
							? Visibility.Visible
							: Visibility.Collapsed;

					visual.IndicatorText.Text = visual.Item.IsExpanded ? "▾" : "▸";
				}

				if (visual.ChildrenHost != null)
				{
					visual.ChildrenHost.Visibility =
						(!isCollapsed && visual.Item.IsExpanded)
							? Visibility.Visible
							: Visibility.Collapsed;
				}
			}
		}

		private void RefreshModuleNavigationVisuals()
		{
			foreach (NavigationVisual visual in _navigationVisuals.Values)
			{
				bool isHighlighted = visual.Item.IsActive || visual.Item.HasActiveChild;

				if (isHighlighted)
				{
					visual.Button.SetResourceReference(Button.BackgroundProperty, "Brush.NavItemActive");
					visual.Button.SetResourceReference(Button.BorderBrushProperty, "Brush.NavItemActive");
				}
				else
				{
					visual.Button.Background = Brushes.Transparent;
					visual.Button.BorderBrush = Brushes.Transparent;
				}

				visual.TitleText.FontWeight = visual.Item.IsActive
					? FontWeights.SemiBold
					: FontWeights.Normal;

				if (visual.IndicatorText != null)
				{
					visual.IndicatorText.FontWeight =
						(visual.Item.IsActive || visual.Item.HasActiveChild)
							? FontWeights.SemiBold
							: FontWeights.Normal;
				}
			}
		}

		private void NavToggleButton_Click(object sender, RoutedEventArgs e)
		{
			_isNavCollapsed = !_isNavCollapsed;

			NavColumn.Width = _isNavCollapsed
				? new GridLength(72)
				: new GridLength(220);

			UpdateNavBrandUi();
			UpdateNavToggleButtonUi();
			RefreshModuleNavigationLayout();
			RefreshModuleNavigationVisuals();
		}

		private void UserProfileViewNavButton_Click(object sender, RoutedEventArgs e)
		{
			_navigationService.ClearActiveState();
			MainContentHost.Content = new UserProfileView();
			RefreshModuleNavigationVisuals();
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

			NavBrandImage.Source = new BitmapImage(
				new Uri(
					_isNavCollapsed
						? "/Resources/Brand/nibsphere-mark.png"
						: "/Resources/Brand/nibsphere-full.png",
					UriKind.Relative));
		}

		private sealed class NavigationVisual
		{
			public required ShellNavigationItem Item { get; init; }
			public required Button Button { get; init; }
			public required Grid LayoutRoot { get; init; }
			public required TextBlock TitleText { get; init; }
			public TextBlock? IndicatorText { get; init; }
			public StackPanel? ChildrenHost { get; init; }
			public int Depth { get; init; }
		}

		private static Thickness GetNavigationButtonPadding(int depth)
		{
			return depth == 0
				? new Thickness(16, 0, 14, 0)
				: new Thickness(34 + ((depth - 1) * 12), 0, 14, 0);
		}

		public void ShowContent(object content)
		{
			MainContentHost.Content = content;
		}
	}
}
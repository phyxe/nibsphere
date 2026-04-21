using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NibSphere.Shell.Navigation
{
	public sealed class ShellNavigationItem : INotifyPropertyChanged
	{
		private bool _isExpanded;
		private bool _isActive;
		private bool _hasActiveChild;

		public required string ModuleKey { get; init; }
		public required string ItemKey { get; init; }
		public required string Title { get; init; }

		public string? IconPath { get; init; }

		public int SortOrder { get; init; }

		public bool IsDefault { get; init; }

		public Func<object>? ContentFactory { get; init; }

		public ObservableCollection<ShellNavigationItem> Children { get; } = new();

		public ShellNavigationItem? Parent { get; internal set; }

		public bool HasChildren => Children.Count > 0;

		public bool CanActivate => ContentFactory != null;

		public bool IsExpanded
		{
			get => _isExpanded;
			set => SetField(ref _isExpanded, value);
		}

		public bool IsActive
		{
			get => _isActive;
			set => SetField(ref _isActive, value);
		}

		public bool HasActiveChild
		{
			get => _hasActiveChild;
			set => SetField(ref _hasActiveChild, value);
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
			{
				return;
			}

			field = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}